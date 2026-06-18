$projectRoot = "C:\inetpub\baskent-enerji-doviz"
$apiProject = "$projectRoot\BaskentEnerji.API\BaskentEnerji.API.csproj"
$publishDir = "$projectRoot\_publish_output"
$deployDir = "C:\inetpub\wwwroot\api"

Write-Host "=== API Build & Deploy ===" -ForegroundColor Cyan

# dotnet kontrol
$dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
if (-not $dotnet) {
    Write-Host "[HATA] dotnet bulunamadi! .NET 8 SDK yuklu olmali." -ForegroundColor Red
    exit 1
}
Write-Host "[OK] dotnet: $($dotnet.Source)" -ForegroundColor Green
& dotnet --version

# App Pool durdur
Write-Host "`n--- App Pool durduruluyor ---" -ForegroundColor Yellow
& "$env:windir\system32\inetsrv\appcmd.exe" stop apppool /apppool.name:"BaskentEnerji-API"
Start-Sleep -Seconds 2

# Publish
Write-Host "`n--- Build & Publish ---" -ForegroundColor Yellow
if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force }

& dotnet publish $apiProject `
    --configuration Release `
    --runtime win-x64 `
    --self-contained false `
    --output $publishDir `
    --no-restore 2>&1

if ($LASTEXITCODE -ne 0) {
    Write-Host "[HATA] dotnet publish basarisiz (exit $LASTEXITCODE)" -ForegroundColor Red
    # App pool'u yeniden baslat
    & "$env:windir\system32\inetsrv\appcmd.exe" start apppool /apppool.name:"BaskentEnerji-API"
    exit 1
}
Write-Host "[OK] Publish tamamlandi." -ForegroundColor Green

# Deploy — appsettings.json ve web.config koruyarak kopyala
Write-Host "`n--- Deploy: $publishDir -> $deployDir ---" -ForegroundColor Yellow

# Geri yuklenmeyecek dosyalar (env var'dan okunan config)
$excludeFiles = @("appsettings.json", "appsettings.Development.json", "web.config")

Get-ChildItem $publishDir | ForEach-Object {
    if ($excludeFiles -notcontains $_.Name) {
        Copy-Item $_.FullName -Destination $deployDir -Recurse -Force
    } else {
        Write-Host "  [ATLA] $($_.Name)" -ForegroundColor DarkGray
    }
}
Write-Host "[OK] Dosyalar kopyalandi." -ForegroundColor Green

# App Pool baslat
Write-Host "`n--- App Pool baslatiliyor ---" -ForegroundColor Yellow
Start-Sleep -Seconds 1
& "$env:windir\system32\inetsrv\appcmd.exe" start apppool /apppool.name:"BaskentEnerji-API"
Start-Sleep -Seconds 3

$status = & "$env:windir\system32\inetsrv\appcmd.exe" list apppool "BaskentEnerji-API"
Write-Host $status -ForegroundColor Cyan
Write-Host "[BITTI] Deploy tamamlandi." -ForegroundColor Green
