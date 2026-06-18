# SQL Login ve User Mapping Fix
# mtturkey_exchange DB icin orphaned user sorunu cozer

$sqlcmd = "C:\Program Files\Microsoft SQL Server\Client SDK\ODBC\170\Tools\Binn\sqlcmd.exe"
$instance = ".\SQLEXPRESS"

# Connection string'den sifreyi oku
$connStr = [System.Environment]::GetEnvironmentVariable('ConnectionStrings__SQL', 'Machine')
Write-Host "Connection String: $connStr" -ForegroundColor Cyan

# Parse connection string
$password = ""
$userId = ""
$database = ""
foreach ($part in $connStr -split ';') {
    if ($part -match '^Password=(.+)$') { $password = $matches[1] }
    if ($part -match '^User Id=(.+)$') { $userId = $matches[1] }
    if ($part -match '^Database=(.+)$') { $database = $matches[1] }
}

Write-Host "Database: $database" -ForegroundColor Yellow
Write-Host "User Id: $userId" -ForegroundColor Yellow
Write-Host "Password: [GIZLI]" -ForegroundColor Yellow

if (-not $password -or -not $userId -or -not $database) {
    Write-Host "[HATA] Connection string parse edilemedi!" -ForegroundColor Red
    exit 1
}

# 1. SQL login mevcut mu kontrol et
Write-Host "`n--- Login kontrolu ---" -ForegroundColor Cyan
$checkLogin = "SELECT name, is_disabled FROM sys.server_principals WHERE name = N'$userId'"
$result = & $sqlcmd -S $instance -E -Q $checkLogin 2>&1
Write-Host "Login mevcut: $result"

# 2. DB'de user mevcut mu kontrol et
Write-Host "`n--- DB user kontrolu ---" -ForegroundColor Cyan
$checkUser = "USE [$database]; SELECT name, sid FROM sys.database_principals WHERE name = N'$userId'"
$result2 = & $sqlcmd -S $instance -E -Q $checkUser 2>&1
Write-Host "DB user: $result2"

# 3. Fix: Login yok/disabled ise yeniden olustur
Write-Host "`n--- Login olusturuluyor/guncelleniyor ---" -ForegroundColor Cyan
$escapedPw = $password -replace "'", "''"
$fixLogin = @"
-- Login yoksa olustur, varsa sifreyi guncelle
IF NOT EXISTS (SELECT * FROM sys.server_principals WHERE name = N'$userId')
BEGIN
    CREATE LOGIN [$userId] WITH PASSWORD = '$escapedPw', CHECK_POLICY = OFF, CHECK_EXPIRATION = OFF;
    PRINT 'Login olusturuldu: $userId';
END
ELSE
BEGIN
    ALTER LOGIN [$userId] WITH PASSWORD = '$escapedPw', CHECK_POLICY = OFF, CHECK_EXPIRATION = OFF;
    ALTER LOGIN [$userId] ENABLE;
    PRINT 'Login guncellendi: $userId';
END
"@
$r = & $sqlcmd -S $instance -E -Q $fixLogin 2>&1
Write-Host $r

# 4. DB user mapping fix (orphaned user)
Write-Host "`n--- DB user mapping fix ---" -ForegroundColor Cyan
$fixUser = @"
USE [$database];
IF NOT EXISTS (SELECT * FROM sys.database_principals WHERE name = N'$userId')
BEGIN
    CREATE USER [$userId] FOR LOGIN [$userId];
    ALTER ROLE [db_owner] ADD MEMBER [$userId];
    PRINT 'DB user olusturuldu ve db_owner yapildi: $userId';
END
ELSE
BEGIN
    -- Orphaned user fix
    ALTER USER [$userId] WITH LOGIN = [$userId];
    ALTER ROLE [db_owner] ADD MEMBER [$userId];
    PRINT 'Orphaned user fix uygulandi: $userId';
END
"@
$r2 = & $sqlcmd -S $instance -E -Q $fixUser 2>&1
Write-Host $r2

# 5. Test: Baglanti calisiyor mu?
Write-Host "`n--- Baglanti testi ---" -ForegroundColor Cyan
$testQuery = "SELECT DB_NAME() AS CurrentDB, SYSTEM_USER AS LoginName"
$testConn = & $sqlcmd -S $instance -d $database -U $userId -P $password -Q $testQuery 2>&1
if ($LASTEXITCODE -eq 0) {
    Write-Host "[OK] Baglanti basarili!" -ForegroundColor Green
    Write-Host $testConn
} else {
    Write-Host "[HATA] Baglanti hala basarisiz:" -ForegroundColor Red
    Write-Host $testConn
}

Write-Host "`n=== Fix Tamamlandi ===" -ForegroundColor Cyan
Write-Host "IIS App Pool'u yeniden baslatin: Restart-WebAppPool 'BaskentEnerji-API'" -ForegroundColor Yellow
