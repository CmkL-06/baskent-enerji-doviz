# set-mtt-env-vars.ps1
# MTT_DB_CONNECTION ve MTT_JWT_SECRET ortam değişkenlerini Machine seviyesinde ayarlar
# YÖNETİCİ olarak çalıştırılmalıdır

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "  MTT Ortam Değişkenleri Kurulumu" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""

# ── Veritabanı bağlantısı ────────────────────────────────────────────────────
$existingDb = [System.Environment]::GetEnvironmentVariable("MTT_DB_CONNECTION", "Machine")
if ($existingDb) {
    Write-Host "MTT_DB_CONNECTION zaten tanımlı." -ForegroundColor Green
    Write-Host "Mevcut: $($existingDb.Substring(0, [Math]::Min(60,$existingDb.Length)))..." -ForegroundColor Gray
    $overwrite = Read-Host "Üzerine yaz? (e/h)"
    if ($overwrite -ne 'e') { Write-Host "Atlandı." -ForegroundColor Yellow }
    else {
        $dbConn = "Server=.\SQLEXPRESS;Database=mtturkey_mtt;Integrated Security=True;TrustServerCertificate=True;"
        [System.Environment]::SetEnvironmentVariable("MTT_DB_CONNECTION", $dbConn, "Machine")
        Write-Host "MTT_DB_CONNECTION güncellendi." -ForegroundColor Green
    }
} else {
    $dbConn = "Server=.\SQLEXPRESS;Database=mtturkey_mtt;Integrated Security=True;TrustServerCertificate=True;"
    [System.Environment]::SetEnvironmentVariable("MTT_DB_CONNECTION", $dbConn, "Machine")
    Write-Host "MTT_DB_CONNECTION ayarlandı." -ForegroundColor Green
    Write-Host "Değer: $dbConn" -ForegroundColor Gray
}

Write-Host ""

# ── JWT Secret ───────────────────────────────────────────────────────────────
$existingJwt = [System.Environment]::GetEnvironmentVariable("MTT_JWT_SECRET", "Machine")
if ($existingJwt) {
    Write-Host "MTT_JWT_SECRET zaten tanımlı." -ForegroundColor Green
    Write-Host "Mevcut: $($existingJwt.Substring(0,8))..." -ForegroundColor Gray
    $overwriteJwt = Read-Host "Üzerine yaz? (e/h)"
    if ($overwriteJwt -ne 'e') { Write-Host "Atlandı." -ForegroundColor Yellow }
    else {
        $jwtKey = -join ((65..90) + (97..122) + (48..57) | Get-Random -Count 64 | ForEach-Object {[char]$_})
        [System.Environment]::SetEnvironmentVariable("MTT_JWT_SECRET", $jwtKey, "Machine")
        Write-Host "MTT_JWT_SECRET güncellendi." -ForegroundColor Green
    }
} else {
    # Rastgele 64 karakter güvenli secret
    $jwtKey = -join ((65..90) + (97..122) + (48..57) | Get-Random -Count 64 | ForEach-Object {[char]$_})
    [System.Environment]::SetEnvironmentVariable("MTT_JWT_SECRET", $jwtKey, "Machine")
    Write-Host "MTT_JWT_SECRET ayarlandı (64 karakter)." -ForegroundColor Green
    Write-Host "Değer: $jwtKey" -ForegroundColor Gray
    Write-Host "NOT: Bu değeri güvenli bir yerde saklayın!" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Ortam değişkenleri hazır. IIS ve deploy scriptini yeniden başlatabilirsiniz." -ForegroundColor Green
Write-Host ""
pause
