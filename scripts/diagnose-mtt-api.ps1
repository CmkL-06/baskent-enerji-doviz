# diagnose-mtt-api.ps1 — Kapsamli tani

Write-Host "=== 1. startup-error.txt ===" -ForegroundColor Cyan
$errFile = "C:\inetpub\moneytransfer-api\startup-error.txt"
if (Test-Path $errFile) {
    Get-Content $errFile
} else {
    Write-Host "Dosya yok (uygulama henuz hic baslamadi ya da yazamadi)." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "=== 2. Event Log (son 5 IIS/ANCM hatasi) ===" -ForegroundColor Cyan
Get-EventLog -LogName Application -Newest 50 -ErrorAction SilentlyContinue |
    Where-Object { $_.EntryType -in "Error","Warning" } |
    Select-Object -First 5 |
    Format-List TimeGenerated, Source, EventID, Message

Write-Host ""
Write-Host "=== 3. App dogrudan calistir (stdout goster) ===" -ForegroundColor Cyan
Write-Host "MoneyTransfer.API.exe calistiriliyor..." -ForegroundColor Yellow
$exe = "C:\inetpub\moneytransfer-api\MoneyTransfer.API.exe"
if (-not (Test-Path $exe)) {
    Write-Host "EXE bulunamadi: $exe" -ForegroundColor Red
    $dll = "C:\inetpub\moneytransfer-api\MoneyTransfer.API.dll"
    if (Test-Path $dll) {
        Write-Host "DLL bulundu, dotnet ile calistiriliyor..." -ForegroundColor Yellow
        $proc = Start-Process -FilePath "dotnet" -ArgumentList $dll `
            -RedirectStandardOutput "C:\inetpub\moneytransfer-api\stdout.txt" `
            -RedirectStandardError  "C:\inetpub\moneytransfer-api\stderr.txt" `
            -WorkingDirectory "C:\inetpub\moneytransfer-api" `
            -PassThru -NoNewWindow
        Start-Sleep -Seconds 5
        $proc | Stop-Process -Force -ErrorAction SilentlyContinue
        Write-Host "--- STDOUT ---" -ForegroundColor Gray
        if (Test-Path "C:\inetpub\moneytransfer-api\stdout.txt") { Get-Content "C:\inetpub\moneytransfer-api\stdout.txt" }
        Write-Host "--- STDERR ---" -ForegroundColor Red
        if (Test-Path "C:\inetpub\moneytransfer-api\stderr.txt") { Get-Content "C:\inetpub\moneytransfer-api\stderr.txt" }
    }
} else {
    $proc = Start-Process -FilePath $exe `
        -RedirectStandardOutput "C:\inetpub\moneytransfer-api\stdout.txt" `
        -RedirectStandardError  "C:\inetpub\moneytransfer-api\stderr.txt" `
        -WorkingDirectory "C:\inetpub\moneytransfer-api" `
        -PassThru -NoNewWindow
    Start-Sleep -Seconds 6
    $proc | Stop-Process -Force -ErrorAction SilentlyContinue
    Write-Host "--- STDOUT ---" -ForegroundColor Gray
    if (Test-Path "C:\inetpub\moneytransfer-api\stdout.txt") { Get-Content "C:\inetpub\moneytransfer-api\stdout.txt" }
    Write-Host "--- STDERR ---" -ForegroundColor Red
    if (Test-Path "C:\inetpub\moneytransfer-api\stderr.txt") { Get-Content "C:\inetpub\moneytransfer-api\stderr.txt" }
}

Write-Host ""
Write-Host "=== 4. web.config ===" -ForegroundColor Cyan
Get-Content "C:\inetpub\moneytransfer-api\web.config" -ErrorAction SilentlyContinue

Write-Host ""
Write-Host "=== 5. Publish klasoru dosyalari ===" -ForegroundColor Cyan
Get-ChildItem "C:\inetpub\moneytransfer-api\" | Format-Table Name, Length, LastWriteTime -AutoSize

pause
