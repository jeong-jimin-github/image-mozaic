using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace ImageMosaicEditor;

public partial class MainForm
{
    private enum EditToolMode
    {
        MosaicSelection,
        MaskEraser
    }

    private EditToolMode _editToolMode = EditToolMode.MosaicSelection;
    private bool _eraserStrokeActive;
    private int _eraserRadius = 32;
    private Point _eraserCursorPoint;
    private bool _eraserCursorVisible;
    private ToolStripMenuItem? _eraserMenuItem;
    private ToolStripMenuItem? _selectionMenuItem;

    private void InitializeEraserTools()
    {
        _selectionMenuItem = new ToolStripMenuItem(L10n.T("모자이크 선택 모드(&M)"))
        {
            Name = "eraserSelectionMenu",
            ShortcutKeys = Keys.Control | Keys.M,
            Checked = true
        };
        _selectionMenuItem.Click += (_, _) => SetEditToolMode(EditToolMode.MosaicSelection);

        _eraserMenuItem = new ToolStripMenuItem(L10n.T("마스크 지우개 모드(&E)"))
        {
            Name = "eraserModeMenu",
            ShortcutKeys = Keys.Control | Keys.E
        };
        _eraserMenuItem.Click += (_, _) => SetEditToolMode(EditToolMode.MaskEraser);

        var eraserSizeMenu = new ToolStripMenuItem(L10n.T("지우개 크기")) { Name = "eraserSizeMenu" };
        foreach (int radius in new[] { 16, 32, 64, 96 })
        {
            var item = new ToolStripMenuItem($"{radius * 2}px") { Tag = radius };
            item.Click += (_, _) =>
            {
                _eraserRadius = radius;
                statusLabel.Text = L10n.F("마스크 지우개 크기: {0}px", radius * 2);
                pictureBox.Invalidate();
            };
            eraserSizeMenu.DropDownItems.Add(item);
        }

        editMenu.DropDownItems.Add(new ToolStripSeparator());
        editMenu.DropDownItems.Add(_selectionMenuItem);
        editMenu.DropDownItems.Add(_eraserMenuItem);
        editMenu.DropDownItems.Add(eraserSizeMenu);

        pictureBox.MouseDown += Eraser_MouseDown;
        pictureBox.MouseMove += Eraser_MouseMove;
        pictureBox.MouseUp += Eraser_MouseUp;
        pictureBox.MouseLeave += Eraser_MouseLeave;
        pictureBox.Paint += Eraser_Paint;
    }

    private void SetEditToolMode(EditToolMode mode)
    {
        _editToolMode = mode;
        _isDragging = false;
        _eraserStrokeActive = false;
        _eraserCursorVisible = false;

        if (_selectionMenuItem != null)
            _selectionMenuItem.Checked = mode == EditToolMode.MosaicSelection;
        if (_eraserMenuItem != null)
            _eraserMenuItem.Checked = mode == EditToolMode.MaskEraser;

        pictureBox.Cursor = mode == EditToolMode.MaskEraser ? Cursors.Hand : Cursors.Cross;
        statusLabel.Text = mode == EditToolMode.MaskEraser
            ? L10n.T("마스크 지우개 모드 - 잘못 처리된 부분을 드래그하면 원본으로 복원됩니다.")
            : L10n.T("모자이크 선택 모드 - 드래그로 수동 모자이크 영역을 선택하세요.");
        UpdateModernToolSelection();
        pictureBox.Invalidate();
    }

    private void Eraser_MouseDown(object? sender, MouseEventArgs e)
    {
        if (_editToolMode != EditToolMode.MaskEraser || e.Button != MouseButtons.Left)
            return;

        _isDragging = false;
        _eraserCursorPoint = e.Location;
        _eraserCursorVisible = true;

        if (_originalBitmap == null || _sourceBitmap == null)
        {
            statusLabel.Text = L10n.T("지울 수 있는 원본 이미지가 없습니다.");
            return;
        }

        if (_originalBitmap.Size != _sourceBitmap.Size)
        {
            statusLabel.Text = L10n.T("원본과 작업 이미지 크기가 달라 지우개를 사용할 수 없습니다.");
            return;
        }

        SaveStateToUndo();
        _eraserStrokeActive = true;
        RestoreFromSourceAt(e.Location);
    }

