@echo off
echo BaskentEnerji-API App Pool yeniden baslatiliyor...
%windir%\system32\inetsrv\appcmd.exe stop apppool /apppool.name:"BaskentEnerji-API"
timeout /t 2 /nobreak > nul
%windir%\system32\inetsrv\appcmd.exe start apppool /apppool.name:"BaskentEnerji-API"
echo.
echo App Pool durumu:
%windir%\system32\inetsrv\appcmd.exe list apppool "BaskentEnerji-API"
echo.
echo [BITTI]
pause
