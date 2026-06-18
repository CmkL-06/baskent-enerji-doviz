$sqlcmd = "C:\Program Files\Microsoft SQL Server\Client SDK\ODBC\170\Tools\Binn\sqlcmd.exe"
$instance = ".\SQLEXPRESS"

$connStr = [System.Environment]::GetEnvironmentVariable('ConnectionStrings__SQL', 'Machine')
$userId = ($connStr -split ';' | Where-Object { $_ -match '^User Id=' }) -replace '^User Id=',''
$database = ($connStr -split ';' | Where-Object { $_ -match '^Database=' }) -replace '^Database=',''

Write-Host "DB: $database, User: $userId" -ForegroundColor Cyan

# Schema listesi kontrol
Write-Host "`n--- Schema listesi ---" -ForegroundColor Yellow
$schemas = "USE [$database]; SELECT name, schema_id FROM sys.schemas ORDER BY name"
& $sqlcmd -S $instance -E -Q $schemas 2>&1

# Users tablosu var mi ve hangi schema'da?
Write-Host "`n--- Users tablosu nerede? ---" -ForegroundColor Yellow
$tables = "USE [$database]; SELECT TABLE_SCHEMA, TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Users' OR TABLE_NAME LIKE 'User%' ORDER BY TABLE_SCHEMA, TABLE_NAME"
& $sqlcmd -S $instance -E -Q $tables 2>&1

# Schema 'mtturkey_exchange' yoksa olustur
Write-Host "`n--- Schema olusturma + yetki ---" -ForegroundColor Yellow
$fixSchema = @"
USE [$database];

-- Schema yoksa olustur (EF Core icin)
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = N'$database')
BEGIN
    EXEC('CREATE SCHEMA [$database] AUTHORIZATION [$userId]');
    PRINT 'Schema olusturuldu: $database';
END
ELSE
BEGIN
    PRINT 'Schema zaten mevcut: $database';
END

-- Kullaniciya schema uzerinde tam yetki ver
GRANT CONTROL ON SCHEMA::[$database] TO [$userId];
-- db_owner ekle (tum erisimleri kapsar)
IF IS_ROLEMEMBER('db_owner', '$userId') = 0
BEGIN
    ALTER ROLE [db_owner] ADD MEMBER [$userId];
    PRINT 'db_owner rolune eklendi: $userId';
END
ELSE
BEGIN
    PRINT 'Zaten db_owner: $userId';
END
"@
& $sqlcmd -S $instance -E -Q $fixSchema 2>&1

# Schema'da tablo var mi kontrol
Write-Host "`n--- Schema 'mtturkey_exchange' tablolari ---" -ForegroundColor Yellow
$schemaTables = "USE [$database]; SELECT TABLE_SCHEMA, TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = '$database' ORDER BY TABLE_NAME"
& $sqlcmd -S $instance -E -Q $schemaTables 2>&1

# App Pool restart
Write-Host "`n--- App Pool restart ---" -ForegroundColor Yellow
& "$env:windir\system32\inetsrv\appcmd.exe" stop apppool /apppool.name:"BaskentEnerji-API"
Start-Sleep -Seconds 2
& "$env:windir\system32\inetsrv\appcmd.exe" start apppool /apppool.name:"BaskentEnerji-API"
Write-Host "[OK] App Pool yeniden basladi." -ForegroundColor Green
