Write-Host "=== Machine-level DB Connection Env Vars ===" -ForegroundColor Cyan
[System.Environment]::GetEnvironmentVariables('Machine').GetEnumerator() |
    Where-Object { $_.Key -match 'ConnectionString|connection|database|Database|DbContext|sqlserver|SqlServer|Default' } |
    Sort-Object Key |
    Format-Table Key, Value -AutoSize

Write-Host "`n=== Deployed API appsettings.json ===" -ForegroundColor Cyan
$apiPath = "C:\inetpub\wwwroot\api\appsettings.json"
if (Test-Path $apiPath) {
    Get-Content $apiPath
} else {
    Write-Host "NOT FOUND: $apiPath" -ForegroundColor Red
}

Write-Host "`n=== appsettings.Production.json ===" -ForegroundColor Cyan
$prodPath = "C:\inetpub\wwwroot\api\appsettings.Production.json"
if (Test-Path $prodPath) {
    Get-Content $prodPath
} else {
    Write-Host "NOT FOUND: $prodPath" -ForegroundColor Yellow
}

Write-Host "`n=== App Pool Identity ===" -ForegroundColor Cyan
$appcmd = "$env:windir\system32\inetsrv\appcmd.exe"
& $appcmd list apppool "BaskentEnerji-API" /processModel.userName /text:*

Write-Host "`n=== SQL Server Servis Durumu ===" -ForegroundColor Cyan
Get-Service -Name "MSSQL*" | Format-Table Name, Status, StartType

Write-Host "`n=== AutoRateUpdate Son Hata ===" -ForegroundColor Cyan
try {
    Get-EventLog -LogName Application -EntryType Error -Newest 3 -Source "*.NET Runtime" -ErrorAction Stop |
        Where-Object { $_.Message -match 'AutoRate|SQLEXPRESS|connection|Connection' } |
        Select-Object TimeGenerated, @{N='Msg';E={$_.Message}} |
        Format-List
} catch {
    Write-Host "Event log sorgulanamadi: $_"
}
