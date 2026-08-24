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

    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $diameter = [Math]::Max(2.0, $Radius * 2.0)
    $arc = New-Object System.Drawing.RectangleF($Rect.X, $Rect.Y, $diameter, $diameter)

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

    $bitmap = New-Object System.Drawing.Bitmap($Size, $Size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $stream = New-Object System.IO.MemoryStream

    try {
        $graphics.Clear([System.Drawing.Color]::Transparent)
        $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
        $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
        $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic

        $margin = [float]($Size * 0.035)
        $bodyRect = New-Object System.Drawing.RectangleF($margin, $margin, $Size - 2 * $margin, $Size - 2 * $margin)
        $bodyPath = New-RoundedRectPath -Rect $bodyRect -Radius ($Size * 0.19)

        try {
            $blue1 = [System.Drawing.Color]::FromArgb(255, 21, 132, 255)
            $blue2 = [System.Drawing.Color]::FromArgb(255, 0, 72, 190)
            $background = New-Object System.Drawing.Drawing2D.LinearGradientBrush($bodyRect, $blue1, $blue2, 135.0)
            try { $graphics.FillPath($background, $bodyPath) } finally { $background.Dispose() }

            $innerPen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(155, 185, 226, 255), [Math]::Max(1.0, $Size * 0.018))
            try { $graphics.DrawPath($innerPen, $bodyPath) } finally { $innerPen.Dispose() }

            # Small mosaic blocks in the background.
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
                    $c = $palette[($row * 3 + $col) % $palette.Count]
                    $brush = New-Object System.Drawing.SolidBrush($c)
                    try {
                        $x = $startX + $col * ($block * 0.88)
                        $y = $startY + $row * ($block * 0.88)
                        $graphics.FillRectangle($brush, $x, $y, $block * 0.72, $block * 0.72)
                    } finally { $brush.Dispose() }
                }
            }

            # Magnifying-glass lens.
            $lensDiameter = [float]($Size * 0.50)
            $lensRect = New-Object System.Drawing.RectangleF($Size * 0.145, $Size * 0.145, $lensDiameter, $lensDiameter)
            $lensFill = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(72, 235, 248, 255))
            try { $graphics.FillEllipse($lensFill, $lensRect) } finally { $lensFill.Dispose() }

            $lensPenShadow = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(130, 0, 32, 88), [Math]::Max(2.0, $Size * 0.058))
            $lensPen = New-Object System.Drawing.Pen([System.Drawing.Color]::White, [Math]::Max(2.0, $Size * 0.038))
            try {
                $graphics.DrawEllipse($lensPenShadow, $lensRect)
                $graphics.DrawEllipse($lensPen, $lensRect)
            } finally {
                $lensPenShadow.Dispose()
                $lensPen.Dispose()
            }

            # Magnifier handle.
            $handleShadow = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(150, 0, 28, 70), [Math]::Max(3.0, $Size * 0.105))
            $handle = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(255, 240, 248, 255), [Math]::Max(3.0, $Size * 0.072))
            $handle.StartCap = $handle.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
            $handleShadow.StartCap = $handleShadow.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
            try {
                $x1 = $Size * 0.555; $y1 = $Size * 0.555
                $x2 = $Size * 0.79;  $y2 = $Size * 0.79
                $graphics.DrawLine($handleShadow, $x1, $y1, $x2, $y2)
                $graphics.DrawLine($handle, $x1, $y1, $x2, $y2)
            } finally {
                $handleShadow.Dispose()
                $handle.Dispose()
            }

            # Pixelated focal blocks inside the lens.
            $focusPath = New-Object System.Drawing.Drawing2D.GraphicsPath
            try {
                $focusPath.AddEllipse($lensRect)
                $oldClip = $graphics.Clip
                try {
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
                            $brush = New-Object System.Drawing.SolidBrush($focusColors[($row + $col * 2) % $focusColors.Count])
                            try {
                                $graphics.FillRectangle(
                                    $brush,
                                    $focusX + $col * $focusSize,
                                    $focusY + $row * $focusSize,
                                    $focusSize + 0.5,
                                    $focusSize + 0.5)
                            } finally { $brush.Dispose() }
                        }
                    }
                } finally {
                    $graphics.Clip = $oldClip
                    $oldClip.Dispose()
                }
            } finally { $focusPath.Dispose() }

            # Sparkle at the top-right for recognizability at small sizes.
            $sparkPen = New-Object System.Drawing.Pen([System.Drawing.Color]::White, [Math]::Max(1.5, $Size * 0.025))
            $sparkPen.StartCap = $sparkPen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
            try {
                $sx = $Size * 0.77; $sy = $Size * 0.21; $r = $Size * 0.075
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

$sizes = @(256, 64, 48, 32, 16)
$images = @()
foreach ($size in $sizes) {
    $images += [PSCustomObject]@{ Size = $size; Data = (New-IconPng -Size $size) }
}

$stream = [System.IO.File]::Open($OutputPath, [System.IO.FileMode]::Create, [System.IO.FileAccess]::Write)
$writer = New-Object System.IO.BinaryWriter($stream)
try {
    $writer.Write([UInt16]0) # reserved
    $writer.Write([UInt16]1) # icon
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
    $stream.Dispose()
}

Write-Host "Generated application icon: $OutputPath"
