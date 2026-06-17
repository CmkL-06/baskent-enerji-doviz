@echo off
echo mtt-moneyexchangeturkey FORCE RESTORE baslatiliyor...
echo.
powershell -ExecutionPolicy Bypass -NoExit -File "C:\inetpub\baskent-enerji-doviz\scripts\fix-restore-main-db.ps1"
pause
