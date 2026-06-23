@echo off
echo IIS Tam Yeniden Baslat + API Test
echo YONETICI olarak calistirilmalidir.
echo.
powershell -ExecutionPolicy Bypass -NoExit -File "C:\inetpub\baskent-enerji-doviz\scripts\iisreset-and-test.ps1"
