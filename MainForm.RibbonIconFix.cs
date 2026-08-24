using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ImageMosaicEditor;

public partial class MainForm
{
    private void ApplyRibbonIconFixes()
    {
        if (_selectionRibbonButton != null)
            AttachIconOverlay(_selectionRibbonButton, RibbonOverlayIcon.Pointer);

        if (_modernRibbon != null)
        {
            ModernRibbonButton? help = FindRibbonButton(_modernRibbon, "도움말");
            if (help != null)
                AttachIconOverlay(help, RibbonOverlayIcon.Help);
        }
    }

    private static ModernRibbonButton? FindRibbonButton(Control root, string text)
    {
        foreach (Control child in root.Controls)
        {
            if (child is ModernRibbonButton button && button.Text == text)
                return button;

            ModernRibbonButton? nested = FindRibbonButton(child, text);
            if (nested != null)
                return nested;
        }
        return null;
    }

    private static void AttachIconOverlay(ModernRibbonButton button, RibbonOverlayIcon kind)
    {
        foreach (Control child in button.Controls)
        {
            if (child is RibbonIconOverlay)
                return;
        }

        var overlay = new RibbonIconOverlay(kind)
        {
            Size = new Size(40, 40),
            Location = new Point((button.Width - 40) / 2, 8),
            Anchor = AnchorStyles.Top,
            Enabled = false,
            TabStop = false
        };
        button.Controls.Add(overlay);
        overlay.BringToFront();
    }
}

internal enum RibbonOverlayIcon
{
    Pointer,
    Help
}

internal sealed class RibbonIconOverlay : Control
{
    private readonly RibbonOverlayIcon _kind;
    private static readonly Color Blue = Color.FromArgb(28, 123, 235);
    private static readonly Color Dark = Color.FromArgb(24, 67, 125);

    public RibbonIconOverlay(RibbonOverlayIcon kind)
    {
        _kind = kind;
        BackColor = Color.Transparent;
        SetStyle(ControlStyles.UserPaint |
                 ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.SupportsTransparentBackColor, true);
    }

    protected override void OnPaintBackground(PaintEventArgs pevent)
    {
        // Preserve the parent ribbon card background.
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

        if (_kind == RibbonOverlayIcon.Pointer)
            DrawPointer(e.Graphics);
        else
            DrawHelp(e.Graphics);
    }

    private static void DrawPointer(Graphics g)
    {
        PointF[] arrow =
        [
            new(8f, 4f),
            new(31f, 22f),
            new(21.5f, 23.5f),
            new(27.5f, 34f),
            new(22f, 37f),
            new(16f, 26f),
            new(9f, 32f)
        ];

        using var path = new GraphicsPath();
        path.AddPolygon(arrow);
        using var shadow = new SolidBrush(Color.FromArgb(38, 29, 77, 142));
        using var fill = new SolidBrush(Blue);
        using var outline = new Pen(Dark, 1.8f) { LineJoin = LineJoin.Round };

        g.TranslateTransform(1.3f, 1.5f);
        g.FillPath(shadow, path);
        g.ResetTransform();
        g.FillPath(fill, path);
        g.DrawPath(outline, path);

        using var shine = new Pen(Color.FromArgb(190, 255, 255, 255), 1.4f)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round
        };
        g.DrawLine(shine, 11f, 9f, 22f, 18f);
    }

    private static void DrawHelp(Graphics g)
    {
        using var outerFill = new SolidBrush(Color.FromArgb(232, 242, 255));
        using var outer = new Pen(Blue, 2.4f);
        g.FillEllipse(outerFill, 4f, 4f, 32f, 32f);
        g.DrawEllipse(outer, 4f, 4f, 32f, 32f);

        using var mark = new Pen(Blue, 3.1f)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round
        };
        using var dot = new SolidBrush(Blue);

        using var hook = new GraphicsPath();
        hook.AddBezier(13f, 15f, 14f, 9.5f, 27f, 9.5f, 27f, 16f);
        hook.AddBezier(27f, 16f, 27f, 20f, 20f, 20f, 20f, 24f);
        g.DrawPath(mark, hook);
        g.FillEllipse(dot, 18.2f, 29f, 3.8f, 3.8f);
    }
}
