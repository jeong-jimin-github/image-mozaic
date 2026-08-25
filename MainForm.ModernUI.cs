using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ImageMosaicEditor;

public partial class MainForm
{
    private Panel? _modernRibbon;
    private ToolStripStatusLabel? _gpuStatusLabel;
    private ToolStripStatusLabel? _modeStatusLabel;
    private ToolStripStatusLabel? _eraserStatusLabel;
    private ToolStripStatusLabel? _imageSizeStatusLabel;
    private ModernRibbonButton? _selectionRibbonButton;
    private ModernRibbonButton? _eraserRibbonButton;
    private EraserSizePicker? _eraserSizePicker;
    private Image? _emptyStateArtwork;

    private static readonly Color UiBackground = Color.FromArgb(245, 247, 250);
    private static readonly Color UiSurface = Color.White;
    private static readonly Color UiText = Color.FromArgb(18, 38, 72);
    private static readonly Color UiMuted = Color.FromArgb(105, 119, 142);
    private static readonly Color UiBlue = Color.FromArgb(25, 122, 234);
    private static readonly Color UiBlueSoft = Color.FromArgb(232, 243, 255);
    private static readonly Color UiBorder = Color.FromArgb(218, 225, 234);
    private static readonly Color CanvasTop = Color.FromArgb(88, 94, 104);
    private static readonly Color CanvasBottom = Color.FromArgb(66, 71, 80);

    private void InitializeModernUi()
    {
        SuspendLayout();
        try
        {
            BackColor = UiBackground;
            Font = new Font("Segoe UI", 9.5f);
            MinimumSize = new Size(1080, 720);
            ClientSize = new Size(1360, 860);

            StyleMenuStrip();
            AddModernTopMenus();
            CreateModernRibbon();
            StyleWorkspace();
            StyleStatusStrip();
            LoadEmptyStateArtwork();

            pictureBox.Paint += ModernCanvas_Paint;
            Shown += (_, _) =>
            {
                LayoutModernChrome();
                RefreshModernStatusDetails();
            };
            SizeChanged += (_, _) => LayoutModernChrome();
        }
        finally
        {
            ResumeLayout(true);
        }
    }

    private void StyleMenuStrip()
    {
        menuStrip.AutoSize = false;
        menuStrip.Height = 40;
        menuStrip.Padding = new Padding(24, 3, 10, 3);
        menuStrip.BackColor = UiSurface;
        menuStrip.ForeColor = UiText;
        menuStrip.Font = new Font("Segoe UI", 10.5f);
        menuStrip.Renderer = new ToolStripProfessionalRenderer(new ModernColorTable());
        foreach (ToolStripItem item in menuStrip.Items)
        {
            item.Margin = new Padding(0, 0, 7, 0);
            item.Padding = new Padding(10, 0, 10, 0);
        }
    }

    private void AddModernTopMenus()
    {
        if (menuStrip.Items.ContainsKey("modernViewMenu")) return;

        var viewMenu = new ToolStripMenuItem("보기(&V)") { Name = "modernViewMenu" };
        var resetView = new ToolStripMenuItem("작업 화면 맞춤");
        resetView.Click += (_, _) => LayoutModernChrome();
        viewMenu.DropDownItems.Add(resetView);

        var toolsMenu = new ToolStripMenuItem("도구(&T)") { Name = "modernToolsMenu" };
        var select = new ToolStripMenuItem("선택 모드") { ShortcutKeys = Keys.Control | Keys.M };
        select.Click += (_, _) => { SetEditToolMode(EditToolMode.MosaicSelection); UpdateModernToolSelection(); };
        var erase = new ToolStripMenuItem("지우개 모드") { ShortcutKeys = Keys.Control | Keys.E };
        erase.Click += (_, _) => { SetEditToolMode(EditToolMode.MaskEraser); UpdateModernToolSelection(); };
        toolsMenu.DropDownItems.AddRange(new ToolStripItem[] { select, erase });

        var settingsMenu = new ToolStripMenuItem("설정(&S)") { Name = "modernSettingsMenu" };
        settingsMenu.Click += MenuAutoSettings_Click;
        var helpMenu = new ToolStripMenuItem("도움말(&H)") { Name = "modernHelpMenu" };
        helpMenu.Click += (_, _) => ShowModernHelp();

        menuStrip.Items.Add(viewMenu);
        menuStrip.Items.Add(toolsMenu);
        menuStrip.Items.Add(settingsMenu);
        menuStrip.Items.Add(helpMenu);
    }

