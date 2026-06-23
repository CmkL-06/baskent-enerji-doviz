$sqlcmd = "C:\Program Files\Microsoft SQL Server\Client SDK\ODBC\170\Tools\Binn\sqlcmd.exe"
& $sqlcmd -S ".\SQLEXPRESS" -E -Q "USE [mtturkey_exchange]; SELECT TOP 10 Id, Mail, Username, Rank FROM dbo.Users ORDER BY Id;" 2>&1
pause
