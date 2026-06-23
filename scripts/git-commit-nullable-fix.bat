@echo off
cd /d C:\inetpub\baskent-enerji-doviz

echo === Git Status ===
git status

echo.
echo === Staging rm_user_login.cs ===
git add BaskentEnerji.Entity/Modals/RequestModals/User/rm_user_login.cs

echo.
echo === Commit ===
git commit -m "fix: login icin Mail ve Password nullable yapildi (email zorunlu degil)"

echo.
echo === Push ===
git push origin BASKENT-DOVIZ

echo.
echo === Done ===
pause
