using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace ImageMosaicEditor;

public partial class MainForm
{
    private static Bitmap LoadBitmapDetached(string path)
    {
        using FileStream stream = new(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using Image decoded = Image.FromStream(stream, useEmbeddedColorManagement: true, validateImageData: true);

        var bitmap = new Bitmap(decoded.Width, decoded.Height, PixelFormat.Format32bppArgb);
        bitmap.SetResolution(
            decoded.HorizontalResolution > 0 ? decoded.HorizontalResolution : 96f,
            decoded.VerticalResolution > 0 ? decoded.VerticalResolution : 96f);

        using Graphics g = Graphics.FromImage(bitmap);
        g.DrawImage(decoded, new Rectangle(0, 0, bitmap.Width, bitmap.Height));
        return bitmap;
    }

    private void LoadImageDetached(string path)
    {
        Bitmap loaded = LoadBitmapDetached(path);

        pictureBox.Image = null;
        _originalBitmap?.Dispose();
        ClearStacks();
        _originalBitmap = loaded;
        _currentFilePath = path;
        _selectionRect = Rectangle.Empty;

        pictureBox.Image = _originalBitmap;
        pictureBox.SizeMode = PictureBoxSizeMode.Zoom;
        Text = $"이미지 모자이크 편집기 - {Path.GetFileName(path)}";
    }

    private static void SaveBitmapAsPngDetached(Bitmap source, string path)
    {
        using var normalized = new Bitmap(source.Width, source.Height, PixelFormat.Format32bppArgb);
        using (Graphics g = Graphics.FromImage(normalized))
            g.DrawImageUnscaled(source, 0, 0);

        normalized.Save(path, ImageFormat.Png);
    }

    private static void WriteAutoDiagnostic(string stage, string? path, Exception ex)
    {
        try
        {
            string dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ImageMosaicEditor");
            Directory.CreateDirectory(dir);
            string logPath = Path.Combine(dir, "auto-error.log");
            File.AppendAllText(logPath,
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] stage={stage}\r\npath={path}\r\n{ex}\r\n\r\n");
        }
        catch
        {
            // Diagnostics must never hide the original error.
        }
    }
}
