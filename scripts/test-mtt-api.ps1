# test-mtt-api.ps1
# MoneyTransfer-API sitesine localhost binding ekler ve API'yi test eder
Import-Module WebAdministration

$site = "MoneyTransfer-API"
$port = 5200

# 1. Localhost binding ekle (yoksa)
$hasLocalhost = Get-WebBinding -Name $site | Where-Object { $_.bindingInformation -like "*:${port}:" }
if (-not $hasLocalhost) {
    New-WebBinding -Name $site -Protocol "http" -Port $port -IPAddress "*" -HostHeader ""
    Write-Host "Localhost binding eklendi." -ForegroundColor Green
    Restart-WebItem "IIS:\Sites\$site"
    Start-Sleep -Seconds 2
}

$base = "http://localhost:$port"

Write-Host ""
Write-Host "=== MoneyTransfer.API Test ===" -ForegroundColor Cyan

# 2. Gecersiz login (401 beklenir)
Write-Host "[TEST 1] Login (yanlis sifre) -> 401 beklenir..." -ForegroundColor Yellow
try {
    $body = '{"username":"nouser","password":"wrong","panelType":"operator"}'
    $r = Invoke-WebRequest -Uri "$base/api/auth/login" -Method POST `
        -ContentType "application/json" -Body $body -ErrorAction Stop
    Write-Host "      HTTP $($r.StatusCode) - BEKLENMEDIK (200 olmamali)" -ForegroundColor Red
} catch {
    $code = $_.Exception.Response.StatusCode.value__
    if ($code -eq 401) {
        Write-Host "      HTTP 401 - OK (yanlis sifre reddedildi)" -ForegroundColor Green
    } else {
        Write-Host "      HTTP $code - HATA: $($_.Exception.Message)" -ForegroundColor Red
    }
}

# 3. Veritabani migration kontrolu
Write-Host "[TEST 2] DB migration kontrolu..." -ForegroundColor Yellow
try {
    # /api/dealer/dashboard -> 401 beklenir (token yok ama API calisiyor demek)
    $r2 = Invoke-WebRequest -Uri "$base/api/dealer/dashboard" -Method GET -ErrorAction Stop
    Write-Host "      HTTP $($r2.StatusCode)" -ForegroundColor Yellow
} catch {
    $code2 = $_.Exception.Response.StatusCode.value__
    if ($code2 -eq 401) {
        Write-Host "      HTTP 401 - OK (JWT koruyor, DB baglantisi calisiyor)" -ForegroundColor Green
    } elseif ($code2 -eq 500) {
        Write-Host "      HTTP 500 - HATA: Muhtemelen DB baglanamadi!" -ForegroundColor Red
        Write-Host "      MTT_DB_CONNECTION kontrol edin." -ForegroundColor Yellow
    } else {
        Write-Host "      HTTP $code2 - $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "Binding durumu:" -ForegroundColor Cyan
Get-WebBinding -Name $site | Select-Object protocol, bindingInformation | Format-Table -AutoSize

pause
