param(
    [Parameter(Mandatory = $false)]
    [string]$OutputPath = (Join-Path $PSScriptRoot 'app.ico')
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

function New-RoundedRectPath {
    param(
        [System.Drawing.RectangleF]$Rect,
        [float]$Radius
    )

    $path = [System.Drawing.Drawing2D.GraphicsPath]::new()
    $diameter = [Math]::Max(2.0, $Radius * 2.0)
    $arc = [System.Drawing.RectangleF]::new($Rect.X, $Rect.Y, $diameter, $diameter)

    $path.AddArc($arc, 180, 90)
    $arc.X = $Rect.Right - $diameter
    $path.AddArc($arc, 270, 90)
    $arc.Y = $Rect.Bottom - $diameter
    $path.AddArc($arc, 0, 90)
    $arc.X = $Rect.Left
    $path.AddArc($arc, 90, 90)
    $path.CloseFigure()
    return $path
}

function New-IconPng {
    param([int]$Size)

    $bitmap = [System.Drawing.Bitmap]::new($Size, $Size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $stream = [System.IO.MemoryStream]::new()

    try {
        $graphics.Clear([System.Drawing.Color]::Transparent)
        $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
        $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
        $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic

        $margin = [float]($Size * 0.035)
        $bodyRect = [System.Drawing.RectangleF]::new($margin, $margin, $Size - 2 * $margin, $Size - 2 * $margin)
        $bodyPath = New-RoundedRectPath -Rect $bodyRect -Radius ($Size * 0.19)

        try {
            $blue1 = [System.Drawing.Color]::FromArgb(255, 21, 132, 255)
            $blue2 = [System.Drawing.Color]::FromArgb(255, 0, 72, 190)
            $background = [System.Drawing.Drawing2D.LinearGradientBrush]::new($bodyRect, $blue1, $blue2, 135.0)
            try { $graphics.FillPath($background, $bodyPath) } finally { $background.Dispose() }

            $innerPen = [System.Drawing.Pen]::new(
                [System.Drawing.Color]::FromArgb(155, 185, 226, 255),
                [single][Math]::Max(1.0, $Size * 0.018))
            try { $graphics.DrawPath($innerPen, $bodyPath) } finally { $innerPen.Dispose() }

            # Mosaic blocks make the app purpose readable even at small sizes.
            $block = [Math]::Max(2.0, $Size * 0.075)
            $startX = $Size * 0.17
            $startY = $Size * 0.19
            $palette = @(
                [System.Drawing.Color]::FromArgb(95, 255, 255, 255),
                [System.Drawing.Color]::FromArgb(80, 130, 210, 255),
                [System.Drawing.Color]::FromArgb(65, 45, 100, 215),
                [System.Drawing.Color]::FromArgb(100, 220, 242, 255)
            )
            for ($row = 0; $row -lt 4; $row++) {
                for ($col = 0; $col -lt 4; $col++) {
                    $color = $palette[($row * 3 + $col) % $palette.Count]
                    $brush = [System.Drawing.SolidBrush]::new($color)
                    try {
                        $x = $startX + $col * ($block * 0.88)
                        $y = $startY + $row * ($block * 0.88)
                        $graphics.FillRectangle(
                            $brush,
                            [single]$x,
                            [single]$y,
                            [single]($block * 0.72),
                            [single]($block * 0.72))
                    } finally { $brush.Dispose() }
                }
            }

            # Magnifying glass: detection/inspection metaphor.
            $lensDiameter = [float]($Size * 0.50)
            $lensRect = [System.Drawing.RectangleF]::new(
                [single]($Size * 0.145),
                [single]($Size * 0.145),
                $lensDiameter,
                $lensDiameter)
            $lensFill = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(72, 235, 248, 255))
            try { $graphics.FillEllipse($lensFill, $lensRect) } finally { $lensFill.Dispose() }

            $lensPenShadow = [System.Drawing.Pen]::new(
                [System.Drawing.Color]::FromArgb(130, 0, 32, 88),
                [single][Math]::Max(2.0, $Size * 0.058))
            $lensPen = [System.Drawing.Pen]::new(
                [System.Drawing.Color]::White,
                [single][Math]::Max(2.0, $Size * 0.038))
            try {
                $graphics.DrawEllipse($lensPenShadow, $lensRect)
                $graphics.DrawEllipse($lensPen, $lensRect)
            } finally {
                $lensPenShadow.Dispose()
                $lensPen.Dispose()
            }

            $handleShadow = [System.Drawing.Pen]::new(
                [System.Drawing.Color]::FromArgb(150, 0, 28, 70),
                [single][Math]::Max(3.0, $Size * 0.105))
            $handle = [System.Drawing.Pen]::new(
                [System.Drawing.Color]::FromArgb(255, 240, 248, 255),
                [single][Math]::Max(3.0, $Size * 0.072))
            $handle.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
            $handle.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
            $handleShadow.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
            $handleShadow.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
            try {
                $x1 = [single]($Size * 0.555); $y1 = [single]($Size * 0.555)
                $x2 = [single]($Size * 0.79);  $y2 = [single]($Size * 0.79)
                $graphics.DrawLine($handleShadow, $x1, $y1, $x2, $y2)
                $graphics.DrawLine($handle, $x1, $y1, $x2, $y2)
            } finally {
                $handleShadow.Dispose()
                $handle.Dispose()
            }

            # Pixelated focal blocks clipped inside the lens.
            $focusPath = [System.Drawing.Drawing2D.GraphicsPath]::new()
            $graphicsState = $graphics.Save()
            try {
                $focusPath.AddEllipse($lensRect)
                $graphics.SetClip($focusPath)
                $focusSize = [Math]::Max(2.0, $Size * 0.075)
                $focusX = $Size * 0.25
                $focusY = $Size * 0.27
                $focusColors = @(
                    [System.Drawing.Color]::FromArgb(225, 232, 246, 255),
                    [System.Drawing.Color]::FromArgb(225, 123, 188, 255),
                    [System.Drawing.Color]::FromArgb(225, 58, 118, 218),
                    [System.Drawing.Color]::FromArgb(225, 255, 224, 235),
                    [System.Drawing.Color]::FromArgb(225, 181, 221, 255)
                )
                for ($row = 0; $row -lt 3; $row++) {
                    for ($col = 0; $col -lt 3; $col++) {
                        $brush = [System.Drawing.SolidBrush]::new($focusColors[($row + $col * 2) % $focusColors.Count])
                        try {
                            $graphics.FillRectangle(
                                $brush,
                                [single]($focusX + $col * $focusSize),
                                [single]($focusY + $row * $focusSize),
                                [single]($focusSize + 0.5),
                                [single]($focusSize + 0.5))
                        } finally { $brush.Dispose() }
                    }
                }
            } finally {
                $graphics.Restore($graphicsState)
                $focusPath.Dispose()
            }

            # Sparkle keeps the silhouette distinct in Explorer/taskbar views.
            $sparkPen = [System.Drawing.Pen]::new(
                [System.Drawing.Color]::White,
                [single][Math]::Max(1.5, $Size * 0.025))
            $sparkPen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
            $sparkPen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
            try {
                $sx = [single]($Size * 0.77); $sy = [single]($Size * 0.21); $r = [single]($Size * 0.075)
                $graphics.DrawLine($sparkPen, $sx - $r, $sy, $sx + $r, $sy)
                $graphics.DrawLine($sparkPen, $sx, $sy - $r, $sx, $sy + $r)
                $r2 = $r * 0.55
                $graphics.DrawLine($sparkPen, $sx - $r2, $sy - $r2, $sx + $r2, $sy + $r2)
                $graphics.DrawLine($sparkPen, $sx + $r2, $sy - $r2, $sx - $r2, $sy + $r2)
            } finally { $sparkPen.Dispose() }
        } finally {
            $bodyPath.Dispose()
        }

        $bitmap.Save($stream, [System.Drawing.Imaging.ImageFormat]::Png)
        return ,$stream.ToArray()
    }
    finally {
        $stream.Dispose()
        $graphics.Dispose()
        $bitmap.Dispose()
    }
}

$targetDirectory = Split-Path -Parent $OutputPath
if ($targetDirectory) {
    [System.IO.Directory]::CreateDirectory($targetDirectory) | Out-Null
}

# ICO contains several native Windows icon sizes. PNG payloads inside ICO are
# supported by modern Windows and preserve alpha transparency cleanly.
$sizes = @(256, 64, 48, 32, 16)
$images = @()
foreach ($size in $sizes) {
    $images += [PSCustomObject]@{ Size = $size; Data = (New-IconPng -Size $size) }
}

$fileStream = [System.IO.File]::Open($OutputPath, [System.IO.FileMode]::Create, [System.IO.FileAccess]::Write)
$writer = [System.IO.BinaryWriter]::new($fileStream)
try {
    $writer.Write([UInt16]0)
    $writer.Write([UInt16]1)
    $writer.Write([UInt16]$images.Count)

    $offset = 6 + (16 * $images.Count)
    foreach ($image in $images) {
        $dimension = if ($image.Size -ge 256) { 0 } else { $image.Size }
        $writer.Write([byte]$dimension)
        $writer.Write([byte]$dimension)
        $writer.Write([byte]0)
        $writer.Write([byte]0)
        $writer.Write([UInt16]1)
        $writer.Write([UInt16]32)
        $writer.Write([UInt32]$image.Data.Length)
        $writer.Write([UInt32]$offset)
        $offset += $image.Data.Length
    }

    foreach ($image in $images) {
        $writer.Write([byte[]]$image.Data)
    }
}
finally {
    $writer.Dispose()
    $fileStream.Dispose()
}

Write-Host "Generated application icon: $OutputPath"
