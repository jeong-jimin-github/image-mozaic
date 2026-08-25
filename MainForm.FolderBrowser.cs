using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ImageMosaicEditor;

public partial class MainForm
{
    private SplitContainer? _mainSplit;
    private ListView? _folderList;
    private ImageList? _folderThumbnails;
    private Label? _folderHeader;
    private Label? _folderCountBadge;
    private Label? _folderPathHint;
    private Label? _folderEmptyHint;
    private string? _currentFolder;
    private bool _folderListUpdating;
    private bool _folderSelectionBusy;
    private bool _folderLayoutBusy;

    private void InitializeFolderBrowserPanel()
    {
        if (_mainSplit != null) return;

        Controls.Remove(pictureBox);

        var split = new SplitContainer
        {
            Dock = DockStyle.None,
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            FixedPanel = FixedPanel.Panel2,
            IsSplitterFixed = false,
            SplitterWidth = 8,
            BackColor = Color.FromArgb(245, 247, 250)
        };

        pictureBox.Dock = DockStyle.Fill;
        split.Panel1.Controls.Add(pictureBox);

        var thumbnails = new ImageList
        {
            ImageSize = new Size(96, 72),
            ColorDepth = ColorDepth.Depth32Bit
        };

        var headerBar = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            Padding = new Padding(12, 6, 8, 6),
            BackColor = Color.White
        };
        headerBar.Paint += (_, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var folderPen = new Pen(Color.FromArgb(25, 122, 234), 2.2f) { LineJoin = LineJoin.Round };
            using var folderFill = new SolidBrush(Color.FromArgb(230, 242, 255));
            var folderRect = new Rectangle(8, 14, 28, 20);
            e.Graphics.FillRectangle(folderFill, folderRect);
            e.Graphics.DrawRectangle(folderPen, folderRect);
            e.Graphics.DrawLines(folderPen, new[]
            {
                new Point(9, 14), new Point(15, 14), new Point(19, 10), new Point(29, 10)
            });
            using var bottomPen = new Pen(Color.FromArgb(226, 232, 240));
            e.Graphics.DrawLine(bottomPen, 0, headerBar.Height - 1, headerBar.Width, headerBar.Height - 1);
        };

        var header = new Label
        {
            AutoSize = false,
            Location = new Point(44, 8),
            Size = new Size(118, 38),
            Text = "이미지 목록",
            TextAlign = ContentAlignment.MiddleLeft,
            BackColor = Color.Transparent,
            ForeColor = Color.FromArgb(18, 38, 72),
            Font = new Font("Segoe UI Semibold", 11.5f, FontStyle.Bold)
        };

