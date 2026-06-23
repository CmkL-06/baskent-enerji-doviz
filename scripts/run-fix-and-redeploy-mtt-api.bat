@echo off
echo MTT API - Tam Duzeltme ve Yeniden Deploy
echo YONETICI olarak calistirilmalidir.
echo.
powershell -ExecutionPolicy Bypass -NoExit -File "C:\inetpub\baskent-enerji-doviz\scripts\fix-and-redeploy-mtt-api.ps1"
