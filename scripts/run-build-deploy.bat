@echo off
echo API build ve deploy basliyor...
powershell -ExecutionPolicy Bypass -NoExit -File "C:\inetpub\baskent-enerji-doviz\scripts\build-and-deploy-api.ps1"
pause