    private void CreateModernRibbon()
    {
        if (_modernRibbon != null) return;

        var ribbon = new Panel
        {
            BackColor = UiSurface,
            Height = 118,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
        ribbon.Paint += (_, e) =>
        {
            using var pen = new Pen(UiBorder);
            e.Graphics.DrawLine(pen, 0, ribbon.Height - 1, ribbon.Width, ribbon.Height - 1);
        };

        var flow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoScroll = true,
            Padding = new Padding(24, 7, 14, 7),
            BackColor = UiSurface
        };

        flow.Controls.Add(CreateRibbonButton("열기", ModernIcon.Open, async () =>
        {
            MenuOpenAndAuto_Click(this, EventArgs.Empty);
            await Task.CompletedTask;
        }, Color.FromArgb(242, 154, 42)));
        flow.Controls.Add(CreateRibbonButton("폴더 열기", ModernIcon.Folder, async () =>
        {
            MenuAutoBatch_Click(this, EventArgs.Empty);
            await Task.CompletedTask;
        }, Color.FromArgb(242, 154, 42)));
        flow.Controls.Add(CreateRibbonSeparator());

        flow.Controls.Add(CreateRibbonButton("저장", ModernIcon.Save, () =>
        {
            MenuSave_Click(this, EventArgs.Empty);
            return Task.CompletedTask;
        }));
        flow.Controls.Add(CreateRibbonButton("다른 이름으로", ModernIcon.SaveAs, () =>
        {
            SaveAsModern();
            return Task.CompletedTask;
        }));
        flow.Controls.Add(CreateRibbonSeparator());

        flow.Controls.Add(CreateRibbonButton("실행 취소", ModernIcon.Undo, () =>
        {
            MenuUndo_Click(this, EventArgs.Empty);
            RefreshModernStatusDetails();
            return Task.CompletedTask;
        }, Color.FromArgb(132, 143, 160)));
        flow.Controls.Add(CreateRibbonButton("다시 실행", ModernIcon.Redo, () =>
        {
            MenuRedo_Click(this, EventArgs.Empty);
            RefreshModernStatusDetails();
            return Task.CompletedTask;
        }, Color.FromArgb(132, 143, 160)));
        flow.Controls.Add(CreateRibbonSeparator());

        flow.Controls.Add(CreateRibbonButton("자동 모자이크", ModernIcon.Magic, async () =>
        {
            MenuAutoCurrent_Click(this, EventArgs.Empty);
            await Task.CompletedTask;
        }));

        _selectionRibbonButton = CreateRibbonButton("선택 모드", ModernIcon.Pointer, () =>
        {
            SetEditToolMode(EditToolMode.MosaicSelection);
            UpdateModernToolSelection();
            return Task.CompletedTask;
        });
        flow.Controls.Add(_selectionRibbonButton);

        _eraserRibbonButton = CreateRibbonButton("지우개 모드", ModernIcon.Eraser, () =>
        {
            SetEditToolMode(EditToolMode.MaskEraser);
            UpdateModernToolSelection();
            return Task.CompletedTask;
        });
        flow.Controls.Add(_eraserRibbonButton);
        flow.Controls.Add(CreateRibbonSeparator());

        _eraserSizePicker = new EraserSizePicker
        {
            Width = 220,
            Height = 94,
            Margin = new Padding(5, 0, 7, 0),
            SelectedDiameter = _eraserRadius * 2
        };
        _eraserSizePicker.SizeSelected += diameter =>
        {
            _eraserRadius = diameter / 2;
            statusLabel.Text = $"마스크 지우개 크기: {diameter}px";
            RefreshModernStatusDetails();
            pictureBox.Invalidate();
        };
        flow.Controls.Add(_eraserSizePicker);
        flow.Controls.Add(CreateRibbonSeparator());

        flow.Controls.Add(CreateRibbonButton("설정", ModernIcon.Settings, () =>
        {
            MenuAutoSettings_Click(this, EventArgs.Empty);
            return Task.CompletedTask;
        }));
        flow.Controls.Add(CreateRibbonButton("도움말", ModernIcon.Help, () =>
        {
            ShowModernHelp();
            return Task.CompletedTask;
        }));

        ribbon.Controls.Add(flow);
        Controls.Add(ribbon);
        _modernRibbon = ribbon;
        UpdateModernToolSelection();
    }

