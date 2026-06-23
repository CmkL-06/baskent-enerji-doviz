# deploy-mtt-api.ps1
# MoneyTransfer.API Build ve Deploy
# api.moneytransferturkey.com -> C:\inetpub\moneytransfer-api\

param(
    [string]$SourceDir  = "C:\inetpub\baskent-enerji-doviz\mtt-api",
    [string]$PublishDir = "C:\inetpub\moneytransfer-api",
    [string]$SiteName   = "MoneyTransfer-API",
    [string]$PoolName   = "MoneyTransfer-API-Pool",
    [int]   $Port       = 5200
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "  MoneyTransfer.API - Build ve Deploy" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""

# 1. Ortam degiskenlerini kontrol et
Write-Host "[1/7] Ortam degiskenleri kontrol ediliyor..." -ForegroundColor Yellow

$dbConn = [System.Environment]::GetEnvironmentVariable("MTT_DB_CONNECTION", "Machine")
$jwtKey = [System.Environment]::GetEnvironmentVariable("MTT_JWT_SECRET",    "Machine")

if (-not $dbConn) {
    Write-Host "      HATA: MTT_DB_CONNECTION tanimli degil!" -ForegroundColor Red
    Write-Host "      Once run-set-mtt-env.bat calistirin." -ForegroundColor Yellow
    pause
    exit 1
}

if (-not $jwtKey) {
    Write-Host "      HATA: MTT_JWT_SECRET tanimli degil!" -ForegroundColor Red
    Write-Host "      Once run-set-mtt-env.bat calistirin." -ForegroundColor Yellow
    pause
    exit 1
}

Write-Host "      MTT_DB_CONNECTION: Tanimli OK" -ForegroundColor Green
Write-Host "      MTT_JWT_SECRET:    Tanimli OK" -ForegroundColor Green

# 2. Dotnet CLI kontrol
Write-Host "[2/7] .NET SDK kontrol ediliyor..." -ForegroundColor Yellow
$dotnetPath = Get-Command dotnet -ErrorAction SilentlyContinue
if (-not $dotnetPath) {
    Write-Host "      HATA: dotnet CLI bulunamadi! .NET 8 SDK yukleyin." -ForegroundColor Red
    pause
    exit 1
}
$dotnetVer = (dotnet --version 2>&1).ToString()
Write-Host "      .NET SDK: $dotnetVer OK" -ForegroundColor Green

# 3. Build
Write-Host "[3/7] Proje build ediliyor..." -ForegroundColor Yellow
Push-Location $SourceDir
dotnet restore --no-cache
dotnet publish -c Release -o $PublishDir --no-self-contained
$buildResult = $LASTEXITCODE
Pop-Location

if ($buildResult -ne 0) {
    Write-Host "      HATA: Build basarisiz!" -ForegroundColor Red
    pause
    exit 1
}
Write-Host "      Build tamamlandi OK" -ForegroundColor Green

# 4. IIS modulu yukle
Write-Host "[4/7] IIS WebAdministration yukleniyor..." -ForegroundColor Yellow
Import-Module WebAdministration
Write-Host "      Yuklendi OK" -ForegroundColor Green

# 5. App Pool
Write-Host "[5/7] App Pool: $PoolName" -ForegroundColor Yellow
if (-not (Test-Path "IIS:\AppPools\$PoolName")) {
    New-WebAppPool -Name $PoolName
    Write-Host "      App Pool olusturuldu." -ForegroundColor Green
} else {
    Write-Host "      App Pool zaten mevcut." -ForegroundColor Green
}
Set-ItemProperty "IIS:\AppPools\$PoolName" -Name managedRuntimeVersion -Value ""
Set-ItemProperty "IIS:\AppPools\$PoolName" -Name enable32BitAppOnWin64 -Value $false
Write-Host "      App Pool ayarlandi OK" -ForegroundColor Green

# 6. IIS Site
Write-Host "[6/7] IIS Site: $SiteName" -ForegroundColor Yellow
$existingSite = Get-Website -Name $SiteName -ErrorAction SilentlyContinue

if ($null -eq $existingSite) {
    New-Website -Name $SiteName `
        -PhysicalPath $PublishDir `
        -ApplicationPool $PoolName `
        -Port $Port `
        -HostHeader "api.moneytransferturkey.com"
    Write-Host "      Site olusturuldu (Port: $Port) OK" -ForegroundColor Green
} else {
    Set-ItemProperty "IIS:\Sites\$SiteName" -Name physicalPath    -Value $PublishDir
    Set-ItemProperty "IIS:\Sites\$SiteName" -Name applicationPool -Value $PoolName
    Write-Host "      Site guncellendi OK" -ForegroundColor Green
}

# 7. Yeniden baslat
Write-Host "[7/7] Site yeniden baslatiliyor..." -ForegroundColor Yellow
Stop-Website -Name $SiteName -ErrorAction SilentlyContinue
Start-WebAppPool -Name $PoolName -ErrorAction SilentlyContinue
Start-Website -Name $SiteName
Write-Host "      Site baslatildi OK" -ForegroundColor Green

Write-Host ""
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "  Deploy tamamlandi!" -ForegroundColor Green
Write-Host ""
Write-Host "  API: http://localhost:$Port" -ForegroundColor White
Write-Host "  PRD: https://api.moneytransferturkey.com" -ForegroundColor White
Write-Host ""
Write-Host "  POST /api/auth/login" -ForegroundColor Gray
Write-Host "  GET  /api/dealer/dashboard" -ForegroundColor Gray
Write-Host "  GET  /api/operator/transactions" -ForegroundColor Gray
Write-Host "  GET  /api/admin/dashboard" -ForegroundColor Gray
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""

pause
