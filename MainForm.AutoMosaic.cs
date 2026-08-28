using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ImageMosaicEditor;

public partial class MainForm
{
    private static readonly string[] ImportExtensions = [".png", ".jpg", ".jpeg", ".webp"];
    private string? _lastBatchInputDir;
    private string? _lastBatchOutputDir;

    private void InitializeAutoMosaicMenu()
    {
        menuOpen.Click -= MenuOpen_Click;
        menuOpen.Click += MenuOpenAndAuto_Click;

        InitializeFolderBrowserPanel();
        InitializeDragDropImport();
        InitializeEraserTools();

        var autoMenu = new ToolStripMenuItem(L10n.T("자동 모자이크(&A)")) { Name = "autoMosaicMenu" };

        var currentItem = new ToolStripMenuItem(L10n.T("현재 이미지 다시 자동 처리(&A)"))
        {
            Name = "autoCurrentMenu",
            ShortcutKeys = Keys.Control | Keys.Shift | Keys.A
        };
        currentItem.Click += MenuAutoCurrent_Click;

        var batchItem = new ToolStripMenuItem(L10n.T("폴더 일괄 처리(&B)")) { Name = "autoBatchMenu" };
        batchItem.Click += MenuAutoBatch_Click;

        var settingsItem = new ToolStripMenuItem(L10n.T("설정(&S)")) { Name = "autoSettingsMenu" };
        settingsItem.Click += MenuAutoSettings_Click;

        autoMenu.DropDownItems.AddRange([
            currentItem,
            batchItem,
            new ToolStripSeparator(),
            settingsItem
        ]);
        menuStrip.Items.Add(autoMenu);
    }

    private void InitializeDragDropImport()
    {
        RegisterDropTarget(this);
        RegisterDropTarget(pictureBox);
        if (_folderList != null) RegisterDropTarget(_folderList);
    }

    private void RegisterDropTarget(Control control)
    {
        control.AllowDrop = true;
        control.DragEnter += Import_DragEnter;
        control.DragDrop += Import_DragDrop;
    }

