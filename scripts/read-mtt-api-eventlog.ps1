# read-mtt-api-eventlog.ps1
# MoneyTransfer-API icin Windows Event Log ve IIS loglarini okur

Write-Host "=== Windows Application Event Log (son 10 hata) ===" -ForegroundColor Cyan
Get-EventLog -LogName Application -EntryType Error,Warning -Newest 10 |
    Where-Object { $_.Source -match "IIS|ASP|\.NET|MoneyTransfer|ANCM|W3SVC" } |
    Format-List TimeGenerated, Source, EventID, Message

Write-Host ""
Write-Host "=== ASP.NET Core ANCM hatalari ===" -ForegroundColor Cyan
Get-EventLog -LogName Application -Newest 30 |
    Where-Object { $_.Message -match "MoneyTransfer|aspnetcore|ANCM|stdout" } |
    Format-List TimeGenerated, Source, Message

Write-Host ""
Write-Host "=== IIS stdout log dosyasi (varsa) ===" -ForegroundColor Cyan
$stdoutLog = "C:\inetpub\moneytransfer-api\logs"
if (Test-Path $stdoutLog) {
    Get-ChildItem $stdoutLog -Filter "*.log" | Sort-Object LastWriteTime -Descending |
        Select-Object -First 1 | Get-Content -Tail 30
} else {
    Write-Host "Log klasoru yok: $stdoutLog" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "=== web.config icerik kontrolu ===" -ForegroundColor Cyan
$wc = "C:\inetpub\moneytransfer-api\web.config"
if (Test-Path $wc) { Get-Content $wc } else { Write-Host "web.config bulunamadi!" -ForegroundColor Red }

pause
