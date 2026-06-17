@echo off
cd /d "C:\inetpub\baskent-enerji-doviz"
git add scripts/sql-backup-otomatik.ps1 scripts/setup-backup-task.ps1 scripts/run-setup-backup.bat scripts/grant-system-sql-access.ps1 scripts/run-grant-system-sql.bat scripts/git-commit-backup-scripts.bat
git commit -m "feat: MSSQL otomatik backup otomasyonu (Task Scheduler, SYSTEM, sqlcmd)"
git push
echo.
echo [BITTI] Git commit ve push tamamlandi.
pause
