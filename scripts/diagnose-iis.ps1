Write-Host "=== IIS Durum Teshisi ===" -ForegroundColor Cyan
$appcmd = "$env:windir\system32\inetsrv\appcmd.exe"

Write-Host "`n--- IIS Siteleri ---" -ForegroundColor Yellow
& $appcmd list site

Write-Host "`n--- App Pools ---" -ForegroundColor Yellow
& $appcmd list apppool

Write-Host "`n--- Virtual Dirs ---" -ForegroundColor Yellow
& $appcmd list vdir

Write-Host "`n--- wwwroot klasoru ---" -ForegroundColor Yellow
Get-ChildItem "C:\inetpub\wwwroot" -ErrorAction SilentlyContinue | Format-Table Name, LastWriteTime, Attributes

Write-Host "`n--- API klasoru ---" -ForegroundColor Yellow
if (Test-Path "C:\inetpub\wwwroot\api") {
    Get-ChildItem "C:\inetpub\wwwroot\api" | Format-Table Name, LastWriteTime
} else {
    Write-Host "C:\inetpub\wwwroot\api YOK" -ForegroundColor Red
}

Write-Host "`n--- web.config (api) ---" -ForegroundColor Yellow
if (Test-Path "C:\inetpub\wwwroot\api\web.config") {
    Get-Content "C:\inetpub\wwwroot\api\web.config"
}

Write-Host "`n--- Env Variables (SMTP/Email) ---" -ForegroundColor Yellow
[System.Environment]::GetEnvironmentVariables('Machine').GetEnumerator() |
    Where-Object { $_.Key -match 'smtp|email|brevo|mail' -or $_.Value -match 'smtp|brevo' } |
    Format-Table Key, Value

Write-Host "`n--- Windows App Event Errors (son 5) ---" -ForegroundColor Yellow
try {
    Get-EventLog -LogName Application -EntryType Error -Newest 5 -Source "*IIS*","*ASP*","*.NET*","*W3SVC*" -ErrorAction Stop |
        Select-Object TimeGenerated, Source, @{N='Msg';E={$_.Message.Substring(0,[Math]::Min(200,$_.Message.Length))}} |
        Format-List
} catch {
    Write-Host "Event log sorgulanamadi: $_" -ForegroundColor Red
}

Write-Host "`n--- IIS Log Dosyalari ---" -ForegroundColor Yellow
$logBase = "C:\inetpub\logs\LogFiles"
if (Test-Path $logBase) {
    Get-ChildItem $logBase -Recurse -Filter "*.log" | Sort-Object LastWriteTime -Descending | Select-Object -First 3 | Format-Table FullName, LastWriteTime, Length
    $lastLog = Get-ChildItem $logBase -Recurse -Filter "*.log" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if ($lastLog) {
        Write-Host "`nSon 20 satir: $($lastLog.FullName)" -ForegroundColor Gray
        Get-Content $lastLog.FullName -Tail 20
    }
} else {
    Write-Host "$logBase YOK" -ForegroundColor Red
}

Write-Host "`n=== Teshis Tamamlandi ===" -ForegroundColor Cyan
