# quick-api-test.ps1

Import-Module WebAdministration

# localhost:5000 binding yoksa ekle
$site = "MoneyTransfer-API"
$existing = Get-WebBinding -Name $site | Where-Object { $_.bindingInformation -eq "*:5000:" }
if (-not $existing) {
    New-WebBinding -Name $site -Protocol http -Port 5000 -HostHeader "" -IPAddress "*"
    Write-Host "localhost:5000 binding eklendi." -ForegroundColor Green
} else {
    Write-Host "localhost:5000 binding zaten var." -ForegroundColor Gray
}

Start-Sleep -Seconds 2

Write-Host ""
Write-Host "=== API Login Test (localhost:5000) ===" -ForegroundColor Cyan
try {
    $resp = Invoke-RestMethod -Uri "http://localhost:5000/api/auth/login" `
        -Method POST -ContentType "application/json" `
        -Body '{"username":"x","password":"x","panelType":"operator"}' `
        -ErrorVariable restErr -ErrorAction SilentlyContinue
    Write-Host "200 OK: $($resp | ConvertTo-Json)" -ForegroundColor Green
} catch {
    Write-Host "StatusCode: $($_.Exception.Response.StatusCode.value__)" -ForegroundColor Yellow
    Write-Host "Mesaj: $($_.ErrorDetails.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "=== startup-error.txt ===" -ForegroundColor Cyan
$f = "C:\inetpub\moneytransfer-api\startup-error.txt"
if (Test-Path $f) { Get-Content $f } else { Write-Host "Yok - startup basarili." -ForegroundColor Green }

pause
