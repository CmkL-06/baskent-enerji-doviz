Import-Module WebAdministration
$site = "MoneyTransfer-API"
# Localhost test binding (host header yok)
$existing = Get-WebBinding -Name $site | Where-Object { $_.bindingInformation -eq "*:5200:" }
if (-not $existing) {
    New-WebBinding -Name $site -Protocol "http" -Port 5200 -IPAddress "*" -HostHeader ""
    Write-Host "Localhost binding eklendi: http://localhost:5200" -ForegroundColor Green
} else {
    Write-Host "Zaten mevcut." -ForegroundColor Yellow
}
Get-WebBinding -Name $site | Format-Table -AutoSize
pause
