using System;
using System.Drawing;
using System.Windows.Forms;
namespace ImageMosaicEditor;

internal sealed class AutoMosaicSettingsDialog : Form
{
    private readonly ComboBox _mode = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly NumericUpDown _strength = new() { Minimum = 1, Maximum = 100, Value = 20 };
    private readonly NumericUpDown _confidence = new()
    {
        Minimum = 0, Maximum = 1, DecimalPlaces = 2, Increment = 0.05M, Value = 0.25M
    };
    private readonly NumericUpDown _padding = new() { Minimum = 0, Maximum = 500, Value = 10 };
    private readonly ComboBox _detector = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly CheckBox _nipple = new() { Text = "유두 포함" };
    private readonly CheckBox _anus = new() { Text = "항문 포함" };
    private readonly CheckBox _testicles = new() { Text = "고환 포함" };
    private readonly TextBox _modelPath = new() { Dock = DockStyle.Fill };

    public AutoMosaicSettings Settings { get; private set; }

    public AutoMosaicSettingsDialog(AutoMosaicSettings current)
    {
        Settings = current.Clone();
        Text = "자동 모자이크 설정";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(520, 360);

        _mode.Items.AddRange(["mosaic", "black", "blur"]);
        _detector.Items.AddRange(["auto", "imgutils", "ntd11"]);
        _mode.SelectedItem = current.Mode;
        _detector.SelectedItem = current.Detector;
        _strength.Value = Math.Clamp(current.Strength, 1, 100);
        _confidence.Value = Math.Clamp((decimal)current.Confidence, 0, 1);
        _padding.Value = Math.Clamp(current.Padding, 0, 500);
        _nipple.Checked = current.IncludeNipple;
        _anus.Checked = current.IncludeAnus;
        _testicles.Checked = current.IncludeTesticles;
        _modelPath.Text = current.Ntd11ModelPath;

        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 8,
            Padding = new Padding(14),
            AutoSize = false
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (int i = 0; i < 7; i++) table.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        table.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        AddRow(table, 0, "처리 방식", _mode);
        AddRow(table, 1, "강도", _strength);
        AddRow(table, 2, "신뢰도", _confidence);
        AddRow(table, 3, "검출 여백(px)", _padding);
        AddRow(table, 4, "검출기", _detector);

        var targets = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true };
        targets.Controls.AddRange([_nipple, _anus, _testicles]);
        AddRow(table, 5, "추가 검출", targets);

        var modelPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
        modelPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        modelPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));
        var browse = new Button { Text = "찾기...", Dock = DockStyle.Fill };
        browse.Click += BrowseModel_Click;
        modelPanel.Controls.Add(_modelPath, 0, 0);
        modelPanel.Controls.Add(browse, 1, 0);
        AddRow(table, 6, "NTD11 모델(.pt)", modelPanel);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(0, 8, 0, 0)
        };
        var ok = new Button { Text = "확인", DialogResult = DialogResult.OK, Width = 90 };
        var cancel = new Button { Text = "취소", DialogResult = DialogResult.Cancel, Width = 90 };
        ok.Click += (_, _) => SaveSettings();
        buttons.Controls.Add(ok);
        buttons.Controls.Add(cancel);
        table.Controls.Add(buttons, 0, 7);
        table.SetColumnSpan(buttons, 2);

        Controls.Add(table);
        AcceptButton = ok;
        CancelButton = cancel;
    }

    private static void AddRow(TableLayoutPanel table, int row, string label, Control control)
    {
        var lbl = new Label
        {
            Text = label,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };
        control.Dock = DockStyle.Fill;
        table.Controls.Add(lbl, 0, row);
        table.Controls.Add(control, 1, row);
    }

    private void BrowseModel_Click(object? sender, EventArgs e)
    {
        using var dlg = new OpenFileDialog
        {
            Title = "NTD11 모델 선택",
            Filter = "PyTorch 모델 (*.pt)|*.pt|모든 파일 (*.*)|*.*"
        };
        if (dlg.ShowDialog(this) == DialogResult.OK)
            _modelPath.Text = dlg.FileName;
    }

    private void SaveSettings()
    {
        Settings = new AutoMosaicSettings
        {
            Mode = _mode.SelectedItem?.ToString() ?? "mosaic",
            Strength = (int)_strength.Value,
            Confidence = (double)_confidence.Value,
            Padding = (int)_padding.Value,
            Detector = _detector.SelectedItem?.ToString() ?? "auto",
            IncludeNipple = _nipple.Checked,
            IncludeAnus = _anus.Checked,
            IncludeTesticles = _testicles.Checked,
            Ntd11ModelPath = _modelPath.Text.Trim()
        };
    }
}
