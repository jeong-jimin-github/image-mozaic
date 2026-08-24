using System;
using System.Windows.Forms;

namespace ImageMosaicEditor;

public partial class MainForm
{
    private Timer? _modernStatusTimer;

    private void StartModernStatusSync()
    {
        if (_modernStatusTimer != null) return;

        _modernStatusTimer = new Timer { Interval = 500 };
        _modernStatusTimer.Tick += (_, _) =>
        {
            string text = statusLabel.Text ?? string.Empty;
            if (text.Contains("GPU (CUDA)", StringComparison.OrdinalIgnoreCase)
                || text.Contains("CUDAExecutionProvider", StringComparison.OrdinalIgnoreCase))
            {
                UpdateGpuStatus("GPU (CUDA)");
            }
            else if (text.Contains(" · CPU", StringComparison.OrdinalIgnoreCase)
                || text.Contains("CPUExecutionProvider", StringComparison.OrdinalIgnoreCase))
            {
                UpdateGpuStatus("CPU");
            }
        };
        _modernStatusTimer.Start();
        FormClosed += (_, _) =>
        {
            _modernStatusTimer?.Stop();
            _modernStatusTimer?.Dispose();
            _modernStatusTimer = null;
        };
    }
}
