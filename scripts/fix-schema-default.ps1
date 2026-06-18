# Default schema dbo olarak ayarla
$sqlcmd = "C:\Program Files\Microsoft SQL Server\Client SDK\ODBC\170\Tools\Binn\sqlcmd.exe"
$instance = ".\SQLEXPRESS"

$connStr = [System.Environment]::GetEnvironmentVariable('ConnectionStrings__SQL', 'Machine')
$userId = ($connStr -split ';' | Where-Object { $_ -match '^User Id=' }) -replace '^User Id=',''
$database = ($connStr -split ';' | Where-Object { $_ -match '^Database=' }) -replace '^Database=',''

Write-Host "DB: $database, User: $userId"

$sql = @"
USE [$database];
ALTER USER [$userId] WITH DEFAULT_SCHEMA = dbo;
PRINT 'Default schema dbo olarak ayarlandi.';
SELECT name, default_schema_name FROM sys.database_principals WHERE name = '$userId';
"@

$r = & $sqlcmd -S $instance -E -Q $sql 2>&1
Write-Host $r

# App pool restart
Write-Host "`nApp Pool yeniden baslatiliyor..." -ForegroundColor Yellow
& "$env:windir\system32\inetsrv\appcmd.exe" stop apppool /apppool.name:"BaskentEnerji-API"
Start-Sleep -Seconds 2
& "$env:windir\system32\inetsrv\appcmd.exe" start apppool /apppool.name:"BaskentEnerji-API"
Write-Host "App Pool yeniden basladi." -ForegroundColor Green
