@echo off
cd /d C:\inetpub\baskent-enerji-doviz
git add BaskentEnerji.Business\BaskentEnerji.Business.csproj
git commit -m "fix: AutoMapper 14.0.0 -> 15.1.3 (CVE-2026-32933 DoS fix)"
git push origin BASKENT-DOVIZ
echo.
echo [BITTI] AutoMapper guncellendi ve push edildi.
pause
