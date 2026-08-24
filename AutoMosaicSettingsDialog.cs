using System;
using System.Drawing;
using System.Windows.Forms;

namespace ImageMosaicEditor;

internal sealed class AutoMosaicSettingsDialog : Form
{
    private static readonly Color Surface = Color.White;
    private static readonly Color Background = Color.FromArgb(244, 247, 251);
    private static readonly Color TextColor = Color.FromArgb(25, 39, 66);
    private static readonly Color Muted = Color.FromArgb(111, 123, 143);
    private static readonly Color Blue = Color.FromArgb(28, 123, 235);

    private readonly ComboBox _mode = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly NumericUpDown _strength = new() { Minimum = 1, Maximum = 100, Value = 20 };
    private readonly NumericUpDown _confidence = new()
    {
        Minimum = 0, Maximum = 1, DecimalPlaces = 2, Increment = 0.05M, Value = 0.25M
    };
    private readonly NumericUpDown _padding = new() { Minimum = 0, Maximum = 500, Value = 10 };
    private readonly CheckBox _nipple = new() { Text = "유두" };
    private readonly CheckBox _anus = new() { Text = "항문" };
    private readonly CheckBox _testicles = new() { Text = "고환/남성 생식기" };

    public AutoMosaicSettings Settings { get; private set; }

    public AutoMosaicSettingsDialog(AutoMosaicSettings current)
    {
        Settings = current.Clone();
        Text = "자동 모자이크 설정";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(570, 390);
        BackColor = Background;
        Font = new Font("Segoe UI", 9.5f);

        _mode.Items.AddRange(["mosaic", "black", "blur"]);
        _mode.SelectedItem = current.Mode;
        _strength.Value = Math.Clamp(current.Strength, 1, 100);
        _confidence.Value = Math.Clamp((decimal)current.Confidence, 0, 1);
        _padding.Value = Math.Clamp(current.Padding, 0, 500);
        _nipple.Checked = current.IncludeNipple;
        _anus.Checked = current.IncludeAnus;
        _testicles.Checked = current.IncludeTesticles;

        var header = new Panel
        {
            Dock = DockStyle.Top,
            Height = 74,
            BackColor = Surface,
            Padding = new Padding(22, 13, 20, 8)
        };
        var title = new Label
        {
            Text = "자동 모자이크 설정",
            Dock = DockStyle.Top,
            Height = 28,
            ForeColor = TextColor,
            Font = new Font("Segoe UI Semibold", 14f, FontStyle.Bold)
        };
        var subtitle = new Label
        {
            Text = "검출 민감도와 처리 방식, 기본 검출 대상을 조정합니다.",
            Dock = DockStyle.Fill,
            ForeColor = Muted,
            Font = new Font("Segoe UI", 9f)
        };
        header.Controls.Add(subtitle);
        header.Controls.Add(title);

        var card = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Surface,
            Padding = new Padding(20),
            Margin = new Padding(18)
        };

        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 6,
            Padding = new Padding(0),
            BackColor = Surface
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (int i = 0; i < 5; i++) table.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        table.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        StyleInput(_mode);
        StyleInput(_strength);
        StyleInput(_confidence);
        StyleInput(_padding);
        StyleCheck(_nipple);
        StyleCheck(_anus);
        StyleCheck(_testicles);

        AddRow(table, 0, "처리 방식", _mode);
        AddRow(table, 1, "강도", _strength);
        AddRow(table, 2, "신뢰도", _confidence);
        AddRow(table, 3, "검출 여백(px)", _padding);

        var targets = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            BackColor = Surface,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(0, 8, 0, 0)
        };
        targets.Controls.AddRange([_nipple, _anus, _testicles]);
        AddRow(table, 4, "추가 검출", targets);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            BackColor = Surface,
            Padding = new Padding(0, 14, 0, 0)
        };
        var ok = CreateButton("확인", true);
        var cancel = CreateButton("취소", false);
        ok.DialogResult = DialogResult.OK;
        cancel.DialogResult = DialogResult.Cancel;
        ok.Click += (_, _) => SaveSettings();
        buttons.Controls.Add(ok);
        buttons.Controls.Add(cancel);
        table.Controls.Add(buttons, 0, 5);
        table.SetColumnSpan(buttons, 2);

        card.Controls.Add(table);
        var shell = new Panel { Dock = DockStyle.Fill, BackColor = Background, Padding = new Padding(18) };
        shell.Controls.Add(card);

        Controls.Add(shell);
        Controls.Add(header);
        AcceptButton = ok;
        CancelButton = cancel;
    }

    private static Button CreateButton(string text, bool primary)
    {
        return new Button
        {
            Text = text,
            Width = 96,
            Height = 36,
            FlatStyle = FlatStyle.Flat,
            BackColor = primary ? Blue : Color.FromArgb(246, 248, 251),
            ForeColor = primary ? Color.White : TextColor,
            Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold),
            Margin = new Padding(8, 0, 0, 0),
            FlatAppearance = { BorderColor = primary ? Blue : Color.FromArgb(215, 222, 232), BorderSize = 1 }
        };
    }

    private static void StyleInput(Control control)
    {
        control.Font = new Font("Segoe UI", 9.5f);
        control.BackColor = Surface;
        control.ForeColor = TextColor;
        control.Margin = new Padding(0, 7, 0, 7);
    }

    private static void StyleCheck(CheckBox control)
    {
        control.ForeColor = TextColor;
        control.BackColor = Surface;
        control.Margin = new Padding(0, 0, 16, 0);
    }

    private static void AddRow(TableLayoutPanel table, int row, string label, Control control)
    {
        var lbl = new Label
        {
            Text = label,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = TextColor,
            Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold)
        };
        control.Dock = DockStyle.Fill;
        table.Controls.Add(lbl, 0, row);
        table.Controls.Add(control, 1, row);
    }

    private void SaveSettings()
    {
        Settings = new AutoMosaicSettings
        {
            Mode = _mode.SelectedItem?.ToString() ?? "mosaic",
            Strength = (int)_strength.Value,
            Confidence = (double)_confidence.Value,
            Padding = (int)_padding.Value,
            IncludeNipple = _nipple.Checked,
            IncludeAnus = _anus.Checked,
            IncludeTesticles = _testicles.Checked
        };
    }
}
