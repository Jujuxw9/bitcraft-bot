# Crée (ou recrée) le raccourci « BitCraft Bot » sur le bureau : il lance toujours la dernière version
# du dossier artifacts\release, quelle qu'elle soit (demande de Julien, v0.3.18).
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$launcher = Join-Path $PSScriptRoot 'lancer-bot.ps1'
$icon = Join-Path $root 'src\BitcraftBot.App\Assets\bitcraftbot.ico'
$path = Join-Path ([Environment]::GetFolderPath('Desktop')) 'BitCraft Bot.lnk'

$shell = New-Object -ComObject WScript.Shell
$link = $shell.CreateShortcut($path)
$link.TargetPath = Join-Path $env:SystemRoot 'System32\WindowsPowerShell\v1.0\powershell.exe'
$link.Arguments = "-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File `"$launcher`""
$link.WorkingDirectory = Join-Path $root 'artifacts\release'
$link.IconLocation = "$icon,0"
$link.Description = 'Lance la dernière version de BitCraft Bot'
$link.Save()
Write-Host "Raccourci créé : $path"
