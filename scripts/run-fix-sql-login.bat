@echo off
echo SQL Login ve User Mapping Fix baslatiliyor...
powershell -ExecutionPolicy Bypass -NoExit -File "C:\inetpub\baskent-enerji-doviz\scripts\fix-sql-login-mapping.ps1"
pause
