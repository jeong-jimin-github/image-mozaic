using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace ImageMosaicEditor;

public partial class MainForm
{
    private static readonly byte[] RawImageMagic = [(byte)'I', (byte)'M', (byte)'B', (byte)'G'];

    // _originalBitmap is the editable/visible working bitmap kept for compatibility
    // with the existing manual mosaic and undo/redo code. _sourceBitmap is never
    // modified after import and is used for every automatic re-process and eraser.
    private Bitmap? _sourceBitmap;
    private string? _sourceFilePath;

    private static Bitmap LoadBitmapDetached(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("이미지 경로가 비어 있습니다.", nameof(path));
        if (!File.Exists(path))
            throw new FileNotFoundException("이미지 파일을 찾을 수 없습니다.", path);

        string cachePath = EnsurePillowDecodedCache(path);
        return LoadRawBgraBitmap(cachePath);
    }

    private static string EnsurePillowDecodedCache(string sourcePath)
    {
        FileInfo info = new(sourcePath);
        string fullPath = Path.GetFullPath(sourcePath);
        string cacheKey = $"{fullPath}|{info.Length}|{info.LastWriteTimeUtc.Ticks}";
        string hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(cacheKey)));

        string appDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ImageMosaicEditor");
        string cacheDir = Path.Combine(appDir, "decode-cache");
        Directory.CreateDirectory(cacheDir);

        string cachePath = Path.Combine(cacheDir, hash + ".imbgra");
        if (File.Exists(cachePath) && new FileInfo(cachePath).Length >= 12)
            return cachePath;

        string helper = Path.Combine(AppContext.BaseDirectory, "python", "image_decode_bridge.py");
        if (!File.Exists(helper))
            throw new FileNotFoundException("이미지 디코딩 브리지를 찾을 수 없습니다.", helper);

        string bundledPython = Path.Combine(AppContext.BaseDirectory, "python-runtime", "python.exe");
        string python = File.Exists(bundledPython) ? bundledPython : "python";

        var psi = new ProcessStartInfo
        {
            FileName = python,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = AppContext.BaseDirectory
        };
        psi.Environment["PYTHONUTF8"] = "1";
        psi.Environment["PYTHONIOENCODING"] = "utf-8";
        psi.Environment["PYTHONNOUSERSITE"] = "1";
        psi.ArgumentList.Add(helper);
        psi.ArgumentList.Add("--input-file");
        psi.ArgumentList.Add(fullPath);
        psi.ArgumentList.Add("--output-file");
        psi.ArgumentList.Add(cachePath);

        using var process = new Process { StartInfo = psi };
        try
        {
            if (!process.Start())
                throw new InvalidOperationException("Pillow 이미지 디코더를 시작할 수 없습니다.");
        }
        catch (Exception ex) when (!File.Exists(bundledPython))
        {
            throw new InvalidOperationException(
                "번들 Python을 찾을 수 없고 시스템 Python도 실행할 수 없습니다.", ex);
        }

        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();

        if (!process.WaitForExit(120_000))
        {
            try { process.Kill(entireProcessTree: true); } catch { }
            throw new TimeoutException("Pillow 이미지 디코딩이 120초 안에 끝나지 않았습니다.");
        }

        string stdout = stdoutTask.GetAwaiter().GetResult();
        string stderr = stderrTask.GetAwaiter().GetResult();

        if (process.ExitCode != 0 || !File.Exists(cachePath))
        {
            try { if (File.Exists(cachePath)) File.Delete(cachePath); } catch { }
            string detail = string.IsNullOrWhiteSpace(stderr) ? stdout : stderr;
            throw new InvalidDataException(
                string.IsNullOrWhiteSpace(detail)
                    ? $"Pillow가 이미지를 디코딩하지 못했습니다. 종료 코드: {process.ExitCode}"
                    : detail.Trim());
        }

        return cachePath;
    }

    private static Bitmap LoadRawBgraBitmap(string cachePath)
    {
        using FileStream stream = new(cachePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using BinaryReader reader = new(stream, Encoding.UTF8, leaveOpen: false);

        byte[] magic = reader.ReadBytes(4);
        if (magic.Length != 4 || !magic.AsSpan().SequenceEqual(RawImageMagic))
            throw new InvalidDataException("이미지 디코딩 캐시 형식이 올바르지 않습니다.");

        uint widthValue = reader.ReadUInt32();
        uint heightValue = reader.ReadUInt32();
        if (widthValue == 0 || heightValue == 0 || widthValue > 100_000 || heightValue > 100_000)
            throw new InvalidDataException($"이미지 크기가 올바르지 않습니다: {widthValue}x{heightValue}");

        int width = checked((int)widthValue);
        int height = checked((int)heightValue);
        long expectedLong = checked((long)width * height * 4L);
        if (expectedLong > int.MaxValue)
            throw new InvalidDataException("이미지가 너무 커서 메모리에 로드할 수 없습니다.");

        int expected = (int)expectedLong;
        byte[] pixels = reader.ReadBytes(expected);
        if (pixels.Length != expected)
            throw new InvalidDataException("이미지 디코딩 캐시의 픽셀 데이터가 불완전합니다.");

        var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        BitmapData data = bitmap.LockBits(
            new Rectangle(0, 0, width, height),
            ImageLockMode.WriteOnly,
            PixelFormat.Format32bppArgb);

        try
        {
            int rowBytes = checked(width * 4);
            for (int y = 0; y < height; y++)
            {
                IntPtr rowPtr = IntPtr.Add(data.Scan0, y * data.Stride);
                Marshal.Copy(pixels, y * rowBytes, rowPtr, rowBytes);
            }
        }
        finally
        {
            bitmap.UnlockBits(data);
        }

        return bitmap;
    }

    private void LoadImageDetached(string path, string? sourcePath = null)
    {
        Bitmap working = LoadBitmapDetached(path);
        Bitmap source;

        string? resolvedSource = sourcePath ?? ResolveSourcePathForWorkingFile(path);
        if (!string.IsNullOrWhiteSpace(resolvedSource)
            && File.Exists(resolvedSource)
            && !string.Equals(Path.GetFullPath(resolvedSource), Path.GetFullPath(path), StringComparison.OrdinalIgnoreCase))
        {
            source = LoadBitmapDetached(resolvedSource);
            _sourceFilePath = Path.GetFullPath(resolvedSource);
        }
        else
        {
            source = (Bitmap)working.Clone();
            _sourceFilePath = Path.GetFullPath(path);
        }

        if (source.Size != working.Size)
        {
            source.Dispose();
            source = (Bitmap)working.Clone();
            _sourceFilePath = Path.GetFullPath(path);
        }

        pictureBox.Image = null;
        _originalBitmap?.Dispose();
        _sourceBitmap?.Dispose();
        ClearStacks();

        _originalBitmap = working;
        _sourceBitmap = source;
        _currentFilePath = path;
        _selectionRect = Rectangle.Empty;

        pictureBox.Image = _originalBitmap;
        pictureBox.SizeMode = PictureBoxSizeMode.Zoom;
        Text = $"이미지 모자이크 편집기 - {Path.GetFileName(path)}";
    }

    private void ReplaceWorkingBitmap(Bitmap next, bool saveUndo)
    {
        if (saveUndo && _originalBitmap != null)
            SaveStateToUndo();

        pictureBox.Image = null;
        _originalBitmap?.Dispose();
        _originalBitmap = next;
        pictureBox.Image = _originalBitmap;
        pictureBox.Invalidate();
    }

    private void ResetWorkingFromSource(bool saveUndo)
    {
        if (_sourceBitmap == null) return;
        ReplaceWorkingBitmap((Bitmap)_sourceBitmap.Clone(), saveUndo);
    }

    private static void SaveBitmapAsPngDetached(Bitmap source, string path)
    {
        if (source.Width <= 0 || source.Height <= 0)
            throw new InvalidOperationException("저장할 이미지 크기가 올바르지 않습니다.");

        string? directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

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
