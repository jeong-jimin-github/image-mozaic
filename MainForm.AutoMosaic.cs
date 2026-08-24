using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Drawing.Imaging;

namespace ImageMosaicEditor;

public partial class MainForm
{
    private void InitializeAutoMosaicMenu()
    {
        var autoMenu = new ToolStripMenuItem("자동 모자이크(&A)");

        var currentItem = new ToolStripMenuItem("현재 이미지 자동 처리(&A)")
        {
            ShortcutKeys = Keys.Control | Keys.Shift | Keys.A
        };
        currentItem.Click += MenuAutoCurrent_Click;

        var batchItem = new ToolStripMenuItem("폴더 일괄 처리(&B)");
        batchItem.Click += MenuAutoBatch_Click;

        var settingsItem = new ToolStripMenuItem("설정(&S)");
        settingsItem.Click += MenuAutoSettings_Click;

        var installItem = new ToolStripMenuItem("Python 의존성 설치(&I)");
        installItem.Click += MenuInstallPythonDependencies_Click;

        autoMenu.DropDownItems.AddRange([
            currentItem,
            batchItem,
            new ToolStripSeparator(),
            settingsItem,
            installItem
        ]);
        menuStrip.Items.Add(autoMenu);
    }

    private async void MenuAutoCurrent_Click(object? sender, EventArgs e)
    {
        if (_autoBusy) return;
        if (_originalBitmap == null)
        {
            MessageBox.Show("먼저 이미지를 열어주세요.", "자동 모자이크",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        string tempDir = Path.Combine(Path.GetTempPath(), "ImageMosaicEditor", Guid.NewGuid().ToString("N"));
        string input = Path.Combine(tempDir, "input.png");
        string output = Path.Combine(tempDir, "output.png");
        Directory.CreateDirectory(tempDir);

        try
        {
            SetAutoBusy(true, "자동 검출 중...");
            _originalBitmap.Save(input, ImageFormat.Png);
            AutoMosaicResult result = await AutoMosaicEngine.ProcessFileAsync(input, output, _autoSettings);

            if (result.Status == "undetected" || !File.Exists(output))
            {
                statusLabel.Text = "자동 검출 결과: 대상 영역을 찾지 못했습니다.";
                MessageBox.Show("검출된 영역이 없습니다.", "자동 모자이크",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var loaded = new Bitmap(output);
            var processed = new Bitmap(loaded);
            SaveStateToUndo();
            _originalBitmap.Dispose();
            _originalBitmap = processed;
            pictureBox.Image = _originalBitmap;
            pictureBox.Invalidate();
            statusLabel.Text = $"자동 처리 완료: {result.Count}개 영역";

            if (!string.IsNullOrWhiteSpace(result.Warning))
                MessageBox.Show(result.Warning, "자동 모자이크 경고",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (Exception ex)
        {
            ShowAutoError(ex);
        }
        finally
        {
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

        try
        {
            SetAutoBusy(true, "폴더 일괄 처리 중...");
            Directory.CreateDirectory(outputDir);
            AutoMosaicResult result = await AutoMosaicEngine.ProcessFolderAsync(inputDir, outputDir, _autoSettings);
            statusLabel.Text = $"일괄 처리 완료: 처리 {result.Processed}, 미검출 {result.Undetected}, 오류 {result.Errors}";

            string message = $"출력 폴더:\n{outputDir}\n\n처리: {result.Processed}개\n미검출: {result.Undetected}개\n오류: {result.Errors}개\n검출 영역: {result.Count}개";
            if (!string.IsNullOrWhiteSpace(result.Warning))
                message += $"\n\n경고: {result.Warning}";
            MessageBox.Show(message, "자동 모자이크 완료",
                MessageBoxButtons.OK,
                result.Errors == 0 ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        }
        catch (Exception ex)
        {
            ShowAutoError(ex);
        }
        finally
        {
            SetAutoBusy(false);
        }
    }

    private void MenuAutoSettings_Click(object? sender, EventArgs e)
    {
        using var dlg = new AutoMosaicSettingsDialog(_autoSettings);
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            _autoSettings = dlg.Settings;
            statusLabel.Text = $"자동 모자이크 설정: {_autoSettings.Detector} / {_autoSettings.Mode}";
        }
    }

    private async void MenuInstallPythonDependencies_Click(object? sender, EventArgs e)
    {
        if (_autoBusy) return;
        DialogResult answer = MessageBox.Show(
            "Python 패키지 dghs-imgutils와 Pillow를 설치합니다. 계속할까요?",
            "Python 의존성 설치",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);
        if (answer != DialogResult.Yes) return;

        try
        {
            SetAutoBusy(true, "Python 의존성 설치 중...");
            await AutoMosaicEngine.InstallDependenciesAsync();
            MessageBox.Show("필수 Python 의존성 설치가 완료되었습니다.\nNTD11을 사용하려면 ultralytics를 별도로 설치하고 .pt 모델을 설정하세요.",
                "설치 완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
            statusLabel.Text = "Python 의존성 설치 완료";
        }
        catch (Exception ex)
        {
            ShowAutoError(ex);
        }
        finally
        {
            SetAutoBusy(false);
        }
    }

    private void SetAutoBusy(bool busy, string? message = null)
    {
        _autoBusy = busy;
        UseWaitCursor = busy;
        menuStrip.Enabled = !busy;
        pictureBox.Enabled = !busy;
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
