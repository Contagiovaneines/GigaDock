# Gera um ?cone geom?trico original, sem fontes ou depend?ncias externas.
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$assetDir = Join-Path (Split-Path $PSScriptRoot -Parent) 'assets'
New-Item -ItemType Directory -Path $assetDir -Force | Out-Null
$frames = @()
foreach ($size in @(16, 20, 24, 32, 48, 64, 128, 256)) {
    $bitmap = [Drawing.Bitmap]::new($size, $size)
    $g = [Drawing.Graphics]::FromImage($bitmap)
    $g.SmoothingMode = [Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.ScaleTransform($size / 256.0, $size / 256.0)
    $path = [Drawing.Drawing2D.GraphicsPath]::new()
    $path.AddArc(8,8,64,64,180,90)
    $path.AddArc(184,8,64,64,270,90)
    $path.AddArc(184,184,64,64,0,90)
    $path.AddArc(8,184,64,64,90,90)
    $path.CloseFigure()
    $brush = [Drawing.Drawing2D.LinearGradientBrush]::new([Drawing.Point]::new(0,0), [Drawing.Point]::new(256,256), [Drawing.Color]::FromArgb(27,66,130), [Drawing.Color]::FromArgb(13,22,43))
    $g.FillPath($brush,$path)
    $pen = [Drawing.Pen]::new([Drawing.Color]::FromArgb(67,210,235),23)
    $pen.StartCap = $pen.EndCap = [Drawing.Drawing2D.LineCap]::Round
    $g.DrawArc($pen,66,43,124,124,35,285)
    $g.DrawLine($pen,186,115,143,115)
    $g.DrawLine($pen,186,115,186,141)
    $dock = [Drawing.SolidBrush]::new([Drawing.Color]::FromArgb(228,240,255))
    $g.FillRectangle($dock,65,193,31,27)
    $g.FillRectangle($dock,113,183,31,37)
    $g.FillRectangle($dock,161,193,31,27)
    $stream = [IO.MemoryStream]::new()
    $bitmap.Save($stream,[Drawing.Imaging.ImageFormat]::Png)
    $frames += ,$stream.ToArray()
    if ($size -eq 256) { $bitmap.Save((Join-Path $assetDir 'gigadock.png'),[Drawing.Imaging.ImageFormat]::Png) }
    $stream.Dispose(); $dock.Dispose(); $pen.Dispose(); $brush.Dispose(); $path.Dispose(); $g.Dispose(); $bitmap.Dispose()
}
$sizes = @(16,20,24,32,48,64,128,256)
$file = [IO.File]::Create((Join-Path $assetDir 'gigadock.ico'))
$writer = [IO.BinaryWriter]::new($file)
try {
    $writer.Write([uint16]0); $writer.Write([uint16]1); $writer.Write([uint16]$frames.Count)
    $offset = 6 + 16 * $frames.Count
    for ($i=0; $i -lt $frames.Count; $i++) {
        $dimension = if ($sizes[$i] -eq 256) { 0 } else { $sizes[$i] }
        $writer.Write([byte]$dimension); $writer.Write([byte]$dimension)
        $writer.Write([byte]0); $writer.Write([byte]0)
        $writer.Write([uint16]1); $writer.Write([uint16]32)
        $writer.Write([uint32]$frames[$i].Length); $writer.Write([uint32]$offset)
        $offset += $frames[$i].Length
    }
    foreach ($frame in $frames) { $writer.Write([byte[]]$frame) }
} finally { $writer.Dispose() }
