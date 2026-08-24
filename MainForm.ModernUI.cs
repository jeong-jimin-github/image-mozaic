using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ImageMosaicEditor;

public partial class MainForm
{
    private Panel? _modernRibbon;
    private ToolStripStatusLabel? _gpuStatusLabel;
    private ModernRibbonButton? _selectionRibbonButton;
    private ModernRibbonButton? _eraserRibbonButton;

    private static readonly Color UiBackground = Color.FromArgb(244, 247, 251);
    private static readonly Color UiSurface = Color.White;
    private static readonly Color UiText = Color.FromArgb(25, 39, 66);
    private static readonly Color UiBlue = Color.FromArgb(28, 123, 235);
    private static readonly Color UiBlueSoft = Color.FromArgb(229, 241, 255);
    private static readonly Color CanvasTop = Color.FromArgb(82, 88, 99);
    private static readonly Color CanvasBottom = Color.FromArgb(62, 67, 77);

    private void InitializeModernUi()
    {
        SuspendLayout();
        try
        {
            BackColor = UiBackground;
            Font = new Font("Segoe UI", 9.5f);
            MinimumSize = new Size(980, 680);
            ClientSize = new Size(1280, 820);

            StyleMenuStrip();
            AddModernTopMenus();
            CreateModernRibbon();
            StyleWorkspace();
            StyleStatusStrip();

            pictureBox.Paint += ModernCanvas_Paint;
            pictureBox.Resize += (_, _) => ApplyRoundedRegion(pictureBox, 16);
            Shown += (_, _) =>
            {
                LayoutModernChrome();
                ApplyRoundedRegion(pictureBox, 16);
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
        menuStrip.Padding = new Padding(18, 4, 8, 4);
        menuStrip.BackColor = UiSurface;
        menuStrip.ForeColor = UiText;
        menuStrip.Font = new Font("Segoe UI", 10f);
        menuStrip.Renderer = new ToolStripProfessionalRenderer(new ModernColorTable());
        foreach (ToolStripItem item in menuStrip.Items)
        {
            item.Margin = new Padding(2, 0, 8, 0);
            item.Padding = new Padding(8, 0, 8, 0);
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
            Height = 108,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
        var flow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoScroll = true,
            Padding = new Padding(16, 10, 10, 8),
            BackColor = UiSurface
        };

        flow.Controls.Add(CreateRibbonButton("열기", ModernIcon.Open, async () =>
        {
            MenuOpenAndAuto_Click(this, EventArgs.Empty);
            await Task.CompletedTask;
        }, Color.FromArgb(245, 158, 11)));
        flow.Controls.Add(CreateRibbonButton("폴더 열기", ModernIcon.Folder, async () =>
        {
            MenuAutoBatch_Click(this, EventArgs.Empty);
            await Task.CompletedTask;
        }, Color.FromArgb(245, 158, 11)));
        flow.Controls.Add(CreateRibbonSeparator());
        flow.Controls.Add(CreateRibbonButton("저장", ModernIcon.Save, () => { MenuSave_Click(this, EventArgs.Empty); return Task.CompletedTask; }));
        flow.Controls.Add(CreateRibbonSeparator());
        flow.Controls.Add(CreateRibbonButton("실행 취소", ModernIcon.Undo, () => { MenuUndo_Click(this, EventArgs.Empty); return Task.CompletedTask; }, Color.FromArgb(130, 140, 156)));
        flow.Controls.Add(CreateRibbonButton("다시 실행", ModernIcon.Redo, () => { MenuRedo_Click(this, EventArgs.Empty); return Task.CompletedTask; }, Color.FromArgb(130, 140, 156)));
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
        flow.Controls.Add(CreateRibbonButton("설정", ModernIcon.Settings, () => { MenuAutoSettings_Click(this, EventArgs.Empty); return Task.CompletedTask; }));
        flow.Controls.Add(CreateRibbonButton("도움말", ModernIcon.Help, () => { ShowModernHelp(); return Task.CompletedTask; }));

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
            Width = 88,
            Height = 82,
            Margin = new Padding(3, 0, 3, 0)
        };
        button.Click += async (_, _) => await action();
        return button;
    }

    private static Control CreateRibbonSeparator() => new Panel
    {
        Width = 1,
        Height = 64,
        BackColor = Color.FromArgb(228, 233, 240),
        Margin = new Padding(8, 8, 8, 0)
    };

    private void StyleWorkspace()
    {
        pictureBox.BackColor = CanvasBottom;
        pictureBox.Margin = Padding.Empty;
        if (_mainSplit != null)
        {
            _mainSplit.BackColor = UiBackground;
            _mainSplit.SplitterWidth = 8;
            _mainSplit.Panel1.BackColor = UiBackground;
            _mainSplit.Panel1.Padding = new Padding(14, 14, 7, 14);
            _mainSplit.Panel2.BackColor = UiBackground;
            _mainSplit.Panel2.Padding = new Padding(7, 14, 14, 14);
        }
        if (_folderHeader != null)
        {
            _folderHeader.BackColor = UiSurface;
            _folderHeader.ForeColor = UiText;
            _folderHeader.Font = new Font("Segoe UI Semibold", 11f, FontStyle.Bold);
            _folderHeader.Padding = new Padding(14, 0, 10, 0);
            _folderHeader.Text = "이미지 목록  ·  0개";
        }
        if (_folderList != null)
        {
            _folderList.BackColor = UiSurface;
            _folderList.ForeColor = UiText;
            _folderList.Font = new Font("Segoe UI", 9.2f);
            _folderList.BorderStyle = BorderStyle.None;
            _folderList.TileSize = new Size(270, 88);
        }
    }

    private void StyleStatusStrip()
    {
        statusStrip.AutoSize = false;
        statusStrip.Height = 34;
        statusStrip.BackColor = UiSurface;
        statusStrip.ForeColor = UiText;
        statusStrip.SizingGrip = false;
        statusStrip.Padding = new Padding(10, 0, 10, 0);
        statusStrip.Renderer = new ToolStripProfessionalRenderer(new ModernColorTable());
        statusLabel.Spring = true;
        statusLabel.TextAlign = ContentAlignment.MiddleLeft;
        statusLabel.ForeColor = UiText;

        _gpuStatusLabel = new ToolStripStatusLabel("GPU: 자동 감지")
        {
            AutoSize = true,
            ForeColor = Color.FromArgb(22, 163, 74),
            Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold),
            Margin = new Padding(14, 0, 2, 0)
        };
        statusStrip.Items.Add(_gpuStatusLabel);
    }

