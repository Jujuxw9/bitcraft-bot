# Publie le .exe d'une version validée par Julien et archive l'ancien (D-020).
# Usage : powershell -ExecutionPolicy Bypass -File tools/valider-exe.ps1 -Version 0.1.0

param(
    [Parameter(Mandatory = $true)]
    [string]$Version
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$exeDir = Join-Path $root 'executable'
$archiveDir = Join-Path $exeDir 'anciennes-versions'
$publishDir = Join-Path $root 'artifacts\publish'

Write-Host "Publication de la version $Version..."
dotnet publish (Join-Path $root 'src\BitcraftBot.App') -c Release -r win-x64 `
    --self-contained true -p:PublishSingleFile=true -p:Version=$Version -o $publishDir
if ($LASTEXITCODE -ne 0) { throw "Échec de la publication." }

$newExe = Join-Path $publishDir 'BitcraftBot.exe'
$sizeMb = (Get-Item $newExe).Length / 1MB
if ($sizeMb -gt 100) { throw ("Le .exe fait {0:N1} Mo : GitHub refuse les fichiers de plus de 100 Mo." -f $sizeMb) }
if ($sizeMb -gt 50) { Write-Warning ("Le .exe fait {0:N1} Mo (au-delà de 50 Mo, GitHub affiche un avertissement)." -f $sizeMb) }

# Archivage des anciennes versions (dossier ignoré par Git)
New-Item -ItemType Directory -Force $archiveDir | Out-Null
Get-ChildItem $exeDir -Filter 'BitcraftBot-v*.exe' | ForEach-Object {
    Write-Host "Archivage de $($_.Name)"
    Move-Item $_.FullName $archiveDir -Force
}

$target = Join-Path $exeDir "BitcraftBot-v$Version.exe"
Copy-Item $newExe $target
Write-Host ("OK : {0} ({1:N1} Mo). Pense à mettre à jour executable/README.md puis à commiter." -f $target, $sizeMb)
