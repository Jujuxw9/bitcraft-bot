# Lance la dernière version du bot (artifacts\release\BitcraftBot-vX.Y.Z.exe, numéro le plus haut).
# Utilisé par le raccourci « BitCraft Bot » du bureau (voir tools\creer-raccourci-bureau.ps1).
$ErrorActionPreference = 'Stop'
$release = Join-Path (Split-Path -Parent $PSScriptRoot) 'artifacts\release'
$latest = Get-ChildItem -Path $release -Filter 'BitcraftBot-v*.exe' -ErrorAction SilentlyContinue |
    Where-Object { $_.BaseName -match '^BitcraftBot-v(\d+\.\d+\.\d+)$' } |
    Sort-Object { [version]($_.BaseName -replace '^BitcraftBot-v', '') } -Descending |
    Select-Object -First 1
if ($null -eq $latest) {
    Add-Type -AssemblyName PresentationFramework
    [System.Windows.MessageBox]::Show("Aucune version du bot dans :`n$release", 'BitCraft Bot') | Out-Null
    exit 1
}
Start-Process -FilePath $latest.FullName -WorkingDirectory $release
