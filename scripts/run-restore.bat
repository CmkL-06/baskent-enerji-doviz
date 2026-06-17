@echo off
echo MSSQL Backup Restore baslatiliyor...
echo.
powershell -ExecutionPolicy Bypass -NoExit -File "C:\inetpub\baskent-enerji-doviz\scripts\restore-mssql-backup.ps1"
pause
