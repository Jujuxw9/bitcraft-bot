# Vérification automatique de la mémoire du projet (dossier memory/ et CLAUDE.md).
# Contrôles mécaniques seulement (le reste, le sens, est fait par l'agent mémoire : voir PROMPT.md).
# Usage : powershell -ExecutionPolicy Bypass -File tools/agent-memoire/verifier-memoire.ps1
# Sortie : une ligne par problème, « ERREUR » (à corriger) ou « ATTENTION » (à regarder). Code de sortie 1 s'il y a une erreur.

$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [Text.Encoding]::UTF8
$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$memory = Join-Path $root 'memory'
$errors = 0
$warnings = 0

function Report([string]$level, [string]$file, [string]$message) {
    $relative = $file.Replace($root + '\', '').Replace('\', '/')
    Write-Host "$level  $relative : $message"
    if ($level -eq 'ERREUR') { $script:errors++ } else { $script:warnings++ }
}

function Read-Text([string]$path) { [IO.File]::ReadAllText($path, [Text.Encoding]::UTF8) }

$files = @(Get-ChildItem $memory -Recurse -Filter *.md) + @(Get-Item (Join-Path $root 'CLAUDE.md'))

# 1. Liens Markdown vers des fichiers qui n'existent pas.
foreach ($f in $files) {
    $text = Read-Text $f.FullName
    # On ignore les blocs de code (gabarits, exemples).
    $text = [regex]::Replace($text, '(?s)```.*?```', '')
    foreach ($m in [regex]::Matches($text, '\]\(([^)\s]+)\)')) {
        $target = $m.Groups[1].Value
        if ($target -match '^(https?:|mailto:|#)') { continue }
        $target = ($target -split '#')[0]
        if ($target -eq '') { continue }
        $target = [Uri]::UnescapeDataString($target)
        $full = [IO.Path]::GetFullPath((Join-Path $f.DirectoryName $target))
        if (-not (Test-Path $full)) { Report 'ERREUR' $f.FullName "lien cassé vers « $target »" }
    }
}

# 2. Décisions et questions : un fichier par ID, ID unique, listé dans l'index avec le même statut.
function Check-Series([string]$folder, [string]$prefix) {
    $dir = Join-Path $memory $folder
    $index = Read-Text (Join-Path $dir 'README.md')
    $ids = @{}
    foreach ($f in Get-ChildItem $dir -Filter "$prefix-*.md") {
        if ($f.Name -notmatch "^$prefix-(\d{3})-") { Report 'ERREUR' $f.FullName "nom hors format $prefix-NNN-sujet.md"; continue }
        $id = "$prefix-$($Matches[1])"
        if ($ids.ContainsKey($id)) { Report 'ERREUR' $f.FullName "identifiant $id en double (aussi : $($ids[$id]))" }
        $ids[$id] = $f.Name
        if ($index -notmatch [regex]::Escape("($($f.Name))")) { Report 'ERREUR' (Join-Path $dir 'README.md') "$id absent de l'index" }
        $text = Read-Text $f.FullName
        if ($text -notmatch "^#\s*$id\b") { Report 'ATTENTION' $f.FullName "le titre ne commence pas par « # $id »" }
        if ($text -match '\*\*Statut\*\*\s*:\s*([^\r\n]+)') {
            $status = $Matches[1].Trim()
            $row = ($index -split "`n") | Where-Object { $_ -match [regex]::Escape("[$id]") } | Select-Object -First 1
            $first = ($status -split '[\s(]')[0]
            if ($row -and $row -notmatch [regex]::Escape($first)) { Report 'ATTENTION' $f.FullName "statut « $status » différent de l'index" }
            foreach ($ref in [regex]::Matches($status, '(D|Q)-\d{3}')) {
                $refDir = if ($ref.Value.StartsWith('D')) { 'decisions' } else { 'questions-ouvertes' }
                if (-not (Get-ChildItem (Join-Path $memory $refDir) -Filter "$($ref.Value)-*.md")) { Report 'ERREUR' $f.FullName "statut renvoie vers $($ref.Value), qui n'existe pas" }
            }
        } else { Report 'ATTENTION' $f.FullName "pas de ligne « **Statut** : … »" }
    }
    # Trous dans la numérotation (un ID n'est jamais réutilisé : un trou est suspect).
    $numbers = $ids.Keys | ForEach-Object { [int]$_.Substring(2) } | Sort-Object
    if ($numbers) {
        for ($n = 1; $n -le $numbers[-1]; $n++) {
            if ($numbers -notcontains $n) { Report 'ATTENTION' $dir ("{0}-{1:000} manquant dans la suite" -f $prefix, $n) }
        }
    }
    # Lignes de l'index sans fichier.
    foreach ($m in [regex]::Matches($index, "\[($prefix-\d{3})\]\(([^)]+)\)")) {
        if (-not (Test-Path (Join-Path $dir $m.Groups[2].Value))) { Report 'ERREUR' (Join-Path $dir 'README.md') "$($m.Groups[1].Value) listé mais fichier absent" }
    }
}
Check-Series 'decisions' 'D'
Check-Series 'questions-ouvertes' 'Q'

# 3. Chaque fichier d'un dossier est cité dans le README.md de ce dossier, ou dans memory/README.md.
$mainIndex = Read-Text (Join-Path $memory 'README.md')
foreach ($dir in Get-ChildItem $memory -Directory -Recurse) {
    $localIndex = Join-Path $dir.FullName 'README.md'
    $local = if (Test-Path $localIndex) { Read-Text $localIndex } else { '' }
    foreach ($f in Get-ChildItem $dir.FullName -Filter *.md -File) {
        if ($f.Name -eq 'README.md') { continue }
        if ($local -notmatch [regex]::Escape($f.Name) -and $mainIndex -notmatch [regex]::Escape($f.Name)) {
            Report 'ERREUR' $f.FullName "pas cité dans un index (README.md du dossier ou memory/README.md)"
        }
    }
}

# 4. Résumé global : 10 lignes numérotées au plus (CLAUDE.md).
$summary = Join-Path $memory 'echanges/resume-global.md'
$numbered = @((Get-Content $summary -Encoding UTF8) | Where-Object { $_ -match '^\d+\.' }).Count
if ($numbered -gt 10) { Report 'ERREUR' $summary "$numbered lignes numérotées (10 au plus)" }

# 5. Fichiers trop longs (règle de découpage : ~300 lignes).
foreach ($f in $files) {
    $lines = @(Get-Content $f.FullName -Encoding UTF8).Count
    if ($lines -gt 300) { Report 'ATTENTION' $f.FullName "$lines lignes (découper au-delà de ~300)" }
}

# 6. Version : Directory.Build.props, résumé global et dernier .exe local doivent concorder.
[xml]$props = Get-Content (Join-Path $root 'Directory.Build.props') -Encoding UTF8
$version = ($props.Project.PropertyGroup.Version | Where-Object { $_ }) | Select-Object -First 1
if ((Read-Text $summary) -notmatch [regex]::Escape("v$version")) { Report 'ATTENTION' $summary "ne cite pas la version en cours v$version" }
$exe = Join-Path $root "artifacts/release/BitcraftBot-v$version.exe"
if (-not (Test-Path $exe)) { Report 'ATTENTION' $exe "absent : la version v$version n'est pas fabriquée" }

# 7. Jeton ou e-mail dans la mémoire : interdit (D-039).
foreach ($f in $files) {
    $text = Read-Text $f.FullName
    if ($text -match 'eyJ[A-Za-z0-9_-]{20,}\.[A-Za-z0-9_-]{20,}') { Report 'ERREUR' $f.FullName "ressemble à un jeton : à retirer tout de suite" }
    if ($text -match '[A-Za-z0-9._%+-]+@(gmail|hotmail|outlook|yahoo|orange|free)\.[a-z]{2,}') { Report 'ERREUR' $f.FullName "adresse e-mail personnelle : à retirer" }
}

Write-Host ""
Write-Host "Bilan : $errors erreur(s), $warnings point(s) d'attention, $($files.Count) fichiers lus."
if ($errors -gt 0) { exit 1 }
