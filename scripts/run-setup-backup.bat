@echo off
echo MSSQL Backup Task Scheduler Kurulumu baslatiliyor...
echo.
powershell -ExecutionPolicy Bypass -NoExit -File "C:\inetpub\baskent-enerji-doviz\scripts\setup-backup-task.ps1"
pause
