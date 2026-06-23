@echo off
echo MoneyTransfer.API — Build ve Deploy
echo Bu script YONETICI olarak calistirilmalidir.
echo.
echo ADIM 1: Once ortam degiskenlerini ayarlayin (ilk kurulumda):
echo   scripts\run-set-mtt-env.bat
echo.
echo ADIM 2: Deploy:
powershell -ExecutionPolicy Bypass -NoExit -File "C:\inetpub\baskent-enerji-doviz\scripts\deploy-mtt-api.ps1"
