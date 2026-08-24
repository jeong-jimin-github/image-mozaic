using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ImageMosaicEditor;

public partial class MainForm
{
    private static readonly string[] ImportExtensions = [".png", ".jpg", ".jpeg"];

    private void InitializeAutoMosaicMenu()
    {
        menuOpen.Click -= MenuOpen_Click;
        menuOpen.Click += MenuOpenAndAuto_Click;

        InitializeFolderBrowserPanel();
        InitializeDragDropImport();

        var autoMenu = new ToolStripMenuItem("자동 모자이크(&A)");

        var currentItem = new ToolStripMenuItem("현재 이미지 다시 자동 처리(&A)")
        {
            ShortcutKeys = Keys.Control | Keys.Shift | Keys.A
        };
        currentItem.Click += MenuAutoCurrent_Click;

        var batchItem = new ToolStripMenuItem("폴더 일괄 처리(&B)");
        batchItem.Click += MenuAutoBatch_Click;

        var settingsItem = new ToolStripMenuItem("설정(&S)");
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
            Title = "이미지 파일 열기",
            Filter = "이미지 파일 (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg|모든 파일 (*.*)|*.*"
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

        string[]? paths = e.Data.GetData(DataFormats.FileDrop) as string[];
        if (paths == null || paths.Length == 0) return;

        string? folder = paths.FirstOrDefault(Directory.Exists);
        if (folder != null)
        {
            await OpenFolderAsync(folder, autoOpen: true);
            return;
        }

        string? firstImage = paths.FirstOrDefault(IsSupportedImportFile);
        if (firstImage != null)
            await ImportAndAutoCensorAsync(firstImage);
    }

    private static bool IsSupportedImportFile(string path)
    {
        return File.Exists(path) && ImportExtensions.Contains(
            Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);
    }

    private async Task ImportAndAutoCensorAsync(string path, bool refreshFolderPreview = true)
    {
        try
        {
            if (refreshFolderPreview)
                await RefreshFolderPreviewForFileAsync(path);

            LoadImage(path);
            SelectFolderImage(path);

            statusLabel.Text = "이미지 가져오기 완료 - 자동 검열을 시작합니다...";
            await AutoCensorCurrentImageAsync(showUndetectedMessage: false);
        }
        catch (Exception ex)
        {
            ShowAutoError(ex);
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
            ShowAutoError(ex);
        }
    }

    private async Task AutoCensorCurrentImageAsync(bool showUndetectedMessage)
    {
        if (_autoBusy) return;
        if (_originalBitmap == null)
        {
            if (showUndetectedMessage)
            {
                MessageBox.Show("먼저 이미지를 열어주세요.", "자동 모자이크",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            return;
        }

        string tempDir = Path.Combine(Path.GetTempPath(), "ImageMosaicEditor", Guid.NewGuid().ToString("N"));
        string input = Path.Combine(tempDir, "input.png");
        string output = Path.Combine(tempDir, "output.png");
        Directory.CreateDirectory(tempDir);

        using var progressWindow = new AutoMosaicProgressForm("자동 검열 진행");
        var progress = new Progress<AutoMosaicProgress>(p =>
        {
            if (!progressWindow.IsDisposed && progressWindow.IsHandleCreated)
                progressWindow.UpdateProgress(p);
            statusLabel.Text = p.Message;
        });

        try
        {
            SetAutoBusy(true, "자동 검열 준비 중...");
            progressWindow.Show(this);
            progressWindow.UpdateProgress(new AutoMosaicProgress(1, "이미지 준비 중..."));

            _originalBitmap.Save(input, ImageFormat.Png);
            AutoMosaicResult result = await AutoMosaicEngine.ProcessFileAsync(
                input, output, _autoSettings, progress);

            if (result.Status == "undetected" || !File.Exists(output))
            {
                statusLabel.Text = "자동 검출 완료: 검열 대상 영역 없음";
                if (showUndetectedMessage)
                {
                    MessageBox.Show("검출된 영역이 없습니다.", "자동 모자이크",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                return;
            }

            using var loaded = new Bitmap(output);
            var processed = new Bitmap(loaded);
            SaveStateToUndo();
            _originalBitmap.Dispose();
            _originalBitmap = processed;
            pictureBox.Image = _originalBitmap;
            pictureBox.Invalidate();
            statusLabel.Text = $"자동 검열 완료: {result.Count}개 영역";

            if (!string.IsNullOrWhiteSpace(result.Warning))
            {
                MessageBox.Show(result.Warning, "자동 모자이크 경고",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
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
            Description = "자동 처리할 이미지 폴더를 선택하세요.",
            UseDescriptionForTitle = true
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        string inputDir = dlg.SelectedPath;
        string parent = Directory.GetParent(inputDir)?.FullName ?? inputDir;
        string name = new DirectoryInfo(inputDir).Name;
        string outputDir = Path.Combine(parent, name + "_mosaic");

        using var progressWindow = new AutoMosaicProgressForm("폴더 자동 검열 진행");
        var progress = new Progress<AutoMosaicProgress>(p =>
        {
            if (!progressWindow.IsDisposed && progressWindow.IsHandleCreated)
                progressWindow.UpdateProgress(p);
            statusLabel.Text = p.Message;
        });

        try
        {
            SetAutoBusy(true, "폴더 일괄 처리 준비 중...");
            progressWindow.Show(this);
            progressWindow.UpdateProgress(new AutoMosaicProgress(1, "처리할 파일 확인 중..."));
            Directory.CreateDirectory(outputDir);

            AutoMosaicResult result = await AutoMosaicEngine.ProcessFolderAsync(
                inputDir, outputDir, _autoSettings, progress);

            statusLabel.Text = $"일괄 처리 완료: 처리 {result.Processed}, 미검출 {result.Undetected}, 오류 {result.Errors}";
            string message = $"출력 폴더:\n{outputDir}\n\n처리: {result.Processed}개\n미검출: {result.Undetected}개\n오류: {result.Errors}개\n검출 영역: {result.Count}개";
            if (!string.IsNullOrWhiteSpace(result.Warning))
                message += $"\n\n경고: {result.Warning}";

            MessageBox.Show(message, "자동 모자이크 완료",
                MessageBoxButtons.OK,
                result.Errors == 0 ? MessageBoxIcon.Information : MessageBoxIcon.Warning);

            if (string.Equals(_currentFolder, inputDir, StringComparison.OrdinalIgnoreCase))
                await OpenFolderAsync(inputDir, _currentFilePath, autoOpen: false);
        }
        catch (Exception ex)
        {
            ShowAutoError(ex);
        }
        finally
        {
            if (!progressWindow.IsDisposed) progressWindow.Close();
            SetAutoBusy(false);
        }
    }

    private void MenuAutoSettings_Click(object? sender, EventArgs e)
    {
        using var dlg = new AutoMosaicSettingsDialog(_autoSettings);
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            _autoSettings = dlg.Settings;
            int enabled = (_autoSettings.IncludeNipple ? 1 : 0)
                + (_autoSettings.IncludeAnus ? 1 : 0)
                + (_autoSettings.IncludeTesticles ? 1 : 0);
            statusLabel.Text = $"자동 모자이크 설정: {_autoSettings.Mode} / 추가 검출 {enabled}/3";
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

    private void ShowAutoError(Exception ex)
    {
        statusLabel.Text = "자동 모자이크 처리 실패";
        MessageBox.Show(ex.Message, "자동 모자이크 오류",
            MessageBoxButtons.OK, MessageBoxIcon.Error);
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
