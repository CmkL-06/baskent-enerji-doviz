# iisreset-and-test.ps1
# IIS servisini tamamen yeniden baslatir (env var'lari kalici yükler)

Write-Host "=== IIS tam yeniden baslatiyor (iisreset) ===" -ForegroundColor Cyan
iisreset /restart
Start-Sleep -Seconds 8

Write-Host ""
Write-Host "=== Env var kontrol (IIS process'i görmeli) ===" -ForegroundColor Cyan
Write-Host "MTT_DB_CONNECTION : $([System.Environment]::GetEnvironmentVariable('MTT_DB_CONNECTION','Machine'))"
Write-Host "MTT_JWT_SECRET    : $([System.Environment]::GetEnvironmentVariable('MTT_JWT_SECRET','Machine').Substring(0,8))... ($([System.Environment]::GetEnvironmentVariable('MTT_JWT_SECRET','Machine').Length) karakter)"

Write-Host ""
Write-Host "=== App Pool baslatiliyor ===" -ForegroundColor Cyan
Start-WebAppPool -Name "MoneyTransfer-API-Pool" -ErrorAction SilentlyContinue
Start-Sleep -Seconds 5

Write-Host ""
Write-Host "=== API testi ===" -ForegroundColor Cyan
try {
    $body = '{"username":"x","password":"x","panelType":"operator"}'
    Invoke-WebRequest -Uri "http://localhost:5200/api/auth/login" `
        -Method POST -ContentType "application/json" -Body $body -ErrorAction Stop | Out-Null
    Write-Host "HTTP 200 - Beklenmedik!" -ForegroundColor Red
} catch {
    $code = $_.Exception.Response.StatusCode.value__
    if ($code -eq 401) {
        Write-Host "HTTP 401 — API CALISTIYOR! Her sey tamam." -ForegroundColor Green
    } elseif ($code -eq 500) {
        try {
            $s = $_.Exception.Response.GetResponseStream()
            $r = [System.IO.StreamReader]::new($s)
            $d = $r.ReadToEnd(); $r.Close()
            Write-Host "HTTP 500 — $($d.Substring(0,[Math]::Min(500,$d.Length)))" -ForegroundColor Red
        } catch { Write-Host "HTTP 500 — bos yanit (startup crash)" -ForegroundColor Red }
    } else {
        Write-Host "HTTP $code — $($_.Exception.Message)" -ForegroundColor Yellow
    }
}
pause