    private void LayoutModernChrome()
    {
        if (_modernRibbon == null || _modernRibbon.IsDisposed) return;
        _modernRibbon.SetBounds(0, menuStrip.Bottom, Math.Max(1, ClientSize.Width), 108);
        _modernRibbon.BringToFront();
        menuStrip.BringToFront();
        statusStrip.BringToFront();
        LayoutFolderWorkspace();
    }

    private int GetModernWorkspaceTop() => _modernRibbon is { IsDisposed: false } ? _modernRibbon.Bottom : menuStrip.Bottom;

    private void ModernCanvas_Paint(object? sender, PaintEventArgs e)
    {
        if (_originalBitmap != null) return;
        Rectangle bounds = pictureBox.ClientRectangle;
        if (bounds.Width < 120 || bounds.Height < 120) return;

        using var gradient = new LinearGradientBrush(bounds, CanvasTop, CanvasBottom, 90f);
        e.Graphics.FillRectangle(gradient, bounds);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        int boxWidth = Math.Min(620, Math.Max(300, bounds.Width - 120));
        int boxHeight = Math.Min(360, Math.Max(220, bounds.Height - 120));
        var dropRect = new Rectangle((bounds.Width - boxWidth) / 2, (bounds.Height - boxHeight) / 2, boxWidth, boxHeight);
        using GraphicsPath path = CreateRoundedRect(dropRect, 24);
        using var dashPen = new Pen(Color.FromArgb(150, 205, 214, 226), 2f) { DashStyle = DashStyle.Dash, DashPattern = new[] { 6f, 5f } };
        e.Graphics.DrawPath(dashPen, path);

        int cx = dropRect.Left + dropRect.Width / 2;
        int iconY = dropRect.Top + Math.Max(42, dropRect.Height / 4);
        DrawDropImageIcon(e.Graphics, new Rectangle(cx - 34, iconY - 25, 68, 54));

        using var titleFont = new Font("Segoe UI Semibold", 16f, FontStyle.Bold);
        using var subFont = new Font("Segoe UI", 10.5f);
        using var titleBrush = new SolidBrush(Color.White);
        using var subBrush = new SolidBrush(Color.FromArgb(205, 221, 230, 241));
        string title = "이미지 또는 폴더를 드래그해 놓으세요";
        string sub = "JPG · PNG · WEBP  /  폴더 드롭 시 전체 자동처리";
        SizeF titleSize = e.Graphics.MeasureString(title, titleFont);
        SizeF subSize = e.Graphics.MeasureString(sub, subFont);
        float titleY = iconY + 56;
        e.Graphics.DrawString(title, titleFont, titleBrush, cx - titleSize.Width / 2f, titleY);
        e.Graphics.DrawString(sub, subFont, subBrush, cx - subSize.Width / 2f, titleY + 40);
    }

