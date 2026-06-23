# SMTP env var durum kontrolu (deger gizli, sadece set/empty kontrolu)

Write-Host "=== SMTP / Brevo Env Var Durumu ===" -ForegroundColor Cyan

$vars = @(
    "Smtp__EnableMail",
    "Smtp__ApiKey",
    "Smtp__Password",
    "Smtp__From",
    "Smtp__FromName"
)

foreach ($v in $vars) {
    $val = [System.Environment]::GetEnvironmentVariable($v, "Machine")
    if ($null -eq $val -or $val -eq "") {
        Write-Host "  [$v]  -> EKSIK / BOŞ" -ForegroundColor Red
    } else {
        $masked = if ($val.Length -gt 6) { $val.Substring(0,4) + "****" } else { "****" }
        Write-Host "  [$v]  -> TAMAM ($masked)" -ForegroundColor Green
    }
}

Write-Host ""
Write-Host "=== ForgotPassword testi ===" -ForegroundColor Cyan

$enableMail = [System.Environment]::GetEnvironmentVariable("Smtp__EnableMail", "Machine")
if ($enableMail -ne "true") {
    Write-Host "  Smtp__EnableMail = '$enableMail' — email gonderimi KAPALI" -ForegroundColor Yellow
    Write-Host "  Etkinlestirmek icin: [System.Environment]::SetEnvironmentVariable('Smtp__EnableMail','true','Machine')" -ForegroundColor Yellow
} else {
    Write-Host "  Email gonderimi AKTIF — ForgotPassword tetiklenecek..." -ForegroundColor Green

    $body = '{"email":"kulemlakyatirim.as@gmail.com"}'
    try {
        $resp = Invoke-RestMethod -Uri "https://api.baskentenerji.com/api/v1/User/ForgotPassword" `
            -Method POST -ContentType "application/json" -Body $body
        Write-Host "  API Yaniti: $($resp | ConvertTo-Json -Depth 2)" -ForegroundColor Green
    } catch {
        Write-Host "  API Hatasi: $($_.Exception.Message)" -ForegroundColor Red
    }
}

pause
