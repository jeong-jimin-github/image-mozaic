using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Svg;

namespace ImageMosaicEditor;

internal static class SvgIconRenderer
{
    private static readonly Dictionary<string, Bitmap> Cache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly object Sync = new();

    public static void Draw(Graphics graphics, string iconName, Rectangle bounds, Color color)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0) return;
        Bitmap? bitmap = GetBitmap(iconName, Math.Max(bounds.Width, bounds.Height), color);
        if (bitmap == null) return;

        graphics.DrawImage(bitmap, bounds);
    }

    public static Bitmap? GetBitmap(string iconName, int size, Color color)
    {
        string key = $"{iconName}|{size}|{color.ToArgb()}";
        lock (Sync)
        {
            if (Cache.TryGetValue(key, out Bitmap? cached))
                return cached;

            string path = Path.Combine(AppContext.BaseDirectory, "assets", "icons", iconName + ".svg");
            if (!File.Exists(path)) return null;

            string svgText = File.ReadAllText(path);
            string hex = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
            svgText = svgText.Replace("currentColor", hex, StringComparison.Ordinal);

            SvgDocument document = SvgDocument.FromSvg<SvgDocument>(svgText);
            using Bitmap rendered = document.Draw(size, size);
            Bitmap bitmap = new(rendered);
            Cache[key] = bitmap;
            return bitmap;
        }
    }

    public static string NameFor(ModernIcon icon) => icon switch
    {
        ModernIcon.Open => "open",
        ModernIcon.Folder => "folder",
        ModernIcon.Save => "save",
        ModernIcon.SaveAs => "save-as",
        ModernIcon.Undo => "undo",
        ModernIcon.Redo => "redo",
        ModernIcon.Magic => "magic",
        ModernIcon.Pointer => "select",
        ModernIcon.Eraser => "eraser",
        ModernIcon.Settings => "settings",
        ModernIcon.Help => "help",
        _ => "help"
    };
}
