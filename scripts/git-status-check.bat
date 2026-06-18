@echo off
cd /d "C:\inetpub\baskent-enerji-doviz"
echo === GIT STATUS ===
git status
echo.
echo === GIT LOG (son 5) ===
git log --oneline -5
echo.
pause
