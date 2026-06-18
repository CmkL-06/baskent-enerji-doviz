@echo off
echo Git pull ve API deploy basliyor...
powershell -ExecutionPolicy Bypass -NoExit -Command ^
  "cd 'C:\inetpub\baskent-enerji-doviz'; git pull origin BASKENT-DOVIZ; & 'C:\inetpub\baskent-enerji-doviz\scripts\build-and-deploy-api.ps1'"
pause
