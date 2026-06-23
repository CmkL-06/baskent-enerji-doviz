@echo off
echo MoneyTransfer.API Test
echo YONETICI olarak calistirilmalidir.
echo.
powershell -ExecutionPolicy Bypass -NoExit -File "C:\inetpub\baskent-enerji-doviz\scripts\test-mtt-api.ps1"
