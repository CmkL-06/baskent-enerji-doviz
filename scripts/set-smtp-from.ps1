# Smtp__From ve Smtp__FromName env var ayarla + app pool restart

[System.Environment]::SetEnvironmentVariable("Smtp__From", "info@baskentenerji.com", "Machine")
[System.Environment]::SetEnvironmentVariable("Smtp__FromName", "Baskent Enerji", "Machine")

Write-Host "Smtp__From  -> " -NoNewline
Write-Host ([System.Environment]::GetEnvironmentVariable("Smtp__From","Machine")) -ForegroundColor Green

Write-Host "Smtp__FromName -> " -NoNewline
Write-Host ([System.Environment]::GetEnvironmentVariable("Smtp__FromName","Machine")) -ForegroundColor Green

Write-Host ""
Write-Host "App Pool yeniden baslatiliyor..." -ForegroundColor Cyan
& "C:\Windows\System32\inetsrv\appcmd.exe" stop apppool /apppool.name:"BaskentEnerji-API"
Start-Sleep -Seconds 2
& "C:\Windows\System32\inetsrv\appcmd.exe" start apppool /apppool.name:"BaskentEnerji-API"
Write-Host "App Pool baslatildi." -ForegroundColor Green

Write-Host ""
Write-Host "ForgotPassword testi baslatiliyor..." -ForegroundColor Cyan
Start-Sleep -Seconds 3

$body = '{"email":"kulemlakyatirim.as@gmail.com"}'
try {
    $resp = Invoke-RestMethod -Uri "https://api.baskentenerji.com/api/v1/User/ForgotPassword" `
        -Method POST -ContentType "application/json" -Body $body -ErrorAction Stop
    Write-Host "API Yaniti: " -NoNewline
    Write-Host ($resp | ConvertTo-Json -Depth 3) -ForegroundColor Green
} catch {
    $status = $_.Exception.Response.StatusCode.value__
    $msg = $_.Exception.Message
    Write-Host ("HTTP " + $status + " - " + $msg) -ForegroundColor Red
}

pause
