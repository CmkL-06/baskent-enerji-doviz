# fix-and-redeploy-mtt-api.ps1
# 1. Env var kontrol + resetle
# 2. Rebuild + publish
# 3. LocalSystem identity
# 4. IIS restart
# YÖNETİCİ olarak çalıştırılmalıdır

Import-Module WebAdministration

$SourceDir  = "C:\inetpub\baskent-enerji-doviz\mtt-api"
$PublishDir = "C:\inetpub\moneytransfer-api"
$PoolName   = "MoneyTransfer-API-Pool"
$SiteName   = "MoneyTransfer-API"

Write-Host "================================================" -ForegroundColor Cyan
Write-Host "  MTT API - Tam Düzeltme + Yeniden Deploy" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""

# ── ADIM 1: Env var kontrol ───────────────────────────────────────────────────
Write-Host "[1/5] Ortam değişkenleri kontrol ediliyor..." -ForegroundColor Yellow

$dbConn  = [System.Environment]::GetEnvironmentVariable("MTT_DB_CONNECTION", "Machine")
$jwtSec  = [System.Environment]::GetEnvironmentVariable("MTT_JWT_SECRET",    "Machine")

Write-Host "  MTT_DB_CONNECTION: $(if ($dbConn) { "'$dbConn'" } else { 'TANIMSIZ/BOŞ' })"
Write-Host "  MTT_JWT_SECRET   : $(if ($jwtSec) { "'$($jwtSec.Substring(0,[Math]::Min(8,$jwtSec.Length)))...' ($($jwtSec.Length) karakter)" } else { 'TANIMSIZ/BOŞ' })"

# Boşsa veya kısaysa yeniden ayarla
if ([string]::IsNullOrWhiteSpace($dbConn)) {
    $dbConn = "Server=.\SQLEXPRESS;Database=mtturkey_mtt;Integrated Security=True;TrustServerCertificate=True;"
    [System.Environment]::SetEnvironmentVariable("MTT_DB_CONNECTION", $dbConn, "Machine")
    Write-Host "  MTT_DB_CONNECTION SIFIRLANDI." -ForegroundColor Green
} else {
    Write-Host "  MTT_DB_CONNECTION mevcut." -ForegroundColor Green
}

if ([string]::IsNullOrWhiteSpace($jwtSec) -or $jwtSec.Length -lt 32) {
    $jwtSec = -join ((65..90) + (97..122) + (48..57) | Get-Random -Count 64 | ForEach-Object {[char]$_})
    [System.Environment]::SetEnvironmentVariable("MTT_JWT_SECRET", $jwtSec, "Machine")
    Write-Host "  MTT_JWT_SECRET SIFIRLANDI (64 karakter)." -ForegroundColor Green
} else {
    Write-Host "  MTT_JWT_SECRET mevcut." -ForegroundColor Green
}

# ── ADIM 2: Build + Publish ───────────────────────────────────────────────────
Write-Host ""
Write-Host "[2/5] Proje derleniyor ve publish ediliyor..." -ForegroundColor Yellow

# App Pool durdur (dosyalar kilitli olmasın)
Stop-WebAppPool -Name $PoolName -ErrorAction SilentlyContinue
Start-Sleep -Seconds 2

# Temiz publish
dotnet publish "$SourceDir\MoneyTransfer.API.csproj" `
    -c Release `
    -o $PublishDir `
    --no-self-contained `
    /p:EnvironmentName=Production

if ($LASTEXITCODE -ne 0) {
    Write-Host "  BUILD HATASI! Devam edilemiyor." -ForegroundColor Red
    pause
    exit 1
}
Write-Host "  Build ve publish tamamlandi." -ForegroundColor Green

# ── ADIM 3: LocalSystem identity ─────────────────────────────────────────────
Write-Host ""
Write-Host "[3/5] App Pool identity -> LocalSystem yapiliyor..." -ForegroundColor Yellow
Set-ItemProperty "IIS:\AppPools\$PoolName" -Name processModel -Value @{
    userName     = ""
    password     = ""
    identityType = 0   # LocalSystem
}
Write-Host "  Tamamlandi." -ForegroundColor Green

# ── ADIM 4: IIS yeniden başlat ────────────────────────────────────────────────
Write-Host ""
Write-Host "[4/5] IIS yeniden baslatiliyor..." -ForegroundColor Yellow
Stop-WebAppPool  -Name $PoolName -ErrorAction SilentlyContinue
Start-Sleep -Seconds 2
Start-WebAppPool -Name $PoolName
Start-Sleep -Seconds 3
Restart-WebItem  "IIS:\Sites\$SiteName"
Start-Sleep -Seconds 5
Write-Host "  Tamamlandi." -ForegroundColor Green

# ── ADIM 5: Test ──────────────────────────────────────────────────────────────
Write-Host ""
Write-Host "[5/5] API testi yapiliyor (http://localhost:5200/api/auth/login)..." -ForegroundColor Yellow
try {
    $body = '{"username":"x","password":"x","panelType":"operator"}'
    $resp = Invoke-WebRequest -Uri "http://localhost:5200/api/auth/login" `
        -Method POST -ContentType "application/json" -Body $body -ErrorAction Stop
    Write-Host "  HTTP $($resp.StatusCode) - Beklenmedik! Detay: $($resp.Content)" -ForegroundColor Red
} catch {
    $code = $_.Exception.Response.StatusCode.value__
    if ($code -eq 401) {
        Write-Host "  HTTP 401 - API CALISYOR! Veritabani baglantisi basarili." -ForegroundColor Green
    } elseif ($code -eq 400) {
        Write-Host "  HTTP 400 - Calistiyor (validation error bekleniyor olabilir)." -ForegroundColor Yellow
        try {
            $stream = $_.Exception.Response.GetResponseStream()
            $reader = [System.IO.StreamReader]::new($stream)
            Write-Host "  Detay: $($reader.ReadToEnd())" -ForegroundColor Yellow
            $reader.Close()
        } catch {}
    } elseif ($code -eq 500) {
        try {
            $stream = $_.Exception.Response.GetResponseStream()
            $reader = [System.IO.StreamReader]::new($stream)
            $detail = $reader.ReadToEnd()
            $reader.Close()
            Write-Host "  HTTP 500 - Hata devam ediyor!" -ForegroundColor Red
            Write-Host "  Detay: $($detail.Substring(0, [Math]::Min(800,$detail.Length)))" -ForegroundColor Red
        } catch {
            Write-Host "  HTTP 500 - Bos yanit (startup crash)." -ForegroundColor Red
        }
    } else {
        Write-Host "  HTTP $code / $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "App Pool durumu:" -ForegroundColor Cyan
Get-WebAppPoolState -Name $PoolName
Get-ItemProperty "IIS:\AppPools\$PoolName" -Name processModel |
    Select-Object identityType, userName | Format-Table -AutoSize

# startup-error.txt varsa goster
$errFile = "$PublishDir\startup-error.txt"
if (Test-Path $errFile) {
    Write-Host ""
    Write-Host "=== startup-error.txt ===" -ForegroundColor Red
    Get-Content $errFile
}

pause
