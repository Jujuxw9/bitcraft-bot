# Génère le logo du bot (arbre-robot dans un hexagone) et le fichier .ico du programme.
# Usage : powershell -ExecutionPolicy Bypass -File tools/generer-icone.ps1
# Sortie : src/BitcraftBot.App/Assets/bitcraftbot.ico et bitcraftbot.png (aperçu 256 px).

Add-Type -AssemblyName System.Drawing
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$assets = Join-Path $root 'src\BitcraftBot.App\Assets'
New-Item -ItemType Directory -Force $assets | Out-Null

function New-Color([string]$hex) { [System.Drawing.ColorTranslator]::FromHtml($hex) }

# Hexagone « pointe en haut », centré, de rayon r.
function Get-Hexagon([double]$cx, [double]$cy, [double]$r) {
    $pts = New-Object 'System.Drawing.PointF[]' 6
    for ($i = 0; $i -lt 6; $i++) {
        $a = [Math]::PI / 180 * (60 * $i - 90)
        $pts[$i] = New-Object System.Drawing.PointF ([float]($cx + $r * [Math]::Cos($a))), ([float]($cy + $r * [Math]::Sin($a)))
    }
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $path.AddPolygon($pts)
    return $path
}

# Dessine le logo à la taille demandée. Les petites tailles sont simplifiées.
function New-Logo([int]$size) {
    $bmp = New-Object System.Drawing.Bitmap $size, $size, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = 'AntiAlias'
    $g.Clear([System.Drawing.Color]::Transparent)
    $s = $size / 256.0
    $small = $size -le 32

    # Fond : hexagone bleu nuit avec dégradé.
    $hex = Get-Hexagon (128 * $s) (128 * $s) (122 * $s)
    $bg = New-Object System.Drawing.Drawing2D.LinearGradientBrush (New-Object System.Drawing.PointF 0, 0), (New-Object System.Drawing.PointF 0, $size), (New-Color '#2B4C7E'), (New-Color '#14213D')
    $g.FillPath($bg, $hex)

    # Contour vert vif : clin d'œil à la sélection au survol dans le jeu.
    $outline = New-Object System.Drawing.Pen (New-Color '#3DF58A'), ([float][Math]::Max(1.5, 9 * $s))
    $outline.LineJoin = 'Round'
    $g.DrawPath($outline, $hex)

    # Tronc.
    $trunk = New-Object System.Drawing.SolidBrush (New-Color '#8B5A2B')
    $g.FillRectangle($trunk, [float](114 * $s), [float](140 * $s), [float](28 * $s), [float](70 * $s))
    if (-not $small) {
        # Racines.
        $g.FillPolygon($trunk, [System.Drawing.PointF[]]@(
            (New-Object System.Drawing.PointF ([float](100 * $s)), ([float](212 * $s))),
            (New-Object System.Drawing.PointF ([float](114 * $s)), ([float](190 * $s))),
            (New-Object System.Drawing.PointF ([float](142 * $s)), ([float](190 * $s))),
            (New-Object System.Drawing.PointF ([float](156 * $s)), ([float](212 * $s)))))
    }

    # Feuillage : trois cercles, du plus sombre au plus clair.
    $dark = New-Object System.Drawing.SolidBrush (New-Color '#1E8449')
    $mid = New-Object System.Drawing.SolidBrush (New-Color '#27AE60')
    $light = New-Object System.Drawing.SolidBrush (New-Color '#58D68D')
    $g.FillEllipse($dark, [float](58 * $s), [float](84 * $s), [float](76 * $s), [float](76 * $s))
    $g.FillEllipse($dark, [float](122 * $s), [float](84 * $s), [float](76 * $s), [float](76 * $s))
    $g.FillEllipse($mid, [float](78 * $s), [float](44 * $s), [float](100 * $s), [float](100 * $s))
    if (-not $small) {
        $g.FillEllipse($light, [float](96 * $s), [float](56 * $s), [float](34 * $s), [float](28 * $s))
    }

    # Visière de robot avec deux yeux lumineux.
    $visor = New-Object System.Drawing.SolidBrush (New-Color '#0B1426')
    $vx = 88 * $s; $vy = 92 * $s; $vw = 80 * $s; $vh = 30 * $s
    $visorPath = New-Object System.Drawing.Drawing2D.GraphicsPath
    $rad = 15 * $s
    $visorPath.AddArc([float]$vx, [float]$vy, [float](2 * $rad), [float]$vh, 90, 180)
    $visorPath.AddArc([float]($vx + $vw - 2 * $rad), [float]$vy, [float](2 * $rad), [float]$vh, 270, 180)
    $visorPath.CloseFigure()
    $g.FillPath($visor, $visorPath)
    $eye = New-Object System.Drawing.SolidBrush (New-Color '#FFB238')
    $eyeSize = [Math]::Max(2, 16 * $s)
    $g.FillEllipse($eye, [float](106 * $s), [float](99 * $s), [float]$eyeSize, [float]$eyeSize)
    $g.FillEllipse($eye, [float](134 * $s), [float](99 * $s), [float]$eyeSize, [float]$eyeSize)

    if (-not $small) {
        # Petite antenne sur le dessus du feuillage.
        $antenna = New-Object System.Drawing.Pen (New-Color '#0B1426'), ([float](5 * $s))
        $g.DrawLine($antenna, [float](128 * $s), [float](46 * $s), [float](128 * $s), [float](26 * $s))
        $g.FillEllipse($eye, [float](120 * $s), [float](16 * $s), [float](16 * $s), [float](16 * $s))
    }

    $g.Dispose()
    return $bmp
}

