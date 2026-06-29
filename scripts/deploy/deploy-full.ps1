# ============================================================
# BAŞKENT ENERJİ — TAM DEPLOY (build + publish + deploy)
# Kullanım: .\deploy-full.ps1
# Yönetici olarak çalıştır!
# ============================================================
$ErrorActionPreference = "Stop"

$repoRoot = "C:\inetpub\baskent-enerji-doviz"
$publishDir = Join-Path $repoRoot "deploy\publish-api"
$apiProject = Join-Path $repoRoot "BaskentEnerji.API\BaskentEnerji.API.csproj"

Write-Host "=== BAŞKENT ENERJİ FULL DEPLOY ===" -ForegroundColor Cyan
Write-Host "Repo: $repoRoot" -ForegroundColor Gray

# 1) Temizle
Write-Host "`n[1/3] Publish klasörü temizleniyor..." -ForegroundColor Yellow
if (Test-Path $publishDir) {
    Remove-Item -Recurse -Force $publishDir
}
New-Item -ItemType Directory -Path $publishDir -Force | Out-Null

# 2) Build & Publish
Write-Host "[2/3] dotnet publish..." -ForegroundColor Yellow
dotnet publish $apiProject -c Release -r win-x64 --no-self-contained -o $publishDir
if ($LASTEXITCODE -ne 0) {
    Write-Host "dotnet publish BAŞARISIZ!" -ForegroundColor Red
    exit 1
}
Write-Host "  Publish başarılı → $publishDir" -ForegroundColor Green

# 3) Deploy
Write-Host "[3/3] Deploy (IIS'e kopyalama)..." -ForegroundColor Yellow
& "$repoRoot\deploy\deploy-api-to-canli.ps1"
if ($LASTEXITCODE -ne 0) {
    Write-Host "Deploy BAŞARISIZ!" -ForegroundColor Red
    exit 1
}

Write-Host "`n✅ DEPLOY TAMAMLANDI" -ForegroundColor Green
Write-Host "Health check: https://api.baskentenerji.com/health" -ForegroundColor Cyan
