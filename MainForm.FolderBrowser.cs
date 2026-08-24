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
            SplitterWidth = 5
        };

        pictureBox.Dock = DockStyle.Fill;
        split.Panel1.Controls.Add(pictureBox);

        var thumbnails = new ImageList
        {
            ImageSize = new Size(96, 72),
            ColorDepth = ColorDepth.Depth32Bit
        };

        var header = new Label
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0),
            Padding = new Padding(10, 0, 8, 0),
            Text = "폴더를 드래그해 놓으세요.",
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true,
            BackColor = SystemColors.Control,
            ForeColor = SystemColors.ControlText
        };

        var list = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Tile,
            TileSize = new Size(230, 82),
            LargeImageList = thumbnails,
            MultiSelect = false,
            HideSelection = false,
            FullRowSelect = true,
            BorderStyle = BorderStyle.FixedSingle,
            AllowDrop = true,
            ShowItemToolTips = true
        };
        list.SelectedIndexChanged += FolderList_SelectedIndexChanged;

        var sidebarLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };
        sidebarLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        sidebarLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        sidebarLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        sidebarLayout.Controls.Add(header, 0, 0);
        sidebarLayout.Controls.Add(list, 0, 1);
        split.Panel2.Controls.Add(sidebarLayout);

        Controls.Add(split);
        split.SendToBack();
        menuStrip.BringToFront();
        statusStrip.BringToFront();

        _mainSplit = split;
        _folderList = list;
        _folderThumbnails = thumbnails;
        _folderHeader = header;

        // Do not rely on Dock=Fill here. The form's existing MenuStrip and
        // StatusStrip can overlay a dynamically-added Fill control depending on
        // z-order/DPI. Explicit bounds guarantee that the folder header always
        // begins below the menu and ends above the status bar.
        Shown += (_, _) => LayoutFolderWorkspace();
        SizeChanged += (_, _) => LayoutFolderWorkspace();
        Layout += (_, _) => LayoutFolderWorkspace();
    }

    private void LayoutFolderWorkspace()
    {
        if (_folderLayoutBusy || _mainSplit == null || _mainSplit.IsDisposed) return;

        _folderLayoutBusy = true;
        try
        {
            int top = Math.Max(0, menuStrip.Bottom);
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

        int sidebarWidth = Math.Clamp((int)(_mainSplit.Width * 0.30), 240, 330);
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

        _folderHeader.Text = $"{new DirectoryInfo(folder).Name}  ·  {_folderList.Items.Count}개 이미지";

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
            await ImportAndAutoCensorAsync(path, refreshFolderPreview: false);
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
        g.Clear(Color.FromArgb(36, 36, 36));
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
}
