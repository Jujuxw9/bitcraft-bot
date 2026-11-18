# Fabrique le .exe de la version en cours et le range (D-040).
#   artifacts/release/ : toujours la dernière version (BitcraftBot.exe, config.local.json,
#                        donnees-jeu/, journaux/ de toutes les versions, rangés par version).
#   artifacts/old/vX.Y.Z/BitcraftBot.exe : les versions précédentes.
# La version est lue dans Directory.Build.props.
# Usage : powershell -ExecutionPolicy Bypass -File tools/publier-version.ps1

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$artifacts = Join-Path $root 'artifacts'
$release = Join-Path $artifacts 'release'
$old = Join-Path $artifacts 'old'
$build = Join-Path $artifacts 'build'

[xml]$props = Get-Content (Join-Path $root 'Directory.Build.props') -Encoding UTF8
$version = ($props.Project.PropertyGroup.Version | Where-Object { $_ }) | Select-Object -First 1
if (-not $version) { throw "Version introuvable dans Directory.Build.props." }

Write-Host "Fabrication de la version $version..."
if (Test-Path $build) { Remove-Item -Recurse -Force $build }
dotnet publish (Join-Path $root 'src\BitcraftBot.App') -c Release -r win-x64 `
    --self-contained true -p:PublishSingleFile=true -o $build
if ($LASTEXITCODE -ne 0) { throw "Échec de la fabrication." }

$newExe = Join-Path $build 'BitcraftBot.exe'
$sizeMb = (Get-Item $newExe).Length / 1MB
if ($sizeMb -gt 100) { Write-Warning ("Le .exe fait {0:N1} Mo : trop gros pour une Release GitHub (100 Mo)." -f $sizeMb) }

# L'ancienne version de release/ part dans old/vX.Y.Z/.
New-Item -ItemType Directory -Force $release, $old | Out-Null
$currentExe = Join-Path $release 'BitcraftBot.exe'
if (Test-Path $currentExe) {
    $current = (Get-Item $currentExe).VersionInfo.ProductVersion -replace '\+.*$', ''
    if (-not $current) { $current = 'inconnue' }
    if ($current -ne $version) {
        $dest = Join-Path $old "v$current"
        New-Item -ItemType Directory -Force $dest | Out-Null
        Move-Item $currentExe (Join-Path $dest 'BitcraftBot.exe') -Force
        Write-Host "Ancienne version v$current rangée dans $dest"
    }
}

Copy-Item $newExe $currentExe -Force
Remove-Item -Recurse -Force $build
Write-Host ("OK : v{0} dans {1} ({2:N1} Mo)." -f $version, $release, $sizeMb)
