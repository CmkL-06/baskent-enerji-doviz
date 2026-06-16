$date = Get-Date -Format "yyyyMMdd_HHmm"
$backupDir = "C:\backups\sql"

foreach ($db in @("mtturkey_exchange", "mtturkey_telegram")) {
    $path = "$backupDir\${db}_$date.bak"
    $sql = "BACKUP DATABASE [$db] TO DISK='$path' WITH FORMAT,COMPRESSION,STATS=10"
    sqlcmd -S ".\SQLEXPRESS" -Q $sql
    if ($LASTEXITCODE -eq 0) {
        Write-Host "OK: $db -> $path"
    } else {
        Write-Host "HATA: $db backup basarisiz"
    }
}

Get-ChildItem $backupDir -Filter "*.bak" |
    Where-Object { $_.LastWriteTime -lt (Get-Date).AddDays(-7) } |
    Remove-Item -Force

Write-Host "Temizlik tamamlandi."