    private async void MenuOpenAndAuto_Click(object? sender, EventArgs e)
    {
        if (_autoBusy) return;

        using var dlg = new OpenFileDialog
        {
            Title = L10n.T("이미지 파일 열기"),
            Filter = L10n.T("이미지 파일 (*.png;*.jpg;*.jpeg;*.webp)|*.png;*.jpg;*.jpeg;*.webp|모든 파일 (*.*)|*.*")
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        await ImportAndAutoCensorAsync(dlg.FileName);
    }

    private void Import_DragEnter(object? sender, DragEventArgs e)
    {
        if (_autoBusy || e.Data == null || !e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effect = DragDropEffects.None;
            return;
        }

        string[]? paths = e.Data.GetData(DataFormats.FileDrop) as string[];
        e.Effect = paths?.Any(path => Directory.Exists(path) || IsSupportedImportFile(path)) == true
            ? DragDropEffects.Copy
            : DragDropEffects.None;
    }

    private async void Import_DragDrop(object? sender, DragEventArgs e)
    {
        if (_autoBusy || e.Data == null) return;

        string stage = "드롭 경로 확인";
        try
        {
            string[]? paths = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (paths == null || paths.Length == 0) return;

            string? folder = paths.FirstOrDefault(Directory.Exists);
            if (folder != null)
            {
                stage = "폴더 미리보기 생성";
                await OpenFolderAsync(folder, autoOpen: false);

                stage = "폴더 전체 자동검열";
                await RunFolderBatchAsync(folder, openOutputFolder: true);
                return;
            }

            string? firstImage = paths.FirstOrDefault(IsSupportedImportFile);
            if (firstImage != null)
            {
                stage = "이미지 가져오기";
                await ImportAndAutoCensorAsync(firstImage);
            }
        }
        catch (Exception ex)
        {
            WriteAutoDiagnostic(stage, null, ex);
            ShowAutoError(ex, stage);
        }
    }

    private static bool IsSupportedImportFile(string path)
    {
        return File.Exists(path) && ImportExtensions.Contains(
            Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);
    }

    private async Task ImportAndAutoCensorAsync(string path, bool refreshFolderPreview = true)
    {
        string stage = "가져오기 준비";
        try
        {
            stage = "이미지 디코딩";
            LoadImageDetached(path);

            if (refreshFolderPreview)
            {
                stage = "폴더 미리보기 생성";
                await RefreshFolderPreviewForFileAsync(path);
            }

            SelectFolderImage(path);
            statusLabel.Text = L10n.T("이미지 가져오기 완료 - 자동 검열을 시작합니다...");

            stage = "자동 검열";
            await AutoCensorCurrentImageAsync(showUndetectedMessage: false);
        }
        catch (Exception ex)
        {
            WriteAutoDiagnostic(stage, path, ex);
            ShowAutoError(ex, stage);
        }
    }

    private async void MenuAutoCurrent_Click(object? sender, EventArgs e)
    {
        try
        {
            await AutoCensorCurrentImageAsync(showUndetectedMessage: true);
        }
        catch (Exception ex)
        {
            WriteAutoDiagnostic("수동 자동검열", _currentFilePath, ex);
            ShowAutoError(ex, "수동 자동검열");
        }
    }

    private async Task AutoCensorCurrentImageAsync(bool showUndetectedMessage)
    {
        if (_autoBusy) return;

        Bitmap? source = _sourceBitmap ?? _originalBitmap;
        if (source == null)
        {
            if (showUndetectedMessage)
            {
                MessageBox.Show(L10n.T("먼저 이미지를 열어주세요."), L10n.T("자동 모자이크"),
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            return;
        }

        string tempDir = Path.Combine(Path.GetTempPath(), "ImageMosaicEditor", Guid.NewGuid().ToString("N"));
        string input = Path.Combine(tempDir, "input.png");
        string output = Path.Combine(tempDir, "output.png");
        Directory.CreateDirectory(tempDir);

        using var progressWindow = new AutoMosaicProgressForm(L10n.T("자동 검열 진행"));
        var progress = new Progress<AutoMosaicProgress>(p =>
        {
            if (!progressWindow.IsDisposed && progressWindow.IsHandleCreated)
                progressWindow.UpdateProgress(p);
            statusLabel.Text = p.Message;
        });

        string stage = "자동 검열 준비";
        try
        {
            SetAutoBusy(true, L10n.T("자동 검열 준비 중..."));
            progressWindow.Show(this);
            progressWindow.UpdateProgress(new AutoMosaicProgress(1, L10n.T("원본 이미지에서 새로 처리 준비 중...")));

            // Always feed the untouched source bitmap to Python. Changing mask
            // settings can therefore never stack a new mosaic over an old one.
            stage = "원본 임시 PNG 저장";
            SaveBitmapAsPngDetached(source, input);

            stage = "Python 검출/정밀 마스킹";
            AutoMosaicResult result = await AutoMosaicEngine.ProcessFileAsync(
                input, output, _autoSettings, progress);

            if (result.Status == "undetected" || !File.Exists(output))
            {
                // A fresh run with no detections means the working image must also
                // become clean source, rather than leaving the previous mask behind.
                ResetWorkingFromSource(saveUndo: true);
                statusLabel.Text = L10n.F("자동 검출 완료: 검열 대상 영역 없음{0}", ProviderSuffix(result.Provider));
                if (showUndetectedMessage)
                {
                    MessageBox.Show(L10n.T("검출된 영역이 없습니다. 이전 자동 처리는 제거하고 원본으로 되돌렸습니다."),
                        L10n.T("자동 모자이크"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                return;
            }

            stage = "검열 결과 이미지 로드";
            Bitmap processed = LoadBitmapDetached(output);
            ReplaceWorkingBitmap(processed, saveUndo: true);
            statusLabel.Text = L10n.F("자동 검열 완료: {0}개 영역{1}", result.Count, ProviderSuffix(result.Provider));

            if (!string.IsNullOrWhiteSpace(result.Warning))
            {
                MessageBox.Show(result.Warning, L10n.T("자동 모자이크 경고"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        catch (Exception ex)
        {
            WriteAutoDiagnostic(stage, _currentFilePath, ex);
            throw new InvalidOperationException($"{stage} 단계 실패: {ex.Message}", ex);
        }
        finally
        {
            if (!progressWindow.IsDisposed) progressWindow.Close();
            SetAutoBusy(false);
            TryDeleteDirectory(tempDir);
        }
    }

    private async void MenuAutoBatch_Click(object? sender, EventArgs e)
    {
        if (_autoBusy) return;

        using var dlg = new FolderBrowserDialog
        {
            Description = L10n.T("자동 처리할 이미지 폴더를 선택하세요."),
            UseDescriptionForTitle = true
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        await RunFolderBatchAsync(dlg.SelectedPath, openOutputFolder: true);
    }

    private async Task RunFolderBatchAsync(string inputDir, bool openOutputFolder)
    {
        if (_autoBusy) return;
        if (!Directory.Exists(inputDir))
            throw new DirectoryNotFoundException(inputDir);

        inputDir = Path.GetFullPath(inputDir);
        string parent = Directory.GetParent(inputDir)?.FullName ?? inputDir;
        string name = new DirectoryInfo(inputDir).Name;
        string outputDir = Path.Combine(parent, name + "_mosaic");

        _lastBatchInputDir = inputDir;
        _lastBatchOutputDir = Path.GetFullPath(outputDir);

        using var progressWindow = new AutoMosaicProgressForm(L10n.T("폴더 전체 자동 검열 진행"));
        var progress = new Progress<AutoMosaicProgress>(p =>
        {
            if (!progressWindow.IsDisposed && progressWindow.IsHandleCreated)
                progressWindow.UpdateProgress(p);
            statusLabel.Text = p.Message;
        });

        try
        {
            SetAutoBusy(true, L10n.T("폴더 전체 자동 검열 준비 중..."));
            progressWindow.Show(this);
            progressWindow.UpdateProgress(new AutoMosaicProgress(1, L10n.T("처리할 파일 확인 중...")));
            Directory.CreateDirectory(outputDir);

            AutoMosaicResult result = await AutoMosaicEngine.ProcessFolderAsync(
                inputDir, outputDir, _autoSettings, progress);

            statusLabel.Text = L10n.F("전체 처리 완료: 처리 {0}, 미검출 {1}, 오류 {2}{3}", result.Processed, result.Undetected, result.Errors, ProviderSuffix(result.Provider));

            if (openOutputFolder && Directory.Exists(outputDir))
            {
                await OpenFolderAsync(outputDir, autoOpen: false);

                string? firstOutput = Directory.EnumerateFiles(outputDir)
                    .Where(IsSupportedImportFile)
                    .OrderBy(path => Path.GetFileName(path), StringComparer.OrdinalIgnoreCase)
                    .FirstOrDefault();

                if (firstOutput != null)
                {
                    LoadImageDetached(firstOutput, ResolveSourcePathForWorkingFile(firstOutput));
                    SelectFolderImage(firstOutput);
                }
            }

            string message = L10n.F("출력 폴더:\n{0}\n\n처리: {1}개\n미검출: {2}개\n오류: {3}개\n검출 영역: {4}개", outputDir, result.Processed, result.Undetected, result.Errors, result.Count);
            if (!string.IsNullOrWhiteSpace(result.Provider))
                message += "\n" + L10n.F("실행 장치: {0}", result.Provider);
            if (!string.IsNullOrWhiteSpace(result.Warning))
                message += "\n\n" + L10n.F("경고: {0}", result.Warning);

            MessageBox.Show(message, L10n.T("폴더 전체 자동 모자이크 완료"),
                MessageBoxButtons.OK,
                result.Errors == 0 ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        }
        catch (Exception ex)
        {
            WriteAutoDiagnostic("폴더 전체 자동검열", inputDir, ex);
            ShowAutoError(ex, "폴더 전체 자동검열");
        }
        finally
        {
            if (!progressWindow.IsDisposed) progressWindow.Close();
            SetAutoBusy(false);
        }
    }

    private string? ResolveSourcePathForWorkingFile(string path)
    {
        if (string.IsNullOrWhiteSpace(_lastBatchInputDir)
            || string.IsNullOrWhiteSpace(_lastBatchOutputDir))
            return null;

        string fullPath = Path.GetFullPath(path);
        string? folder = Path.GetDirectoryName(fullPath);
        if (!string.Equals(folder, _lastBatchOutputDir, StringComparison.OrdinalIgnoreCase))
            return null;

        string candidate = Path.Combine(_lastBatchInputDir, Path.GetFileName(fullPath));
        return File.Exists(candidate) ? candidate : null;
    }

    private bool IsBatchOutputPath(string path)
    {
        if (string.IsNullOrWhiteSpace(_lastBatchOutputDir)) return false;
        string? folder = Path.GetDirectoryName(Path.GetFullPath(path));
        return string.Equals(folder, _lastBatchOutputDir, StringComparison.OrdinalIgnoreCase);
    }

    private static string ProviderSuffix(string? provider)
    {
        return string.IsNullOrWhiteSpace(provider) ? string.Empty : $" · {provider}";
    }

    private void MenuAutoSettings_Click(object? sender, EventArgs e)
    {
        using var dlg = new AutoMosaicSettingsDialog(_autoSettings);
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            _autoSettings = dlg.Settings;
            if (!string.Equals(L10n.CurrentLanguage, dlg.SelectedLanguage, StringComparison.OrdinalIgnoreCase))
            {
                L10n.SetLanguage(dlg.SelectedLanguage);
                ApplyLocalization();
            }
            int enabled = (_autoSettings.IncludeNipple ? 1 : 0)
                + (_autoSettings.IncludeAnus ? 1 : 0)
                + (_autoSettings.IncludeTesticles ? 1 : 0);
            statusLabel.Text = L10n.F("자동 모자이크 설정: {0} / 추가 검출 {1}/3 - 다시 처리하면 원본에서 새로 적용됩니다.", _autoSettings.Mode, enabled);
        }
    }

    private void SetAutoBusy(bool busy, string? message = null)
    {
        _autoBusy = busy;
        UseWaitCursor = busy;
        menuStrip.Enabled = !busy;
        pictureBox.Enabled = !busy;
        if (_folderList != null) _folderList.Enabled = !busy;
        if (!string.IsNullOrWhiteSpace(message)) statusLabel.Text = message;
    }

    private void ShowAutoError(Exception ex, string? stage = null)
    {
        statusLabel.Text = L10n.T("자동 모자이크 처리 실패");
        string stageText = string.IsNullOrWhiteSpace(stage) ? string.Empty : L10n.F("오류 단계: {0}\n\n", stage);
        MessageBox.Show(
            L10n.F("{0}{1}\n\n상세 로그: %LOCALAPPDATA%\\ImageMosaicEditor\\auto-error.log", stageText, ex.Message),
            L10n.T("자동 모자이크 오류"),
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path)) Directory.Delete(path, true);
        }
        catch
        {
            // Temporary files are best-effort cleanup only.
        }
    }
}
