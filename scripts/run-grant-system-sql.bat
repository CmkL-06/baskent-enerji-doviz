@echo off
echo NT AUTHORITY\SYSTEM icin SQL Server sysadmin yetkisi veriliyor...
echo.
powershell -ExecutionPolicy Bypass -NoExit -File "C:\inetpub\baskent-enerji-doviz\scripts\grant-system-sql-access.ps1"
pause
