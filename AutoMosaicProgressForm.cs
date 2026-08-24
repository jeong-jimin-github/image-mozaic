using System;
using System.Drawing;
using System.Windows.Forms;

namespace ImageMosaicEditor;

internal sealed class AutoMosaicProgressForm : Form
{
    private readonly Label _message = new()
    {
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleLeft,
        AutoEllipsis = true
    };

    private readonly Label _percent = new()
    {
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleRight,
        Text = "0%"
    };

    private readonly ProgressBar _progress = new()
    {
        Dock = DockStyle.Fill,
        Minimum = 0,
        Maximum = 100,
        Style = ProgressBarStyle.Continuous
    };

    public AutoMosaicProgressForm(string title)
    {
        Text = title;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ControlBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(460, 126);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 3,
            Padding = new Padding(18, 14, 18, 14)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 64));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        layout.Controls.Add(_message, 0, 0);
        layout.Controls.Add(_percent, 1, 0);
        layout.Controls.Add(_progress, 0, 1);
        layout.SetColumnSpan(_progress, 2);

        var hint = new Label
        {
            Text = "검출 모델 실행 중에는 몇 초간 진행률이 같은 위치에 머물 수 있습니다.",
            Dock = DockStyle.Fill,
            ForeColor = SystemColors.GrayText,
            TextAlign = ContentAlignment.MiddleLeft
        };
        layout.Controls.Add(hint, 0, 2);
        layout.SetColumnSpan(hint, 2);
        Controls.Add(layout);
    }

    public void UpdateProgress(AutoMosaicProgress progress)
    {
        int value = Math.Clamp(progress.Value, 0, 100);
        _progress.Value = value;
        _percent.Text = $"{value}%";
        _message.Text = string.IsNullOrWhiteSpace(progress.Message) ? "처리 중..." : progress.Message;
    }
}
