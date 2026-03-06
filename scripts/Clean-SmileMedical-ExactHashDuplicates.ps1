# SmileMedical nested kopya temizligi
# - Varsayilan: DryRun (hicbir dosya silinmez)
# - -Apply: birebir hash eslesen nested kopyalari siler ve kalanlari _nested_conflict_review altina tasir

[CmdletBinding()]
param(
    [switch]$Apply,
    [string]$RepoPath = ""
)

$ErrorActionPreference = "Stop"

$repoRoot = if ($RepoPath) {
    $RepoPath
} else {
    (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
}

$analysisPath = Join-Path $repoRoot "docs/hash_analysis_20260306.json"
$smileRoot = Join-Path $repoRoot "SmileMedical"
$nestedRoot = Join-Path $smileRoot "SmileMedical"
$reviewRoot = Join-Path $smileRoot "_nested_conflict_review"

if (-not (Test-Path -LiteralPath $analysisPath)) {
    throw "Analiz dosyasi bulunamadi: $analysisPath"
}
if (-not (Test-Path -LiteralPath $smileRoot)) {
    throw "SmileMedical klasoru bulunamadi: $smileRoot"
}
if (-not (Test-Path -LiteralPath $nestedRoot)) {
    Write-Host "Nested klasor yok, temizlenecek kopya bulunmadi: $nestedRoot" -ForegroundColor Yellow
    exit 0
}

$analysis = Get-Content -Raw -LiteralPath $analysisPath | ConvertFrom-Json
$candidates = @($analysis.nested_same_delete_candidates)

if ($candidates.Count -eq 0) {
    Write-Host "Aday liste bos, islem yok." -ForegroundColor Yellow
    exit 0
}

$eligible = 0
$hashMismatch = 0
$missing = 0
$errors = 0

Write-Host "Repo:   $repoRoot" -ForegroundColor Cyan
Write-Host "Aday:   $($candidates.Count)" -ForegroundColor Cyan
Write-Host "Mod:    $((if ($Apply) { 'APPLY' } else { 'DRYRUN' }))" -ForegroundColor Cyan

foreach ($rel in $candidates) {
    $nestedFile = Join-Path $nestedRoot $rel
    $outerFile = Join-Path $smileRoot $rel

    if (-not (Test-Path -LiteralPath $nestedFile) -or -not (Test-Path -LiteralPath $outerFile)) {
        $missing++
        continue
    }

    try {
        $nestedHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $nestedFile).Hash
        $outerHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $outerFile).Hash
    }
    catch {
        $errors++
        continue
    }

    if ($nestedHash -ne $outerHash) {
        $hashMismatch++
        continue
    }

    $eligible++
    if ($Apply) {
        Remove-Item -LiteralPath $nestedFile -Force
    }
}

if ($Apply) {
    # Bos klasorleri asagidan yukari temizle
    Get-ChildItem -LiteralPath $nestedRoot -Recurse -Directory |
        Sort-Object { $_.FullName.Length } -Descending |
        ForEach-Object {
            if (-not (Get-ChildItem -LiteralPath $_.FullName -Force)) {
                Remove-Item -LiteralPath $_.FullName -Force
            }
        }

    if (Test-Path -LiteralPath $nestedRoot) {
        if (Test-Path -LiteralPath $reviewRoot) {
            Write-Warning "_nested_conflict_review zaten var, otomatik tasima atladi: $reviewRoot"
        }
        else {
            Rename-Item -LiteralPath $nestedRoot -NewName "_nested_conflict_review"
            Write-Host "Kalan dosyalar _nested_conflict_review altina tasindi." -ForegroundColor Green
        }
    }
}

Write-Host ""
Write-Host "=== OZET ===" -ForegroundColor Cyan
Write-Host "Aday toplam:          $($candidates.Count)"
Write-Host "Birebir hash eslesen: $eligible"
Write-Host "Hash uyusmayan:       $hashMismatch"
Write-Host "Eksik dosya:          $missing"
Write-Host "Hata sayisi:          $errors"

if (-not $Apply) {
    Write-Host ""
    Write-Host "Gercek temizlik icin su komutu calistir:" -ForegroundColor Yellow
    Write-Host ".\\scripts\\Clean-SmileMedical-ExactHashDuplicates.ps1 -Apply"
}
