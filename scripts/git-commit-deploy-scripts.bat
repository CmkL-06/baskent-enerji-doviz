@echo off
cd /d C:\inetpub\baskent-enerji-doviz
git add scripts\build-and-deploy-api.ps1
git add scripts\run-build-deploy.bat
git add scripts\run-git-pull-and-deploy.bat
git add scripts\check-row-counts.ps1
git add scripts\run-check-rows.bat
git add scripts\fix-schema-perms.ps1
git add scripts\fix-schema-default.ps1
git add scripts\run-fix-schema.bat
git add scripts\run-fix-schema-perms.bat
git add BaskentEnerji.Data\Contexts\BaskentEnerjiDbContext.cs
git status
git commit -m "fix: HasDefaultSchema kaldirildi, API deploy ve db check scriptleri eklendi"
git push origin BASKENT-DOVIZ
echo.
echo [BITTI] Git commit ve push tamamlandi.
pause
