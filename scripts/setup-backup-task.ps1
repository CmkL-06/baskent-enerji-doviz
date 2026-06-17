# ============================================================
# Task Scheduler - MSSQL Gunluk Backup Gorevi Kurulumu
# Her gece 02:00'da calisir
# ============================================================

$TaskName    = "BaskentEnerji-MSSQL-Backup"
$ScriptPath  = "C:\inetpub\baskent-enerji-doviz\scripts\sql-backup-otomatik.ps1"
$LogPath     = "C:\MSSQL_Backups\auto\taskscheduler.log"

# Mevcut gorevi sil (varsa)
if (Get-ScheduledTask -TaskName $TaskName -ErrorAction SilentlyContinue) {
    Unregister-ScheduledTask -TaskName $TaskName -Confirm:$false
    Write-Host "[INFO] Eski gorev silindi."
}

# Backup klasoru olustur
if (-not (Test-Path "C:\MSSQL_Backups\auto")) {
    New-Item -ItemType Directory -Path "C:\MSSQL_Backups\auto" | Out-Null
}

# Gorev tanimlari
$action  = New-ScheduledTaskAction `
    -Execute "powershell.exe" `
    -Argument "-ExecutionPolicy Bypass -NonInteractive -File `"$ScriptPath`""

$trigger = New-ScheduledTaskTrigger -Daily -At "02:00"

$settings = New-ScheduledTaskSettingsSet `
    -ExecutionTimeLimit (New-TimeSpan -Hours 1) `
    -StartWhenAvailable `
    -RunOnlyIfNetworkAvailable:$false

$principal = New-ScheduledTaskPrincipal `
    -UserId "SYSTEM" `
    -LogonType ServiceAccount `
    -RunLevel Highest

# Gorevi kaydet
Register-ScheduledTask `
    -TaskName $TaskName `
    -Action $action `
    -Trigger $trigger `
    -Settings $settings `
    -Principal $principal `
    -Description "BaskentEnerji MSSQL gunluk backup - mtt-moneyexchangeturkey + mtturkey_exchange" `
    -Force

Write-Host ""
Write-Host "[OK] Task Scheduler gorevi olusturuldu: $TaskName"
Write-Host "     Program: powershell.exe"
Write-Host "     Script:  $ScriptPath"
Write-Host "     Zamanlama: Her gece 02:00"
Write-Host "     Kullanici: SYSTEM"
Write-Host ""

# Gorevi dogrula
$task = Get-ScheduledTask -TaskName $TaskName
Write-Host "[DOGRULAMA] Gorev durumu: $($task.State)"
Write-Host ""

# Hemen bir test backupi al
Write-Host "[TEST] Ilk backup aliniyor..."
Start-ScheduledTask -TaskName $TaskName
Start-Sleep -Seconds 5

$taskInfo = Get-ScheduledTaskInfo -TaskName $TaskName
Write-Host "[INFO] Son calisma: $($taskInfo.LastRunTime)"
Write-Host "[INFO] Son sonuc kodu: $($taskInfo.LastTaskResult)"
Write-Host ""
Write-Host "[BITTI] Setup tamamlandi."
