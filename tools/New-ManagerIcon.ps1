#Requires -Version 5.1
<#
.SYNOPSIS
    Regenerates AlibreAddInManager\AlibreAddInManager.ico (16, 24, 32, 48 px).
.DESCRIPTION
    Draws a blue rounded tile with a white "download into tray" glyph and writes a
    classic ICO with 32-bit BMP (DIB) entries — no PNG-compressed entries, so any
    icon loader (LoadImage, System.Drawing.Icon) can read every size.
    Run with Windows PowerShell 5.1 (System.Drawing is part of .NET Framework).
#>
param(
    [string]$OutFile = (Join-Path $PSScriptRoot '..\AlibreAddInManager\AlibreAddInManager.ico')
)

Add-Type -AssemblyName System.Drawing

function New-IconBitmap([int]$size) {
    $bmp = New-Object System.Drawing.Bitmap $size, $size, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.Clear([System.Drawing.Color]::Transparent)

    $s = $size / 32.0
    $r = [Math]::Max(2, [int](6 * $s))
    $rect = New-Object System.Drawing.RectangleF (0.5 * $s), (0.5 * $s), ($size - $s), ($size - $s)
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $d = 2 * $r
    $path.AddArc($rect.X, $rect.Y, $d, $d, 180, 90)
    $path.AddArc($rect.Right - $d, $rect.Y, $d, $d, 270, 90)
    $path.AddArc($rect.Right - $d, $rect.Bottom - $d, $d, $d, 0, 90)
    $path.AddArc($rect.X, $rect.Bottom - $d, $d, $d, 90, 90)
    $path.CloseFigure()
    $brush = New-Object System.Drawing.Drawing2D.LinearGradientBrush $rect, ([System.Drawing.Color]::FromArgb(255, 40, 120, 215)), ([System.Drawing.Color]::FromArgb(255, 18, 70, 150)), 90.0
    $g.FillPath($brush, $path)

    $white = [System.Drawing.Color]::White
    $pen = New-Object System.Drawing.Pen $white, ([Math]::Max(1.5, 3.0 * $s))
    $pen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $pen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    $pen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round

    # Arrow down
    $g.DrawLine($pen, 16 * $s, 6 * $s, 16 * $s, 18 * $s)
    $g.DrawLines($pen, [System.Drawing.PointF[]]@(
        (New-Object System.Drawing.PointF (10.5 * $s), (13 * $s)),
        (New-Object System.Drawing.PointF (16 * $s), (18.5 * $s)),
        (New-Object System.Drawing.PointF (21.5 * $s), (13 * $s))))
    # Tray
    $g.DrawLines($pen, [System.Drawing.PointF[]]@(
        (New-Object System.Drawing.PointF (7 * $s), (19 * $s)),
        (New-Object System.Drawing.PointF (7 * $s), (25 * $s)),
        (New-Object System.Drawing.PointF (25 * $s), (25 * $s)),
        (New-Object System.Drawing.PointF (25 * $s), (19 * $s))))

    $g.Dispose()
    return $bmp
}

function Get-DibBytes([System.Drawing.Bitmap]$bmp) {
    $w = $bmp.Width; $h = $bmp.Height
    $ms = New-Object System.IO.MemoryStream
    $bw = New-Object System.IO.BinaryWriter $ms
    # BITMAPINFOHEADER; height is doubled to cover the AND mask
    $bw.Write([int]40); $bw.Write([int]$w); $bw.Write([int]($h * 2))
    $bw.Write([int16]1); $bw.Write([int16]32); $bw.Write([int]0)
    $bw.Write([int]0); $bw.Write([int]0); $bw.Write([int]0); $bw.Write([int]0); $bw.Write([int]0)
    # XOR bitmap: BGRA, bottom-up
    for ($y = $h - 1; $y -ge 0; $y--) {
        for ($x = 0; $x -lt $w; $x++) {
            $c = $bmp.GetPixel($x, $y)
            $bw.Write([byte]$c.B); $bw.Write([byte]$c.G); $bw.Write([byte]$c.R); $bw.Write([byte]$c.A)
        }
    }
    # AND mask: 1 bpp, rows padded to 32 bits; alpha channel already carries transparency
    $rowBytes = [int]([Math]::Ceiling($w / 32.0) * 4)
    for ($y = $h - 1; $y -ge 0; $y--) {
        $row = New-Object byte[] $rowBytes
        for ($x = 0; $x -lt $w; $x++) {
            if ($bmp.GetPixel($x, $y).A -eq 0) { $row[[int][Math]::Floor($x / 8)] = $row[[int][Math]::Floor($x / 8)] -bor (0x80 -shr ($x % 8)) }
        }
        $bw.Write($row)
    }
    $bw.Flush()
    return , $ms.ToArray()   # leading comma: return the byte[] itself, not its unrolled bytes
}

$sizes = 16, 24, 32, 48
$images = New-Object 'System.Collections.Generic.List[byte[]]'
foreach ($size in $sizes) {
    $b = New-IconBitmap $size
    $images.Add([byte[]](Get-DibBytes $b))
    $b.Dispose()
}

$out = New-Object System.IO.MemoryStream
$w = New-Object System.IO.BinaryWriter $out
$w.Write([int16]0); $w.Write([int16]1); $w.Write([int16]$sizes.Count)
$offset = 6 + 16 * $sizes.Count
for ($i = 0; $i -lt $sizes.Count; $i++) {
    $w.Write([byte]$sizes[$i]); $w.Write([byte]$sizes[$i]); $w.Write([byte]0); $w.Write([byte]0)
    $w.Write([int16]1); $w.Write([int16]32)
    $w.Write([int]$images[$i].Length); $w.Write([int]$offset)
    $offset += $images[$i].Length
}
foreach ($img in $images) { $w.Write($img) }
$w.Flush()

$full = [System.IO.Path]::GetFullPath($OutFile)
[System.IO.File]::WriteAllBytes($full, $out.ToArray())

# Self-check: System.Drawing must be able to read it back.
$check = New-Object System.Drawing.Icon $full
"Wrote $full ($($out.Length) bytes, $($sizes -join '/') px; reads back as $($check.Width)x$($check.Height))"
$check.Dispose()
