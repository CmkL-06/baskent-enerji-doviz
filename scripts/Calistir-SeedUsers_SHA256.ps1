# SeedUsers SHA256 guncellemesi - Veritabaninin oldugu sunucuda calistirin.
# Kullanim: .\Calistir-SeedUsers_SHA256.ps1
# Gereksinim: sqlcmd (SQL Server kurulu), mtt-moneyexchangeturkey DB erisimi

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$sqlFile = Join-Path $scriptDir "SeedUsers_SHA256_Update.sql"

if (-not (Test-Path $sqlFile)) {
    Write-Host "Hata: $sqlFile bulunamadi." -ForegroundColor Red
    exit 1
}

# sqlcmd veritabani: mtt-moneyexchangeturkey (canli appsettings ile ayni)
$server = "localhost\SQLEXPRESS"
$user = "mtturkey_exchange"
$pass = '*cc_pPMmHu79ka7q'
$database = "mtt-moneyexchangeturkey"

Write-Host "Calistiriliyor: $sqlFile" -ForegroundColor Cyan
Write-Host "Server: $server , DB: $database (mtt-moneyexchangeturkey)" -ForegroundColor Gray

& sqlcmd -S $server -U $user -P $pass -d "mtt-moneyexchangeturkey" -i $sqlFile
if ($LASTEXITCODE -ne 0) {
    Write-Host "sqlcmd hata verdi. Windows kimlik dogrulamasi denemek icin:" -ForegroundColor Yellow
    Write-Host "  sqlcmd -S $server -E -d mtt-moneyexchangeturkey -i `"$sqlFile`"" -ForegroundColor Gray
    exit 1
}

Write-Host "Tamamlandi. Giris testi: ihtiyar / owner1" -ForegroundColor Green