# Construit un .ico contenant plusieurs tailles (chaque image enregistrée en PNG).
$sizes = 256, 128, 64, 48, 32, 24, 16
# 256 px en PNG ; les autres tailles en BMP 32 bits, lisible par tous les outils Windows.
function Get-DibBytes([System.Drawing.Bitmap]$bmp) {
    $n = $bmp.Width
    $ms = New-Object System.IO.MemoryStream
    $bw = New-Object System.IO.BinaryWriter $ms
    # En-tête BITMAPINFOHEADER : la hauteur compte deux fois (image + masque).
    $bw.Write([uint32]40); $bw.Write([int32]$n); $bw.Write([int32]($n * 2))
    $bw.Write([uint16]1); $bw.Write([uint16]32); $bw.Write([uint32]0)
    $bw.Write([uint32]0); $bw.Write([int32]0); $bw.Write([int32]0); $bw.Write([uint32]0); $bw.Write([uint32]0)
    # Pixels BGRA, de la ligne du bas vers la ligne du haut.
    for ($y = $n - 1; $y -ge 0; $y--) {
        for ($x = 0; $x -lt $n; $x++) {
            $c = $bmp.GetPixel($x, $y)
            $bw.Write([byte]$c.B); $bw.Write([byte]$c.G); $bw.Write([byte]$c.R); $bw.Write([byte]$c.A)
        }
    }
    # Masque de transparence 1 bit (vide : la transparence vient du canal alpha).
    $maskRow = [int]([Math]::Ceiling($n / 32.0) * 4)
    $bw.Write((New-Object byte[] ($maskRow * $n)))
    $bw.Flush()
    # La virgule empêche PowerShell de dérouler le tableau d'octets.
    return , $ms.ToArray()
}

$images = foreach ($size in $sizes) {
    $bmp = New-Logo $size
    if ($size -eq 256) {
        $ms = New-Object System.IO.MemoryStream
        $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
        $bmp.Save((Join-Path $assets 'bitcraftbot.png'), [System.Drawing.Imaging.ImageFormat]::Png)
        $bytes = $ms.ToArray()
    } else {
        $bytes = Get-DibBytes $bmp
    }
    $bmp.Dispose()
    [pscustomobject]@{ Size = $size; Bytes = $bytes }
}

$out = New-Object System.IO.MemoryStream
$w = New-Object System.IO.BinaryWriter $out
$w.Write([uint16]0); $w.Write([uint16]1); $w.Write([uint16]$images.Count)
$offset = 6 + 16 * $images.Count
foreach ($img in $images) {
    $dim = if ($img.Size -ge 256) { 0 } else { $img.Size }
    $w.Write([byte]$dim); $w.Write([byte]$dim); $w.Write([byte]0); $w.Write([byte]0)
    $w.Write([uint16]1); $w.Write([uint16]32)
    $w.Write([uint32]$img.Bytes.Length); $w.Write([uint32]$offset)
    $offset += $img.Bytes.Length
}
foreach ($img in $images) { $w.Write($img.Bytes) }
$w.Flush()
[System.IO.File]::WriteAllBytes((Join-Path $assets 'bitcraftbot.ico'), $out.ToArray())
Write-Host "Icône générée dans $assets"
