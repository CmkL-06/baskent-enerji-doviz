@echo off
cd /d "C:\inetpub\baskent-enerji-doviz"
echo Untracked restore scriptleri ve docs commit ediliyor...
git add docs/VERI_KAYBI_ANALIZ_RAPORU.md
git add scripts/fix-restore-main-db.ps1 scripts/restore-main-db.ps1 scripts/restore-mssql-backup.ps1
git add scripts/run-fix-restore.bat scripts/run-restore-main.bat scripts/run-restore.bat
git status
git commit -m "chore: restore scriptleri ve veri kaybi analiz raporu eklendi"
git push
echo.
echo [BITTI] Git commit ve push tamamlandi.
pause
