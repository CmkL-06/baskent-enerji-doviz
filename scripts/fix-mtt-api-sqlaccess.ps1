# fix-mtt-api-sqlaccess.ps1
# MoneyTransfer-API-Pool identity'sini LocalSystem'a cevirir
# ve mtturkey_mtt veritabanini olusturur (yoksa)
Import-Module WebAdministration

$poolName = "MoneyTransfer-API-Pool"
$siteName = "MoneyTransfer-API"

# 1. App Pool identity -> LocalSystem
Write-Host "[1/3] App Pool identity LocalSystem yapiliyor..." -ForegroundColor Yellow
Set-ItemProperty "IIS:\AppPools\$poolName" -Name processModel -Value @{
    userName  = ""
    password  = ""
    identityType = 0   # 0 = LocalSystem
}
Write-Host "      Tamamlandi." -ForegroundColor Green

# 2. App Pool'u yeniden baslat
Write-Host "[2/3] App Pool yeniden baslatiliyor..." -ForegroundColor Yellow
Stop-WebAppPool  -Name $poolName -ErrorAction SilentlyContinue
Start-Sleep -Seconds 2
Start-WebAppPool -Name $poolName
Start-Sleep -Seconds 2
Restart-WebItem  "IIS:\Sites\$siteName"
Start-Sleep -Seconds 3
Write-Host "      Tamamlandi." -ForegroundColor Green

# 3. Test: Login -> 401 beklenir
Write-Host "[3/3] API testi yapiliyor..." -ForegroundColor Yellow
try {
    $body = '{"username":"x","password":"x","panelType":"operator"}'
    Invoke-WebRequest -Uri "http://localhost:5200/api/auth/login" `
        -Method POST -ContentType "application/json" -Body $body -ErrorAction Stop | Out-Null
    Write-Host "      200 OK - Beklenmedik!" -ForegroundColor Red
} catch {
    $code = $_.Exception.Response.StatusCode.value__
    if ($code -eq 401) {
        Write-Host "      HTTP 401 - API calistiyor, DB baglandi!" -ForegroundColor Green
    } elseif ($code -eq 500) {
        # Hata detayini oku
        $reader = [System.IO.StreamReader]::new($_.Exception.Response.GetResponseStream())
        $body500 = $reader.ReadToEnd()
        $reader.Close()
        Write-Host "      HTTP 500 - Hata devam ediyor!" -ForegroundColor Red
        Write-Host "      Detay: $($body500.Substring(0, [Math]::Min(500, $body500.Length)))" -ForegroundColor Red
    } else {
        Write-Host "      HTTP $code - $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "App Pool identity:" -ForegroundColor Cyan
Get-ItemProperty "IIS:\AppPools\$poolName" -Name processModel |
    Select-Object identityType, userName | Format-Table -AutoSize

pause
