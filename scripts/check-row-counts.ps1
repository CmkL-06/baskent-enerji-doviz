$sqlcmd = "C:\Program Files\Microsoft SQL Server\Client SDK\ODBC\170\Tools\Binn\sqlcmd.exe"
$instance = ".\SQLEXPRESS"
$database = "mtturkey_exchange"

Write-Host "=== mtturkey_exchange - Tablo Satir Sayilari ===" -ForegroundColor Cyan

$q1 = "USE [mtturkey_exchange]; SELECT t.name AS Tablo, SUM(p.rows) AS SatirSayisi FROM sys.tables t JOIN sys.partitions p ON t.object_id = p.object_id WHERE p.index_id IN (0,1) GROUP BY t.name ORDER BY SUM(p.rows) DESC;"
& $sqlcmd -S $instance -E -Q $q1 2>&1

Write-Host ""
Write-Host "=== Toplam DB Boyutu ===" -ForegroundColor Cyan
$q2 = "USE [mtturkey_exchange]; SELECT name AS dosya, ROUND(size * 8.0 / 1024, 1) AS MB FROM sys.database_files;"
& $sqlcmd -S $instance -E -Q $q2 2>&1