    private static void DrawDropImageIcon(Graphics g, Rectangle rect)
    {
        using var pen = new Pen(Color.FromArgb(170, 219, 228, 238), 3f);
        using var fill = new SolidBrush(Color.FromArgb(32, 255, 255, 255));
        using GraphicsPath path = CreateRoundedRect(rect, 8);
        g.FillPath(fill, path);
        g.DrawPath(pen, path);
        g.DrawEllipse(pen, rect.Left + 12, rect.Top + 10, 9, 9);
        g.DrawLines(pen, new[]
        {
            new Point(rect.Left + 11, rect.Bottom - 12),
            new Point(rect.Left + 27, rect.Top + 27),
            new Point(rect.Left + 38, rect.Bottom - 20),
            new Point(rect.Left + 49, rect.Top + 23),
            new Point(rect.Right - 10, rect.Bottom - 12)
        });
    }

    private static void ApplyRoundedRegion(Control control, int radius)
    {
        if (control.Width <= 0 || control.Height <= 0) return;
        using GraphicsPath path = CreateRoundedRect(new Rectangle(0, 0, control.Width, control.Height), radius);
        Region? old = control.Region;
        control.Region = new Region(path);
        old?.Dispose();
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
        if (_selectionRibbonButton != null) _selectionRibbonButton.Selected = _editToolMode == EditToolMode.MosaicSelection;
        if (_eraserRibbonButton != null) _eraserRibbonButton.Selected = _editToolMode == EditToolMode.MaskEraser;
    }

    private void UpdateGpuStatus(string? provider)
    {
        if (_gpuStatusLabel == null) return;
        if (string.IsNullOrWhiteSpace(provider))
        {
            _gpuStatusLabel.Text = "GPU: 자동 감지";
            _gpuStatusLabel.ForeColor = Color.FromArgb(94, 109, 132);
            return;
        }
        bool cuda = provider.Contains("CUDA", StringComparison.OrdinalIgnoreCase) || provider.Contains("GPU", StringComparison.OrdinalIgnoreCase);
        _gpuStatusLabel.Text = cuda ? "GPU: CUDA 사용 중" : "GPU: CPU fallback";
        _gpuStatusLabel.ForeColor = cuda ? Color.FromArgb(22, 163, 74) : Color.FromArgb(217, 119, 6);
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
        public override Color ToolStripBorder => Color.FromArgb(228, 233, 240);
        public override Color MenuItemSelected => UiBlueSoft;
        public override Color MenuItemBorder => Color.FromArgb(178, 211, 250);
        public override Color MenuItemPressedGradientBegin => UiBlueSoft;
        public override Color MenuItemPressedGradientEnd => UiBlueSoft;
        public override Color ImageMarginGradientBegin => UiSurface;
        public override Color ImageMarginGradientMiddle => UiSurface;
        public override Color ImageMarginGradientEnd => UiSurface;
    }
}

internal enum ModernIcon { Open, Folder, Save, Undo, Redo, Magic, Pointer, Eraser, Settings, Help }

internal sealed class ModernRibbonButton : Control
{
    private bool _hovered;
    private bool _pressed;
    private bool _selected;
    public ModernIcon IconKind { get; set; }
    public Color AccentColor { get; set; } = Color.FromArgb(28, 123, 235);
    public bool Selected { get => _selected; set { _selected = value; Invalidate(); } }

