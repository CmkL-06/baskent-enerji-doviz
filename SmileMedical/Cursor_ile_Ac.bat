@echo off
REM Project: QuantaQuokka_
REM Opens the project in Cursor IDE
cursor "%~dp0"
if %errorlevel% neq 0 (
    echo "cursor" command not found. Trying local app data path...
    "%LOCALAPPDATA%\Programs\cursor\Cursor.exe" "%~dp0"
)
