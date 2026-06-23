@echo off
echo MTT Ortam Degiskenleri Kurulumu
echo YONETICI olarak calistirilmalidir.
echo.
powershell -ExecutionPolicy Bypass -NoExit -File "C:\inetpub\baskent-enerji-doviz\scripts\set-mtt-env-vars.ps1"
