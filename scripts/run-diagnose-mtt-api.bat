@echo off
echo MoneyTransfer-API Kapsamli Tani
echo YONETICI olarak calistirilmalidir.
echo.
powershell -ExecutionPolicy Bypass -NoExit -File "C:\inetpub\baskent-enerji-doviz\scripts\diagnose-mtt-api.ps1"