    private ModernRibbonButton CreateRibbonButton(string text, ModernIcon icon, Func<Task> action, Color? accent = null)
    {
        var button = new ModernRibbonButton
        {
            Text = text,
            IconKind = icon,
            AccentColor = accent ?? UiBlue,
            Width = text == "다른 이름으로" ? 96 : 86,
            Height = 96,
            Margin = new Padding(2, 0, 2, 0)
        };
        button.Click += async (_, _) => await action();
        return button;
    }

    private static Control CreateRibbonSeparator() => new Panel
    {
        Width = 1,
        Height = 76,
        BackColor = UiBorder,
        Margin = new Padding(10, 8, 10, 0)
    };

    private void SaveAsModern()
    {
        if (_originalBitmap == null)
        {
            MessageBox.Show("저장할 이미지가 없습니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        string initialName = string.IsNullOrWhiteSpace(_currentFilePath)
            ? "image.png"
            : Path.GetFileName(_currentFilePath);
        using var dialog = new SaveFileDialog
        {
            Title = "다른 이름으로 저장",
            FileName = initialName,
            Filter = "PNG 이미지 (*.png)|*.png|JPEG 이미지 (*.jpg;*.jpeg)|*.jpg;*.jpeg|모든 파일 (*.*)|*.*",
            AddExtension = true,
            DefaultExt = "png"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            string ext = Path.GetExtension(dialog.FileName).ToLowerInvariant();
            ImageFormat format = ext is ".jpg" or ".jpeg" ? ImageFormat.Jpeg : ImageFormat.Png;
            _originalBitmap.Save(dialog.FileName, format);
            _currentFilePath = dialog.FileName;
            Text = $"이미지 모자이크 편집기 - {Path.GetFileName(dialog.FileName)}";
            statusLabel.Text = $"저장 완료: {Path.GetFileName(dialog.FileName)}";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"저장 중 오류가 발생했습니다:\n{ex.Message}", "저장 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void StyleWorkspace()
    {
        pictureBox.BackColor = CanvasBottom;
        pictureBox.Margin = Padding.Empty;
        if (_mainSplit != null)
        {
            _mainSplit.BackColor = UiBackground;
            _mainSplit.SplitterWidth = 8;
            _mainSplit.Panel1.BackColor = UiBackground;
            _mainSplit.Panel1.Padding = new Padding(12, 12, 6, 12);
            _mainSplit.Panel2.BackColor = UiBackground;
            _mainSplit.Panel2.Padding = new Padding(6, 12, 12, 12);
        }
        if (_folderHeader != null)
        {
            _folderHeader.BackColor = UiSurface;
            _folderHeader.ForeColor = UiText;
            _folderHeader.Font = new Font("Segoe UI Semibold", 11.5f, FontStyle.Bold);
            _folderHeader.Padding = new Padding(12, 0, 8, 0);
            _folderHeader.Text = "이미지 목록   0개";
        }
        if (_folderList != null)
        {
            _folderList.BackColor = UiSurface;
            _folderList.ForeColor = UiText;
            _folderList.Font = new Font("Segoe UI", 9.3f);
            _folderList.BorderStyle = BorderStyle.None;
            _folderList.TileSize = new Size(280, 90);
        }
    }

    private void StyleStatusStrip()
    {
        statusStrip.AutoSize = false;
        statusStrip.Height = 38;
        statusStrip.BackColor = UiSurface;
        statusStrip.ForeColor = UiText;
        statusStrip.SizingGrip = false;
        statusStrip.Padding = new Padding(12, 0, 12, 0);
        statusStrip.Renderer = new ToolStripProfessionalRenderer(new ModernColorTable());

        statusLabel.Spring = true;
        statusLabel.TextAlign = ContentAlignment.MiddleLeft;
        statusLabel.ForeColor = UiText;
        statusLabel.Font = new Font("Segoe UI", 9f);

        _modeStatusLabel = CreateStatusChip("모드: 선택");
        _eraserStatusLabel = CreateStatusChip($"지우개 크기: {_eraserRadius * 2}px");
        _imageSizeStatusLabel = CreateStatusChip("---- × ----");
        _gpuStatusLabel = new ToolStripStatusLabel("GPU: 자동 감지")
        {
            AutoSize = true,
            ForeColor = Color.FromArgb(24, 153, 70),
            Font = new Font("Segoe UI Semibold", 9.2f, FontStyle.Bold),
            Margin = new Padding(12, 0, 2, 0),
            Padding = new Padding(8, 0, 2, 0)
        };

        statusStrip.Items.Add(_modeStatusLabel);
        statusStrip.Items.Add(_eraserStatusLabel);
        statusStrip.Items.Add(_imageSizeStatusLabel);
        statusStrip.Items.Add(_gpuStatusLabel);
    }

    private static ToolStripStatusLabel CreateStatusChip(string text) => new(text)
    {
        AutoSize = true,
        ForeColor = UiText,
        Font = new Font("Segoe UI", 9f),
        Margin = new Padding(6, 0, 6, 0),
        Padding = new Padding(10, 0, 10, 0),
        BorderSides = ToolStripStatusLabelBorderSides.Left,
        BorderStyle = Border3DStyle.Etched
    };

    private void LayoutModernChrome()
    {
        if (_modernRibbon == null || _modernRibbon.IsDisposed) return;
        _modernRibbon.SetBounds(0, menuStrip.Bottom, Math.Max(1, ClientSize.Width), 118);
        _modernRibbon.BringToFront();
        menuStrip.BringToFront();
        statusStrip.BringToFront();
        LayoutFolderWorkspace();
    }

    private int GetModernWorkspaceTop() => _modernRibbon is { IsDisposed: false } ? _modernRibbon.Bottom : menuStrip.Bottom;

    private void LoadEmptyStateArtwork()
    {
        try
        {
            string path = Path.Combine(AppContext.BaseDirectory, "assets", "app_icon_256.png");
            if (File.Exists(path))
            {
                using var temp = Image.FromFile(path);
                _emptyStateArtwork = new Bitmap(temp);
            }
        }
        catch
        {
            _emptyStateArtwork = null;
        }

        FormClosed += (_, _) =>
        {
            _emptyStateArtwork?.Dispose();
            _emptyStateArtwork = null;
        };
    }

    private void ModernCanvas_Paint(object? sender, PaintEventArgs e)
    {
        if (_originalBitmap != null) return;
        Rectangle bounds = pictureBox.ClientRectangle;
        if (bounds.Width < 160 || bounds.Height < 160) return;

        using var gradient = new LinearGradientBrush(bounds, CanvasTop, CanvasBottom, 90f);
        e.Graphics.FillRectangle(gradient, bounds);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;

        int boxWidth = Math.Min(760, Math.Max(420, bounds.Width - 160));
        int boxHeight = Math.Min(470, Math.Max(300, bounds.Height - 150));
        var dropRect = new Rectangle((bounds.Width - boxWidth) / 2, (bounds.Height - boxHeight) / 2, boxWidth, boxHeight);
        using GraphicsPath path = CreateRoundedRect(dropRect, 20);
        using var dashPen = new Pen(Color.FromArgb(175, 206, 215, 227), 2f)
        {
            DashStyle = DashStyle.Dash,
            DashPattern = new[] { 6f, 5f }
        };
        e.Graphics.DrawPath(dashPen, path);

        int cx = dropRect.Left + dropRect.Width / 2;
        int artworkSize = Math.Min(128, Math.Max(88, dropRect.Height / 3));
        int artworkTop = dropRect.Top + 54;
        if (_emptyStateArtwork != null)
        {
            var artRect = new Rectangle(cx - artworkSize / 2, artworkTop, artworkSize, artworkSize);
            e.Graphics.DrawImage(_emptyStateArtwork, artRect);
        }
        else
        {
            DrawDropImageIcon(e.Graphics, new Rectangle(cx - 38, artworkTop + 20, 76, 60));
        }

        using var titleFont = new Font("Segoe UI Semibold", 17f, FontStyle.Bold);
        using var subFont = new Font("Segoe UI", 10.8f);
        using var titleBrush = new SolidBrush(Color.White);
        using var subBrush = new SolidBrush(Color.FromArgb(214, 226, 236, 246));
        string title = "이미지 또는 폴더를 드래그 해주세요";
        string sub1 = "지원 형식: JPG, PNG, WEBP, BMP";
        string sub2 = "(폴더 드래그시 전체 자동처리됩니다)";
        SizeF titleSize = e.Graphics.MeasureString(title, titleFont);
        SizeF sub1Size = e.Graphics.MeasureString(sub1, subFont);
        SizeF sub2Size = e.Graphics.MeasureString(sub2, subFont);
        float titleY = artworkTop + artworkSize + 30;
        e.Graphics.DrawString(title, titleFont, titleBrush, cx - titleSize.Width / 2f, titleY);
        e.Graphics.DrawString(sub1, subFont, subBrush, cx - sub1Size.Width / 2f, titleY + 48);
        e.Graphics.DrawString(sub2, subFont, subBrush, cx - sub2Size.Width / 2f, titleY + 76);
    }

    private static void DrawDropImageIcon(Graphics g, Rectangle rect)
    {
        using var pen = new Pen(Color.FromArgb(180, 220, 228, 238), 3f);
        using var fill = new SolidBrush(Color.FromArgb(26, 255, 255, 255));
        using GraphicsPath path = CreateRoundedRect(rect, 8);
        g.FillPath(fill, path);
        g.DrawPath(pen, path);
        g.DrawEllipse(pen, rect.Left + 12, rect.Top + 10, 9, 9);
        g.DrawLines(pen, new[]
        {
            new Point(rect.Left + 11, rect.Bottom - 12),
            new Point(rect.Left + 27, rect.Top + 29),
            new Point(rect.Left + 40, rect.Bottom - 21),
            new Point(rect.Left + 53, rect.Top + 24),
            new Point(rect.Right - 10, rect.Bottom - 12)
        });
    }

    private static GraphicsPath CreateRoundedRect(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        int d = Math.Max(2, radius * 2);
        var arc = new Rectangle(rect.X, rect.Y, d, d);
        path.AddArc(arc, 180, 90);
        arc.X = rect.Right - d; path.AddArc(arc, 270, 90);
        arc.Y = rect.Bottom - d; path.AddArc(arc, 0, 90);
        arc.X = rect.Left; path.AddArc(arc, 90, 90);
        path.CloseFigure();
        return path;
    }

    private void UpdateModernToolSelection()
    {
        if (_selectionRibbonButton != null)
            _selectionRibbonButton.Selected = _editToolMode == EditToolMode.MosaicSelection;
        if (_eraserRibbonButton != null)
            _eraserRibbonButton.Selected = _editToolMode == EditToolMode.MaskEraser;
        RefreshModernStatusDetails();
    }

    private void RefreshModernStatusDetails()
    {
        if (_modeStatusLabel != null)
            _modeStatusLabel.Text = _editToolMode == EditToolMode.MaskEraser ? "모드: 지우개" : "모드: 선택";
        if (_eraserStatusLabel != null)
            _eraserStatusLabel.Text = $"지우개 크기: {_eraserRadius * 2}px";
        if (_imageSizeStatusLabel != null)
            _imageSizeStatusLabel.Text = _originalBitmap == null ? "---- × ----" : $"{_originalBitmap.Width} × {_originalBitmap.Height}";
        if (_eraserSizePicker != null)
            _eraserSizePicker.SelectedDiameter = _eraserRadius * 2;
    }

    private void UpdateGpuStatus(string? provider)
    {
        if (_gpuStatusLabel == null) return;
        if (string.IsNullOrWhiteSpace(provider))
        {
            _gpuStatusLabel.Text = "GPU: 자동 감지";
            _gpuStatusLabel.ForeColor = UiMuted;
            return;
        }
        bool cuda = provider.Contains("CUDA", StringComparison.OrdinalIgnoreCase) || provider.Contains("GPU", StringComparison.OrdinalIgnoreCase);
        _gpuStatusLabel.Text = cuda ? "GPU: 사용 가능 (CUDA)" : "GPU: CPU fallback";
        _gpuStatusLabel.ForeColor = cuda ? Color.FromArgb(24, 153, 70) : Color.FromArgb(211, 121, 15);
    }

    private void ShowModernHelp()
    {
        MessageBox.Show(
            "드래그 앤 드롭: 이미지/폴더 가져오기\n" +
            "Ctrl+Shift+A: 현재 이미지를 원본에서 다시 자동 처리\n" +
            "Ctrl+E: 마스크 지우개\n" +
            "Ctrl+M: 수동 모자이크 선택\n" +
            "Ctrl+Z / Ctrl+Y: 실행 취소 / 다시 실행",
            "Image Mosaic Editor 도움말", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private sealed class ModernColorTable : ProfessionalColorTable
    {
        public override Color MenuStripGradientBegin => UiSurface;
        public override Color MenuStripGradientEnd => UiSurface;
        public override Color ToolStripGradientBegin => UiSurface;
        public override Color ToolStripGradientEnd => UiSurface;
        public override Color ToolStripBorder => UiBorder;
        public override Color MenuItemSelected => UiBlueSoft;
        public override Color MenuItemBorder => Color.FromArgb(177, 210, 249);
        public override Color MenuItemPressedGradientBegin => UiBlueSoft;
        public override Color MenuItemPressedGradientEnd => UiBlueSoft;
        public override Color ImageMarginGradientBegin => UiSurface;
        public override Color ImageMarginGradientMiddle => UiSurface;
        public override Color ImageMarginGradientEnd => UiSurface;
    }
}

internal enum ModernIcon
{
    Open,
    Folder,
    Save,
    SaveAs,
    Undo,
    Redo,
    Magic,
    Pointer,
    Eraser,
    Settings,
    Help
}

internal sealed class ModernRibbonButton : Control
{
    private bool _hovered;
    private bool _pressed;
    private bool _selected;
    public ModernIcon IconKind { get; set; }
    public Color AccentColor { get; set; } = Color.FromArgb(25, 122, 234);
    public bool Selected { get => _selected; set { _selected = value; Invalidate(); } }

    public ModernRibbonButton()
    {
        DoubleBuffered = true;
        Cursor = Cursors.Hand;
        Font = new Font("Segoe UI", 9.2f);
        ForeColor = Color.FromArgb(18, 38, 72);
        BackColor = Color.White;
        SetStyle(ControlStyles.Selectable, true);
    }

    protected override void OnMouseEnter(EventArgs e) { _hovered = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _hovered = false; _pressed = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnMouseDown(MouseEventArgs e) { if (e.Button == MouseButtons.Left) { _pressed = true; Invalidate(); } base.OnMouseDown(e); }
    protected override void OnMouseUp(MouseEventArgs e) { _pressed = false; Invalidate(); base.OnMouseUp(e); }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var card = new Rectangle(2, 2, Width - 5, Height - 5);
        Color bg = Selected ? Color.FromArgb(230, 242, 255)
            : _pressed ? Color.FromArgb(232, 239, 248)
            : _hovered ? Color.FromArgb(246, 249, 253)
            : Color.White;
        using GraphicsPath cardPath = RoundRect(card, 9);
        using var cardBrush = new SolidBrush(bg);
        e.Graphics.FillPath(cardBrush, cardPath);
        if (Selected)
        {
            using var border = new Pen(Color.FromArgb(39, 134, 241), 1.5f);
            e.Graphics.DrawPath(border, cardPath);
        }

        var iconRect = new Rectangle((Width - 42) / 2, 10, 42, 42);
        DrawIcon(e.Graphics, iconRect, AccentColor, IconKind);
        TextRenderer.DrawText(e.Graphics, Text, Font, new Rectangle(3, 59, Width - 6, 28), ForeColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }

    private static void DrawIcon(Graphics g, Rectangle r, Color c, ModernIcon kind)
    {
        using var pen = new Pen(c, 2.8f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
        using var fill = new SolidBrush(Color.FromArgb(42, c));
        switch (kind)
        {
            case ModernIcon.Open:
            case ModernIcon.Folder:
                g.FillRectangle(fill, r.X + 4, r.Y + 15, r.Width - 8, r.Height - 17);
                g.DrawRectangle(pen, r.X + 4, r.Y + 15, r.Width - 8, r.Height - 18);
                g.DrawLines(pen, new[] { new Point(r.X + 5, r.Y + 15), new Point(r.X + 14, r.Y + 15), new Point(r.X + 18, r.Y + 9), new Point(r.Right - 6, r.Y + 9) });
                if (kind == ModernIcon.Folder)
                {
                    using var green = new Pen(Color.FromArgb(25, 167, 85), 2.5f);
                    g.DrawEllipse(green, r.Right - 14, r.Bottom - 14, 12, 12);
                    g.DrawLine(green, r.Right - 8, r.Bottom - 12, r.Right - 8, r.Bottom - 4);
                    g.DrawLine(green, r.Right - 12, r.Bottom - 8, r.Right - 4, r.Bottom - 8);
                }
                break;
            case ModernIcon.Save:
            case ModernIcon.SaveAs:
                g.FillRectangle(fill, r.X + 7, r.Y + 4, r.Width - 14, r.Height - 8);
                g.DrawRectangle(pen, r.X + 7, r.Y + 4, r.Width - 14, r.Height - 8);
                g.DrawRectangle(pen, r.X + 12, r.Y + 7, r.Width - 24, 11);
                g.DrawRectangle(pen, r.X + 12, r.Bottom - 16, r.Width - 24, 11);
                if (kind == ModernIcon.SaveAs)
                {
                    g.DrawEllipse(pen, r.Right - 15, r.Bottom - 15, 13, 13);
                    g.DrawLine(pen, r.Right - 8, r.Bottom - 12, r.Right - 8, r.Bottom - 4);
                    g.DrawLine(pen, r.Right - 12, r.Bottom - 8, r.Right - 4, r.Bottom - 8);
                }
                break;
            case ModernIcon.Undo:
            case ModernIcon.Redo:
                bool redo = kind == ModernIcon.Redo;
                g.DrawArc(pen, r.X + 6, r.Y + 8, r.Width - 12, r.Height - 15, redo ? 205 : -25, 210);
                float ax = redo ? r.Right - 7 : r.X + 7;
                float dir = redo ? -1 : 1;
                g.DrawLine(pen, ax, r.Y + 11, ax + 7 * dir, r.Y + 4);
                g.DrawLine(pen, ax, r.Y + 11, ax + 8 * dir, r.Y + 18);
                break;
            case ModernIcon.Magic:
                g.DrawLine(pen, r.X + 9, r.Bottom - 5, r.Right - 9, r.Y + 9);
                DrawSpark(g, pen, r.Right - 10, r.Y + 7, 5);
                DrawSpark(g, pen, r.X + 9, r.Y + 14, 4);
                break;
            case ModernIcon.Pointer:
                using (var body = new SolidBrush(Color.FromArgb(244, 248, 255)))
                {
                    PointF[] pts =
                    [
                        new(r.X + 8, r.Y + 5), new(r.X + 12, r.Bottom - 7), new(r.X + 20, r.Bottom - 15),
                        new(r.X + 27, r.Bottom - 3), new(r.X + 33, r.Bottom - 7), new(r.X + 25, r.Bottom - 19)
                    ];
                    g.FillPolygon(body, pts);
                    g.DrawPolygon(pen, pts);
                }
                break;
            case ModernIcon.Eraser:
                using (var eraserFill = new SolidBrush(Color.FromArgb(64, 139, 205)))
                {
                    Point[] pts =
                    [
                        new(r.X + 8, r.Y + 26), new(r.X + 24, r.Y + 9), new(r.X + 35, r.Y + 20),
                        new(r.X + 20, r.Y + 35), new(r.X + 9, r.Y + 35)
                    ];
                    g.FillPolygon(eraserFill, pts);
                    g.DrawPolygon(pen, pts);
                }
                break;
            case ModernIcon.Settings:
                g.DrawEllipse(pen, r.X + 9, r.Y + 9, 24, 24);
                g.DrawEllipse(pen, r.X + 17, r.Y + 17, 8, 8);
                for (int i = 0; i < 8; i++)
                {
                    double a = i * Math.PI / 4;
                    g.DrawLine(pen,
                        r.X + 21 + (float)Math.Cos(a) * 13, r.Y + 21 + (float)Math.Sin(a) * 13,
                        r.X + 21 + (float)Math.Cos(a) * 18, r.Y + 21 + (float)Math.Sin(a) * 18);
                }
                break;
            case ModernIcon.Help:
                g.DrawEllipse(pen, r.X + 5, r.Y + 5, r.Width - 10, r.Height - 10);
                using (var font = new Font("Segoe UI Semibold", 19f, FontStyle.Bold))
                    TextRenderer.DrawText(g, "?", font, r, c, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                break;
        }
    }

    private static void DrawSpark(Graphics g, Pen pen, int x, int y, int radius)
    {
        g.DrawLine(pen, x - radius, y, x + radius, y);
        g.DrawLine(pen, x, y - radius, x, y + radius);
    }

    private static GraphicsPath RoundRect(Rectangle rect, int radius)
    {
        var p = new GraphicsPath();
        int d = radius * 2;
        p.AddArc(rect.X, rect.Y, d, d, 180, 90);
        p.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
        p.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        p.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
        p.CloseFigure();
        return p;
    }
}

internal sealed class EraserSizePicker : Control
{
    private readonly int[] _sizes = [32, 64, 128, 192];
    private int _selectedDiameter = 64;
    public event Action<int>? SizeSelected;

    public int SelectedDiameter
    {
        get => _selectedDiameter;
        set
        {
            if (_selectedDiameter == value) return;
            _selectedDiameter = value;
            Invalidate();
        }
    }

    public EraserSizePicker()
    {
        DoubleBuffered = true;
        Cursor = Cursors.Hand;
        BackColor = Color.White;
        ForeColor = Color.FromArgb(18, 38, 72);
        Font = new Font("Segoe UI", 8.7f);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (e.Button != MouseButtons.Left) return;
        int index = Math.Clamp((e.X - 4) / Math.Max(1, (Width - 8) / 4), 0, 3);
        SelectedDiameter = _sizes[index];
        SizeSelected?.Invoke(SelectedDiameter);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        int cell = (Width - 8) / 4;
        for (int i = 0; i < _sizes.Length; i++)
        {
            int centerX = 4 + i * cell + cell / 2;
            int diameter = 12 + i * 5;
            int y = 26 - diameter / 2;
            Color fillColor = _sizes[i] == SelectedDiameter ? Color.FromArgb(52, 76, 108) : Color.FromArgb(229, 235, 244);
            Color outlineColor = _sizes[i] == SelectedDiameter ? Color.FromArgb(31, 65, 108) : Color.FromArgb(91, 119, 156);
            using var brush = new SolidBrush(fillColor);
            using var pen = new Pen(outlineColor, 1.6f);
            e.Graphics.FillEllipse(brush, centerX - diameter / 2, y, diameter, diameter);
            e.Graphics.DrawEllipse(pen, centerX - diameter / 2, y, diameter, diameter);
            TextRenderer.DrawText(e.Graphics, _sizes[i].ToString(), Font,
                new Rectangle(centerX - cell / 2, 49, cell, 20), ForeColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        using var captionFont = new Font("Segoe UI", 8.8f);
        TextRenderer.DrawText(e.Graphics, "지우개 크기", captionFont,
            new Rectangle(0, 71, Width, 18), ForeColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }
}
