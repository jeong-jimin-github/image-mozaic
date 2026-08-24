using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace ImageMosaicEditor;

public partial class MainForm
{
    private static Bitmap LoadBitmapDetached(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("이미지 경로가 비어 있습니다.", nameof(path));
        if (!File.Exists(path))
            throw new FileNotFoundException("이미지 파일을 찾을 수 없습니다.", path);

        using FileStream stream = new(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete);

        // Bitmap(Stream) is intentionally used instead of Image.FromStream(...,
        // validateImageData: true). Some perfectly viewable JPEG/PNG files contain
        // metadata that makes GDI+ validation throw "Parameter is not valid".
        using var decoded = new Bitmap(stream);
        if (decoded.Width <= 0 || decoded.Height <= 0)
            throw new InvalidDataException("이미지 크기가 올바르지 않습니다.");

        // Detach the bitmap from the file stream and normalize the pixel format so
        // later Save/LockBits operations do not depend on the source file format.
        var bitmap = new Bitmap(decoded.Width, decoded.Height, PixelFormat.Format32bppArgb);
        using (Graphics g = Graphics.FromImage(bitmap))
        {
            g.DrawImage(
                decoded,
                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                0,
                0,
                decoded.Width,
                decoded.Height,
                GraphicsUnit.Pixel);
        }

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
        if (source.Width <= 0 || source.Height <= 0)
            throw new InvalidOperationException("저장할 이미지 크기가 올바르지 않습니다.");

        string? directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        // _originalBitmap is already a detached 32bpp bitmap. Re-rendering it into
        // another Graphics surface is unnecessary and was another GDI+ failure point.
        source.Save(path, ImageFormat.Png);
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