        var countBadge = new Label
        {
            AutoSize = false,
            Location = new Point(163, 15),
            Size = new Size(44, 24),
            Text = "0개",
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.FromArgb(105, 133, 174),
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold)
        };
        countBadge.Resize += (_, _) => ApplyBadgeRegion(countBadge);
        ApplyBadgeRegion(countBadge);

        var tileButton = CreateViewModeButton("▦", true);
        var listButton = CreateViewModeButton("☷", false);
        tileButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        listButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        tileButton.Location = new Point(headerBar.Width - 82, 10);
        listButton.Location = new Point(headerBar.Width - 42, 10);
        headerBar.SizeChanged += (_, _) =>
        {
            tileButton.Left = Math.Max(214, headerBar.Width - 82);
            listButton.Left = Math.Max(254, headerBar.Width - 42);
        };

        headerBar.Controls.Add(header);
        headerBar.Controls.Add(countBadge);
        headerBar.Controls.Add(tileButton);
        headerBar.Controls.Add(listButton);

        var dropCard = new SidebarDropCard
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(10),
            Padding = new Padding(12),
            BackColor = Color.White,
            AllowDrop = true
        };

        var dropIcon = new Label
        {
            Dock = DockStyle.Top,
            Height = 48,
            Text = "▱",
            TextAlign = ContentAlignment.BottomCenter,
            ForeColor = Color.FromArgb(25, 122, 234),
            BackColor = Color.Transparent,
            Font = new Font("Segoe UI Symbol", 28f, FontStyle.Bold),
            AllowDrop = true
        };
        var dropTitle = new Label
        {
            Dock = DockStyle.Top,
            Height = 32,
            Text = "폴더를 드래그해 놓으세요.",
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Color.FromArgb(18, 38, 72),
            BackColor = Color.Transparent,
            Font = new Font("Segoe UI Semibold", 10.2f, FontStyle.Bold),
            AllowDrop = true
        };
        var dropSub = new Label
        {
            Dock = DockStyle.Fill,
            Text = "또는 파일을 드래그해 주세요.",
            TextAlign = ContentAlignment.TopCenter,
            ForeColor = Color.FromArgb(110, 123, 143),
            BackColor = Color.Transparent,
            Font = new Font("Segoe UI", 9f),
            AllowDrop = true
        };
        dropCard.Controls.Add(dropSub);
        dropCard.Controls.Add(dropTitle);
        dropCard.Controls.Add(dropIcon);
        RegisterDropTarget(dropCard);
        RegisterDropTarget(dropIcon);
        RegisterDropTarget(dropTitle);
        RegisterDropTarget(dropSub);

        var list = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Tile,
            TileSize = new Size(286, 88),
            LargeImageList = thumbnails,
            SmallImageList = thumbnails,
            MultiSelect = false,
            HideSelection = false,
            FullRowSelect = true,
            BorderStyle = BorderStyle.None,
            AllowDrop = true,
            ShowItemToolTips = true,
            BackColor = Color.White,
            ForeColor = Color.FromArgb(18, 38, 72),
            Font = new Font("Segoe UI", 9.3f)
        };
        list.SelectedIndexChanged += FolderList_SelectedIndexChanged;

        var emptyHint = new Label
        {
            Dock = DockStyle.Bottom,
            Height = 96,
            Text = "▧\n이미지가 없습니다.",
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Color.FromArgb(181, 188, 199),
            BackColor = Color.White,
            Font = new Font("Segoe UI", 10.5f),
            Visible = true
        };
        list.Controls.Add(emptyHint);

        tileButton.Click += (_, _) =>
        {
            list.View = View.Tile;
            SetViewButtonState(tileButton, listButton, true);
        };
        listButton.Click += (_, _) =>
        {
            list.View = View.List;
            SetViewButtonState(tileButton, listButton, false);
        };

        var pathHint = new Label
        {
            Dock = DockStyle.Bottom,
            Height = 24,
            Padding = new Padding(10, 0, 10, 0),
            Text = string.Empty,
            AutoEllipsis = true,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.FromArgb(120, 132, 151),
            BackColor = Color.White,
            Font = new Font("Segoe UI", 8f)
        };

        var listHost = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding = new Padding(8, 4, 8, 4)
        };
        listHost.Controls.Add(list);
        listHost.Controls.Add(pathHint);
        pathHint.BringToFront();

        var sidebarLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            BackColor = Color.White
        };
        sidebarLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        sidebarLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
        sidebarLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 160));
        sidebarLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        sidebarLayout.Controls.Add(headerBar, 0, 0);
        sidebarLayout.Controls.Add(dropCard, 0, 1);
        sidebarLayout.Controls.Add(listHost, 0, 2);
        split.Panel2.Controls.Add(sidebarLayout);

        Controls.Add(split);
        split.SendToBack();
        menuStrip.BringToFront();
        statusStrip.BringToFront();

        _mainSplit = split;
        _folderList = list;
        _folderThumbnails = thumbnails;
        _folderHeader = header;
        _folderCountBadge = countBadge;
        _folderPathHint = pathHint;
        _folderEmptyHint = emptyHint;

        Shown += (_, _) => LayoutFolderWorkspace();
        SizeChanged += (_, _) => LayoutFolderWorkspace();
        Layout += (_, _) => LayoutFolderWorkspace();
    }

    private static void ApplyBadgeRegion(Label badge)
    {
        if (badge.Width <= 0 || badge.Height <= 0) return;
        using GraphicsPath path = RoundedRect(new Rectangle(0, 0, badge.Width, badge.Height), Math.Min(11, badge.Height / 2));
        Region? old = badge.Region;
        badge.Region = new Region(path);
        old?.Dispose();
    }

    private static Button CreateViewModeButton(string text, bool selected)
    {
        var button = new Button
        {
            Size = new Size(34, 34),
            Text = text,
            FlatStyle = FlatStyle.Flat,
            BackColor = selected ? Color.FromArgb(231, 243, 255) : Color.White,
            ForeColor = selected ? Color.FromArgb(25, 122, 234) : Color.FromArgb(117, 128, 145),
            Font = new Font("Segoe UI Symbol", 13f, FontStyle.Bold),
            TabStop = false,
            Cursor = Cursors.Hand
        };
        button.FlatAppearance.BorderSize = selected ? 1 : 0;
        button.FlatAppearance.BorderColor = Color.FromArgb(64, 150, 247);
        button.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 247, 255);
        return button;
    }

    private static void SetViewButtonState(Button tileButton, Button listButton, bool tile)
    {
        tileButton.BackColor = tile ? Color.FromArgb(231, 243, 255) : Color.White;
        tileButton.ForeColor = tile ? Color.FromArgb(25, 122, 234) : Color.FromArgb(117, 128, 145);
        tileButton.FlatAppearance.BorderSize = tile ? 1 : 0;
        listButton.BackColor = tile ? Color.White : Color.FromArgb(231, 243, 255);
        listButton.ForeColor = tile ? Color.FromArgb(117, 128, 145) : Color.FromArgb(25, 122, 234);
        listButton.FlatAppearance.BorderSize = tile ? 0 : 1;
    }

    private void LayoutFolderWorkspace()
    {
        if (_folderLayoutBusy || _mainSplit == null || _mainSplit.IsDisposed) return;

        _folderLayoutBusy = true;
        try
        {
            int top = Math.Max(0, GetModernWorkspaceTop());
            int bottom = statusStrip.Top;
            if (bottom <= top)
                bottom = Math.Max(top + 1, ClientSize.Height - statusStrip.Height);

            int height = Math.Max(1, bottom - top);
            _mainSplit.SetBounds(0, top, Math.Max(1, ClientSize.Width), height);
            AdjustFolderSplitter();
        }
        finally
        {
            _folderLayoutBusy = false;
        }
    }

    private void AdjustFolderSplitter()
    {
        if (_mainSplit == null || _mainSplit.IsDisposed || _mainSplit.Width <= 0) return;

        int sidebarWidth = Math.Clamp((int)(_mainSplit.Width * 0.29), 300, 390);
        int distance = _mainSplit.Width - sidebarWidth - _mainSplit.SplitterWidth;
        int maxDistance = Math.Max(80, _mainSplit.Width - _mainSplit.SplitterWidth - 80);
        distance = Math.Clamp(distance, 80, maxDistance);

        try
        {
            _mainSplit.SplitterDistance = distance;
        }
        catch (ArgumentOutOfRangeException)
        {
            // Layout can transiently report a smaller width during DPI/form changes.
        }
    }

    private async Task OpenFolderAsync(string folder, string? preferredImage = null, bool autoOpen = true)
    {
        if (_folderList == null || _folderThumbnails == null || _folderHeader == null) return;
        if (!Directory.Exists(folder)) return;

        folder = Path.GetFullPath(folder);
        string[] files = Directory.EnumerateFiles(folder)
            .Where(IsSupportedImportFile)
            .OrderBy(path => Path.GetFileName(path), StringComparer.OrdinalIgnoreCase)
            .ToArray();

        _currentFolder = folder;
        _folderListUpdating = true;
        _folderList.BeginUpdate();
        try
        {
            _folderList.Items.Clear();
            _folderThumbnails.Images.Clear();

            for (int i = 0; i < files.Length; i++)
            {
                string path = files[i];
                try
                {
                    using Bitmap thumb = CreateFolderThumbnail(path, _folderThumbnails.ImageSize);
                    _folderThumbnails.Images.Add((Bitmap)thumb.Clone());

                    var item = new ListViewItem(Path.GetFileName(path), _folderThumbnails.Images.Count - 1)
                    {
                        Tag = path,
                        ToolTipText = path
                    };
                    _folderList.Items.Add(item);
                }
                catch (Exception ex)
                {
                    WriteAutoDiagnostic("thumbnail", path, ex);
                }

                if (i > 0 && i % 8 == 0)
                {
                    _folderList.EndUpdate();
                    await Task.Yield();
                    _folderList.BeginUpdate();
                }
            }
        }
        finally
        {
            _folderList.EndUpdate();
            _folderListUpdating = false;
        }

        string folderName = new DirectoryInfo(folder).Name;
        _folderHeader.Text = "이미지 목록";
        if (_folderCountBadge != null)
            _folderCountBadge.Text = $"{_folderList.Items.Count}개";
        if (_folderPathHint != null)
            _folderPathHint.Text = folderName;
        if (_folderEmptyHint != null)
            _folderEmptyHint.Visible = _folderList.Items.Count == 0;

        string? target = preferredImage;
        if (target == null || !files.Contains(target, StringComparer.OrdinalIgnoreCase))
            target = files.FirstOrDefault();

        if (target != null)
            SelectFolderImage(target);

        if (autoOpen && target != null)
            await ImportAndAutoCensorAsync(target, refreshFolderPreview: false);
    }

    private async Task RefreshFolderPreviewForFileAsync(string path)
    {
        string? folder = Path.GetDirectoryName(path);
        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder)) return;

        if (!string.Equals(_currentFolder, Path.GetFullPath(folder), StringComparison.OrdinalIgnoreCase))
            await OpenFolderAsync(folder, path, autoOpen: false);
        else
            SelectFolderImage(path);
    }

    private void SelectFolderImage(string path)
    {
        if (_folderList == null) return;

        _folderListUpdating = true;
        try
        {
            foreach (ListViewItem item in _folderList.Items)
            {
                bool match = item.Tag is string itemPath
                    && string.Equals(itemPath, path, StringComparison.OrdinalIgnoreCase);
                item.Selected = match;
                if (match)
                {
                    item.Focused = true;
                    item.EnsureVisible();
                }
            }
        }
        finally
        {
            _folderListUpdating = false;
        }
    }

    private async void FolderList_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (_folderListUpdating || _folderSelectionBusy || _autoBusy || _folderList == null) return;
        if (_folderList.SelectedItems.Count == 0) return;
        if (_folderList.SelectedItems[0].Tag is not string path) return;
        if (string.Equals(_currentFilePath, path, StringComparison.OrdinalIgnoreCase)) return;

        _folderSelectionBusy = true;
        try
        {
            if (IsBatchOutputPath(path))
            {
                LoadImageDetached(path, ResolveSourcePathForWorkingFile(path));
                SelectFolderImage(path);
                statusLabel.Text = "일괄 처리 결과 이미지 - Ctrl+E로 잘못 처리된 마스크를 지울 수 있습니다.";
            }
            else
            {
                await ImportAndAutoCensorAsync(path, refreshFolderPreview: false);
            }
        }
        finally
        {
            _folderSelectionBusy = false;
        }
    }

    private static Bitmap CreateFolderThumbnail(string path, Size size)
    {
        using Bitmap source = LoadBitmapDetached(path);
        var thumb = new Bitmap(size.Width, size.Height, PixelFormat.Format32bppArgb);

        using Graphics g = Graphics.FromImage(thumb);
        g.Clear(Color.FromArgb(248, 250, 253));
        g.CompositingQuality = CompositingQuality.HighQuality;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.SmoothingMode = SmoothingMode.HighQuality;

        float scale = Math.Min((float)size.Width / source.Width, (float)size.Height / source.Height);
        int width = Math.Max(1, (int)Math.Round(source.Width * scale));
        int height = Math.Max(1, (int)Math.Round(source.Height * scale));
        int x = (size.Width - width) / 2;
        int y = (size.Height - height) / 2;

        g.DrawImage(source, new Rectangle(x, y, width, height));
        return thumb;
    }

    private static GraphicsPath RoundedRect(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        int d = Math.Max(2, radius * 2);
        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}

internal sealed class SidebarDropCard : Panel
{
    public SidebarDropCard()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        Rectangle rect = new Rectangle(1, 1, Math.Max(1, Width - 3), Math.Max(1, Height - 3));
        using GraphicsPath path = CreatePath(rect, 10);
        using var pen = new Pen(Color.FromArgb(188, 203, 222), 1.4f)
        {
            DashStyle = DashStyle.Dash,
            DashPattern = new[] { 5f, 4f }
        };
        e.Graphics.DrawPath(pen, path);
    }

    private static GraphicsPath CreatePath(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        int d = radius * 2;
        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}
