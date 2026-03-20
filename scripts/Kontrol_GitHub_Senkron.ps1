# Periyodik GitHub senkron kontrolu (ONERILER.md - Madde 5)
# Ayda bir calistirin: yerel ana-repo ile origin/BASKENT-DOVIZ farkini gosterir.

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot
$anaRepo = Join-Path $root "ana-repo"

if (-not (Test-Path (Join-Path $anaRepo ".git"))) {
    Write-Host "HATA: ana-repo icinde .git yok." -ForegroundColor Red
    exit 1
}

Push-Location $anaRepo
try {
    Write-Host "=== GitHub fetch ===" -ForegroundColor Cyan
    git fetch origin 2>&1
    Write-Host ""
    Write-Host "=== Branch durumu ===" -ForegroundColor Cyan
    git status -sb
    Write-Host ""
    Write-Host "=== Yerel son 3 commit ===" -ForegroundColor Cyan
    git log --oneline -3 HEAD
    Write-Host ""
    Write-Host "=== Uzak (origin/BASKENT-DOVIZ) son 3 commit ===" -ForegroundColor Cyan
    git log --oneline -3 origin/BASKENT-DOVIZ
    Write-Host ""
    $behind = (git rev-list --count HEAD..origin/BASKENT-DOVIZ 2>$null)
    $ahead = (git rev-list --count origin/BASKENT-DOVIZ..HEAD 2>$null)
    if ([int]$behind -gt 0) { Write-Host "UYARI: Yerel $behind commit geride. git pull origin BASKENT-DOVIZ onerilir." -ForegroundColor Yellow }
    if ([int]$ahead -gt 0) { Write-Host "BILGI: Yerel $ahead commit ileride. git push origin BASKENT-DOVIZ ile gonderin." -ForegroundColor Green }
    if ([int]$behind -eq 0 -and [int]$ahead -eq 0) { Write-Host "Senkron: Yerel ve GitHub ayni." -ForegroundColor Green }
} finally {
    Pop-Location
}
