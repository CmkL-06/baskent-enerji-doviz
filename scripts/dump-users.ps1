$sqlcmd = "C:\Program Files\Microsoft SQL Server\Client SDK\ODBC\170\Tools\Binn\sqlcmd.exe"
$out = & $sqlcmd -S ".\SQLEXPRESS" -E -Q "USE [mtturkey_exchange]; SELECT Id, Mail, Username, Rank, LEFT(Password,20) AS PassStart FROM dbo.Users ORDER BY Id;" 2>&1
$out | Out-File "C:\inetpub\baskent-enerji-doviz\scripts\users-dump.txt" -Encoding UTF8
Write-Host "Yazildi: C:\inetpub\baskent-enerji-doviz\scripts\users-dump.txt"
pause