    private void Eraser_MouseMove(object? sender, MouseEventArgs e)
    {
        if (_editToolMode != EditToolMode.MaskEraser)
            return;

        _eraserCursorPoint = e.Location;
        _eraserCursorVisible = true;

        if (_eraserStrokeActive && (e.Button & MouseButtons.Left) != 0)
            RestoreFromSourceAt(e.Location);

        pictureBox.Invalidate();
    }

    private void Eraser_MouseUp(object? sender, MouseEventArgs e)
    {
        if (_editToolMode != EditToolMode.MaskEraser)
            return;

        _eraserStrokeActive = false;
        _eraserCursorPoint = e.Location;
        pictureBox.Invalidate();
    }

    private void Eraser_MouseLeave(object? sender, EventArgs e)
    {
        _eraserCursorVisible = false;
        _eraserStrokeActive = false;
        pictureBox.Invalidate();
    }

    private void Eraser_Paint(object? sender, PaintEventArgs e)
    {
        if (_editToolMode != EditToolMode.MaskEraser
            || !_eraserCursorVisible
            || _originalBitmap == null)
            return;

        RectangleF imageRect = GetImageRect();
        if (imageRect.Width <= 0 || imageRect.Height <= 0)
            return;

        float scale = imageRect.Width / _originalBitmap.Width;
        float radius = Math.Max(5f, _eraserRadius * scale);
        var rect = new RectangleF(
            _eraserCursorPoint.X - radius,
            _eraserCursorPoint.Y - radius,
            radius * 2,
            radius * 2);

        using var pen = new Pen(Color.LimeGreen, 2f);
        e.Graphics.DrawEllipse(pen, rect);
    }

    private void RestoreFromSourceAt(Point picturePoint)
    {
        if (_originalBitmap == null || _sourceBitmap == null)
            return;

        RectangleF imageRect = GetImageRect();
        if (!imageRect.Contains(picturePoint))
            return;

        Point center = PictureBoxToImage(picturePoint);
        RestoreCircularRegionFromSource(center, _eraserRadius);
        pictureBox.Image = _originalBitmap;
        pictureBox.Invalidate();
        statusLabel.Text = L10n.T("마스크 지우개: 원본 픽셀 복원 중...");
    }

    private unsafe void RestoreCircularRegionFromSource(Point center, int radius)
    {
        if (_originalBitmap == null || _sourceBitmap == null)
            return;

        int left = Math.Max(0, center.X - radius);
        int top = Math.Max(0, center.Y - radius);
        int right = Math.Min(_originalBitmap.Width, center.X + radius + 1);
        int bottom = Math.Min(_originalBitmap.Height, center.Y + radius + 1);
        if (right <= left || bottom <= top)
            return;

        Rectangle region = Rectangle.FromLTRB(left, top, right, bottom);
        BitmapData sourceData = _sourceBitmap.LockBits(
            region,
            ImageLockMode.ReadOnly,
            PixelFormat.Format32bppArgb);
        BitmapData targetData = _originalBitmap.LockBits(
            region,
            ImageLockMode.ReadWrite,
            PixelFormat.Format32bppArgb);

        try
        {
            for (int y = 0; y < region.Height; y++)
            {
                byte* sourceRow = (byte*)sourceData.Scan0 + y * sourceData.Stride;
                byte* targetRow = (byte*)targetData.Scan0 + y * targetData.Stride;

                for (int x = 0; x < region.Width; x++)
                {
                    int imageX = region.Left + x;
                    int imageY = region.Top + y;
                    int dx = imageX - center.X;
                    int dy = imageY - center.Y;
                    if (dx * dx + dy * dy > radius * radius)
                        continue;

                    byte* src = sourceRow + x * 4;
                    byte* dst = targetRow + x * 4;
                    dst[0] = src[0];
                    dst[1] = src[1];
                    dst[2] = src[2];
                    dst[3] = src[3];
                }
            }
        }
        finally
        {
            _sourceBitmap.UnlockBits(sourceData);
            _originalBitmap.UnlockBits(targetData);
        }
    }
}
