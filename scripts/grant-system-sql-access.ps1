# NT AUTHORITY\SYSTEM hesabina SQL Server sysadmin yetkisi ver
# Task Scheduler backup gorevinin calisabilmesi icin gerekli

$SqlInstance = ".\SQLEXPRESS"

$sql = "IF NOT EXISTS (SELECT * FROM sys.server_principals WHERE name = N'NT AUTHORITY\SYSTEM') " +
       "CREATE LOGIN [NT AUTHORITY\SYSTEM] FROM WINDOWS; " +
       "IF IS_SRVROLEMEMBER('sysadmin', 'NT AUTHORITY\SYSTEM') = 0 " +
       "ALTER SERVER ROLE [sysadmin] ADD MEMBER [NT AUTHORITY\SYSTEM];"

Write-Host "[INFO] NT AUTHORITY\SYSTEM icin SQL Server sysadmin yetkisi veriliyor..."
Invoke-Sqlcmd -ServerInstance $SqlInstance -Database "master" -Query $sql -ErrorAction Stop
Write-Host "[OK] Yetki verildi."

# Dogrula
$check = Invoke-Sqlcmd -ServerInstance $SqlInstance -Database "master" -Query "SELECT IS_SRVROLEMEMBER('sysadmin', 'NT AUTHORITY\SYSTEM') AS is_sysadmin"
Write-Host "[DOGRULAMA] sysadmin: $($check.is_sysadmin)"
Write-Host ""
Write-Host "[BITTI] Task Scheduler backup gorevi artik calisabilir."
