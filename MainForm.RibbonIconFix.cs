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
            Size = new Size(50, 44),
            Location = new Point((button.Width - 50) / 2, 5),
            Anchor = AnchorStyles.Top,
            Enabled = false,
            TabStop = false
        };

        button.Controls.Add(overlay);
        overlay.BringToFront();

        button.MouseEnter += (_, _) => overlay.Invalidate();
        button.MouseLeave += (_, _) => overlay.Invalidate();
        button.MouseDown += (_, _) => overlay.Invalidate();
        button.MouseUp += (_, _) => overlay.Invalidate();
        button.Invalidated += (_, _) => overlay.Invalidate();
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
    private static readonly Color DarkBlue = Color.FromArgb(22, 82, 155);

    public RibbonIconOverlay(RibbonOverlayIcon kind)
    {
        _kind = kind;
        SetStyle(ControlStyles.UserPaint |
                 ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer, true);
    }

    protected override void OnPaintBackground(PaintEventArgs pevent)
    {
        // Paint the complete icon slot so the old built-in icon can never show
        // through underneath the corrected icon.
        pevent.Graphics.Clear(GetParentCardColor());
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(GetParentCardColor());
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

        if (_kind == RibbonOverlayIcon.Pointer)
            DrawPointer(e.Graphics);
        else
            DrawHelp(e.Graphics);
    }

    private Color GetParentCardColor()
    {
        if (Parent is not ModernRibbonButton button)
            return Color.White;

        if (button.Selected)
            return Color.FromArgb(226, 240, 255);

        bool hovered = button.ClientRectangle.Contains(button.PointToClient(Cursor.Position));
        return hovered ? Color.FromArgb(244, 248, 253) : Color.White;
    }

    private static void DrawPointer(Graphics g)
    {
        // Windows-style cursor arrow: white body, crisp blue outline and a small
        // accent ring. This stays recognizable even when the ribbon is scaled.
        PointF[] arrow =
        [
            new(10f, 5f),
            new(32f, 23f),
            new(22f, 24f),
            new(28f, 35f),
            new(22f, 38f),
            new(16f, 27f),
            new(10f, 34f)
        ];

        using var path = new GraphicsPath();
        path.AddPolygon(arrow);
        using var shadow = new SolidBrush(Color.FromArgb(36, 24, 64, 120));
        using var body = new SolidBrush(Color.White);
        using var outline = new Pen(Blue, 2.25f) { LineJoin = LineJoin.Round };

        g.TranslateTransform(1.2f, 1.4f);
        g.FillPath(shadow, path);
        g.ResetTransform();
        g.FillPath(body, path);
        g.DrawPath(outline, path);

        using var accent = new Pen(Blue, 2f);
        using var accentFill = new SolidBrush(Color.FromArgb(228, 241, 255));
        g.FillEllipse(accentFill, 32f, 7f, 10f, 10f);
        g.DrawEllipse(accent, 32f, 7f, 10f, 10f);
    }

    private static void DrawHelp(Graphics g)
    {
        var circle = new Rectangle(8, 4, 34, 34);
        using var fill = new SolidBrush(Color.FromArgb(232, 242, 255));
        using var outline = new Pen(Blue, 2.5f);
        g.FillEllipse(fill, circle);
        g.DrawEllipse(outline, circle);

        using var font = new Font("Segoe UI Semibold", 17f, FontStyle.Bold, GraphicsUnit.Point);
        TextRenderer.DrawText(
            g,
            "?",
            font,
            new Rectangle(circle.X, circle.Y - 1, circle.Width, circle.Height + 1),
            DarkBlue,
            TextFormatFlags.HorizontalCenter |
            TextFormatFlags.VerticalCenter |
            TextFormatFlags.NoPadding);
    }
}
