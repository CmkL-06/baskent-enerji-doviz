@echo off
cd /d C:\inetpub\baskent-enerji-doviz

echo _publish_output/ >> .gitignore
echo. >> .gitignore

git add scripts\check-db-env.ps1
git add scripts\diagnose-api-500.bat
git add scripts\diagnose-iis.ps1
git add scripts\fix-sql-login-mapping.ps1
git add scripts\git-commit-restore-scripts.bat
git add scripts\git-status-check.bat
git add scripts\restart-api-apppool.bat
git add scripts\run-check-db-env.bat
git add scripts\run-diagnose-iis.bat
git add scripts\run-fix-sql-login.bat
git add .gitignore

git status
git commit -m "feat: yeni tanis/diagnose scriptler ve gitignore guncellendi"
git push origin BASKENT-DOVIZ
echo.
echo [BITTI] Git commit ve push tamamlandi.
pause
