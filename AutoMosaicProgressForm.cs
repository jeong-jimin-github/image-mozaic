using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ImageMosaicEditor;

internal sealed class AutoMosaicProgressForm : Form
{
    private readonly Label _message = new()
    {
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleLeft,
        AutoEllipsis = true,
        ForeColor = Color.FromArgb(25, 39, 66),
        Font = new Font("Segoe UI Semibold", 10f, FontStyle.Bold)
    };

    private readonly Label _percent = new()
    {
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleRight,
        Text = "0%",
        ForeColor = Color.FromArgb(28, 123, 235),
        Font = new Font("Segoe UI Semibold", 10f, FontStyle.Bold)
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
        BackColor = Color.White;
        ClientSize = new Size(500, 150);
        Font = new Font("Segoe UI", 9.5f);

        var accent = new Panel
        {
            Dock = DockStyle.Top,
            Height = 5,
            BackColor = Color.FromArgb(28, 123, 235)
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 3,
            Padding = new Padding(22, 16, 22, 18),
            BackColor = Color.White
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        layout.Controls.Add(_message, 0, 0);
        layout.Controls.Add(_percent, 1, 0);
        layout.Controls.Add(_progress, 0, 1);
        layout.SetColumnSpan(_progress, 2);

        var hint = new Label
        {
            Text = L10n.T("AI 검출 단계에서는 모델 실행 동안 진행률이 잠시 같은 위치에 머물 수 있습니다."),
            Dock = DockStyle.Fill,
            ForeColor = Color.FromArgb(111, 123, 143),
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 8.7f)
        };
        layout.Controls.Add(hint, 0, 2);
        layout.SetColumnSpan(hint, 2);

        Controls.Add(layout);
        Controls.Add(accent);
    }

    public void UpdateProgress(AutoMosaicProgress progress)
    {
        int value = Math.Clamp(progress.Value, 0, 100);
        _progress.Value = value;
        _percent.Text = $"{value}%";
        _message.Text = string.IsNullOrWhiteSpace(progress.Message) ? L10n.T("처리 중...") : progress.Message;
    }
}