    public ModernRibbonButton()
    {
        DoubleBuffered = true;
        Cursor = Cursors.Hand;
        Font = new Font("Segoe UI", 9f);
        ForeColor = Color.FromArgb(25, 39, 66);
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
        Color bg = Selected ? Color.FromArgb(226, 240, 255) : _pressed ? Color.FromArgb(234, 239, 247) : _hovered ? Color.FromArgb(244, 248, 253) : Color.White;
        using GraphicsPath cardPath = RoundRect(card, 10);
        using var cardBrush = new SolidBrush(bg);
        e.Graphics.FillPath(cardBrush, cardPath);
        if (Selected)
        {
            using var border = new Pen(Color.FromArgb(75, 154, 244), 1.4f);
            e.Graphics.DrawPath(border, cardPath);
        }

        var iconRect = new Rectangle((Width - 34) / 2, 11, 34, 34);
        DrawIcon(e.Graphics, iconRect, AccentColor, IconKind);
        TextRenderer.DrawText(e.Graphics, Text, Font, new Rectangle(4, 51, Width - 8, 24), ForeColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }

    private static void DrawIcon(Graphics g, Rectangle r, Color c, ModernIcon kind)
    {
        using var pen = new Pen(c, 2.6f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        using var fill = new SolidBrush(Color.FromArgb(38, c));
        switch (kind)
        {
            case ModernIcon.Open:
            case ModernIcon.Folder:
                g.FillRectangle(fill, r.X + 4, r.Y + 12, r.Width - 8, r.Height - 15);
                g.DrawRectangle(pen, r.X + 4, r.Y + 12, r.Width - 8, r.Height - 16);
                g.DrawLines(pen, new[] { new Point(r.X + 5, r.Y + 12), new Point(r.X + 13, r.Y + 12), new Point(r.X + 17, r.Y + 7), new Point(r.Right - 7, r.Y + 7) });
                if (kind == ModernIcon.Folder)
                {
                    g.DrawEllipse(pen, r.Right - 13, r.Bottom - 13, 11, 11);
                    g.DrawLine(pen, r.Right - 8, r.Bottom - 11, r.Right - 8, r.Bottom - 4);
                    g.DrawLine(pen, r.Right - 11, r.Bottom - 8, r.Right - 4, r.Bottom - 8);
                }
                break;
            case ModernIcon.Save:
                g.DrawRectangle(pen, r.X + 5, r.Y + 3, r.Width - 10, r.Height - 6);
                g.DrawRectangle(pen, r.X + 10, r.Y + 5, r.Width - 20, 9);
                g.DrawRectangle(pen, r.X + 10, r.Bottom - 13, r.Width - 20, 9);
                break;
            case ModernIcon.Undo:
            case ModernIcon.Redo:
                bool redo = kind == ModernIcon.Redo;
                g.DrawArc(pen, r.X + 7, r.Y + 8, r.Width - 14, r.Height - 13, redo ? 205 : -25, 210);
                float ax = redo ? r.Right - 7 : r.X + 7;
                float dir = redo ? -1 : 1;
                g.DrawLine(pen, ax, r.Y + 10, ax + 6 * dir, r.Y + 4);
                g.DrawLine(pen, ax, r.Y + 10, ax + 7 * dir, r.Y + 16);
                break;
            case ModernIcon.Magic:
                g.DrawLine(pen, r.X + 8, r.Bottom - 5, r.Right - 8, r.Y + 8);
                g.DrawLine(pen, r.Right - 10, r.Y + 3, r.Right - 10, r.Y + 11);
                g.DrawLine(pen, r.Right - 14, r.Y + 7, r.Right - 6, r.Y + 7);
                g.DrawLine(pen, r.X + 8, r.Y + 8, r.X + 8, r.Y + 16);
                g.DrawLine(pen, r.X + 4, r.Y + 12, r.X + 12, r.Y + 12);
                break;
            case ModernIcon.Pointer:
                g.DrawPolygon(pen, new[] { new Point(r.X + 7, r.Y + 4), new Point(r.X + 10, r.Bottom - 6), new Point(r.X + 17, r.Bottom - 13), new Point(r.X + 23, r.Bottom - 3), new Point(r.X + 28, r.Bottom - 6), new Point(r.X + 21, r.Bottom - 16) });
                break;
            case ModernIcon.Eraser:
                g.FillRectangle(fill, r.X + 8, r.Y + 11, 20, 14);
                g.DrawPolygon(pen, new[] { new Point(r.X + 8, r.Y + 20), new Point(r.X + 20, r.Y + 8), new Point(r.X + 29, r.Y + 17), new Point(r.X + 17, r.Y + 29), new Point(r.X + 9, r.Y + 29) });
                break;
            case ModernIcon.Settings:
                g.DrawEllipse(pen, r.X + 7, r.Y + 7, 20, 20);
                g.DrawEllipse(pen, r.X + 14, r.Y + 14, 6, 6);
                for (int i = 0; i < 8; i++)
                {
                    double a = i * Math.PI / 4;
                    g.DrawLine(pen, r.X + 17 + (float)Math.Cos(a) * 11, r.Y + 17 + (float)Math.Sin(a) * 11,
                        r.X + 17 + (float)Math.Cos(a) * 15, r.Y + 17 + (float)Math.Sin(a) * 15);
                }
                break;
            case ModernIcon.Help:
                g.DrawEllipse(pen, r.X + 4, r.Y + 4, r.Width - 8, r.Height - 8);
                using (var font = new Font("Segoe UI Semibold", 16f, FontStyle.Bold))
                    TextRenderer.DrawText(g, "?", font, r, c, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                break;
        }
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
