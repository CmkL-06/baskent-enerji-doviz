@echo off
echo === API 500 Hata Teshisi ===
echo.

echo --- IIS Site ve App Pool Durumu ---
%windir%\system32\inetsrv\appcmd.exe list site
echo.
%windir%\system32\inetsrv\appcmd.exe list apppool
echo.

echo --- baskentenerji.com Site Detayi ---
%windir%\system32\inetsrv\appcmd.exe list vdir
echo.

echo --- web.config kontrolu ---
if exist "C:\inetpub\wwwroot\api\web.config" (
    echo Found: C:\inetpub\wwwroot\api\web.config
    type "C:\inetpub\wwwroot\api\web.config"
) else (
    echo NOT found: C:\inetpub\wwwroot\api\web.config
)
echo.

echo --- API Klasoru ---
if exist "C:\inetpub\wwwroot\api" (
    dir "C:\inetpub\wwwroot\api"
) else (
    echo C:\inetpub\wwwroot\api yok
)
echo.

echo --- wwwroot klasoru ---
dir "C:\inetpub\wwwroot"
echo.

echo --- IIS Log Dosyalari ---
dir "C:\inetpub\logs\LogFiles\" /s /b 2>nul | head
echo.

echo --- Son IIS Log (varsa) ---
for /f "delims=" %%f in ('dir "C:\inetpub\logs\LogFiles\W3SVC*\*.log" /s /b /o-d 2^>nul') do (
    echo Son log: %%f
    tail -20 "%%f" 2>nul
    goto :logdone
)
:logdone

echo.
echo --- Windows Event Log - Application Errors (son 10) ---
powershell -Command "Get-EventLog -LogName Application -EntryType Error -Newest 10 | Select-Object TimeGenerated,Source,Message | Format-List"

echo.
echo --- Env Variables (SMTP) ---
powershell -Command "[System.Environment]::GetEnvironmentVariables('Machine') | Where-Object { $_.Key -like '*smtp*' -or $_.Key -like '*Smtp*' -or $_.Key -like '*email*' -or $_.Key -like '*Email*' -or $_.Key -like '*brevo*' -or $_.Key -like '*Brevo*' } | Format-Table"

echo.
echo [BITTI] Teshis tamamlandi.
pause
