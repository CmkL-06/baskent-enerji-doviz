# ============================================================
# mtt-moneyexchangeturkey Restore — WITH MOVE (Linux->Windows)
# Kaynak: C:\MSSQL_Backups\mtt-moneyexchangeturkey.bak
# ============================================================

$SqlInstance = ".\SQLEXPRESS"
$BakPath     = "C:\MSSQL_Backups\mtt-moneyexchangeturkey.bak"
$DbName      = "mtt-moneyexchangeturkey"

[System.Net.ServicePointManager]::ServerCertificateValidationCallback = [System.Net.Security.RemoteCertificateValidationCallback]{ $true }

# --- 1. SQL Server veri dizinini bul ---
Write-Host "[INFO] SQL Server veri dizini aliniyor..."
$dataDirSql = "SELECT SERVERPROPERTY('InstanceDefaultDataPath') AS DataDir"
try {
    $result = Invoke-Sqlcmd -ServerInstance $SqlInstance -Query $dataDirSql
    $DataDir = $result.DataDir.TrimEnd('\')
    Write-Host "[OK] DataDir: $DataDir"
} catch {
    $DataDir = "C:\Program Files\Microsoft SQL Server\MSSQL16.SQLEXPRESS\MSSQL\DATA"
    Write-Host "[WARN] DataDir alinamadi, default kullaniliyor: $DataDir"
}

# --- 2. Backup dosyasindaki mantiksal dosya adlarini al ---
Write-Host ""
Write-Host "[INFO] Backup dosyasi inceleniyor (FILELISTONLY)..."
$fileListSql = "RESTORE FILELISTONLY FROM DISK = N'" + $BakPath + "'"
try {
    $fileList = Invoke-Sqlcmd -ServerInstance $SqlInstance -Query $fileListSql
    $fileList | Format-Table LogicalName, PhysicalName, Type -AutoSize
} catch {
    Write-Host "[HATA] FILELISTONLY basarisiz: $_"
    exit 1
}

# --- 3. MOVE ifadelerini olustur ---
$moveClauses = @()
foreach ($file in $fileList) {
    $logicalName = $file.LogicalName
    $fileType    = $file.Type  # 'D'=data, 'L'=log

    if ($fileType -eq 'D') {
        $newPath = $DataDir + "\" + $DbName + ".mdf"
    } elseif ($fileType -eq 'L') {
        $newPath = $DataDir + "\" + $DbName + "_log.ldf"
    } else {
        $newPath = $DataDir + "\" + $logicalName + ".ndf"
    }

    $moveClauses += "MOVE N'" + $logicalName + "' TO N'" + $newPath + "'"
    Write-Host "  MOVE: $logicalName -> $newPath"
}

# --- 4. AppPool durdur ---
Write-Host ""
Write-Host "[INFO] AppPool durduruluyor..."
try {
    Import-Module WebAdministration -ErrorAction SilentlyContinue
    Stop-WebAppPool -Name "BaskentEnerji-API" -ErrorAction SilentlyContinue
    Write-Host "[OK] AppPool durduruldu."
} catch { Write-Host "[WARN] AppPool durdurulamadi." }
Start-Sleep -Seconds 2

# --- 5. Mevcut baglantilar kes ---
$killSql = "IF EXISTS (SELECT name FROM sys.databases WHERE name = N'" + $DbName + "') BEGIN ALTER DATABASE [" + $DbName + "] SET SINGLE_USER WITH ROLLBACK IMMEDIATE END"
Invoke-Sqlcmd -ServerInstance $SqlInstance -Query $killSql -ErrorAction SilentlyContinue

# --- 6. Restore WITH MOVE ---
Write-Host ""
Write-Host "[RESTORE] $DbName (WITH MOVE)..."

$moveStr = [string]::Join(", " + [System.Environment]::NewLine, $moveClauses)
$restoreSql = "RESTORE DATABASE [" + $DbName + "] FROM DISK = N'" + $BakPath + "' WITH REPLACE, RECOVERY, STATS = 10, " + $moveStr

Write-Host ""
Write-Host "SQL:"
Write-Host $restoreSql
Write-Host ""

try {
    Invoke-Sqlcmd -ServerInstance $SqlInstance -Query $restoreSql -QueryTimeout 600
    Write-Host "[OK] Restore BASARILI: $DbName"
    $multiSql = "ALTER DATABASE [" + $DbName + "] SET MULTI_USER"
    Invoke-Sqlcmd -ServerInstance $SqlInstance -Query $multiSql
    Write-Host "[OK] Multi-user moda alindi."
} catch {
    Write-Host "[HATA] Restore basarisiz: $_"
}

# --- 7. Satir sayisi dogrula ---
Write-Host ""
Write-Host "[DOGRULAMA] Satir sayilari..."
$verifySql = "SELECT t.TABLE_NAME AS Tablo, p.rows AS Satir FROM INFORMATION_SCHEMA.TABLES t JOIN sys.tables st ON st.name = t.TABLE_NAME JOIN sys.partitions p ON p.object_id = st.object_id AND p.index_id IN (0,1) WHERE t.TABLE_TYPE = 'BASE TABLE' AND p.rows > 0 ORDER BY p.rows DESC"
try {
    $rows = Invoke-Sqlcmd -ServerInstance $SqlInstance -Database $DbName -Query $verifySql
    if ($rows) {
        $rows | Select-Object -First 15 | Format-Table -AutoSize
        $total = ($rows | Measure-Object -Property Satir -Sum).Sum
        Write-Host "[OK] Toplam satir: $total"
    } else {
        Write-Host "[WARN] Veri bulunamadi."
    }
} catch {
    Write-Host "[WARN] Dogrulama basarisiz: $_"
}

# --- 8. AppPool yeniden basiat ---
Write-Host ""
try {
    Start-WebAppPool -Name "BaskentEnerji-API" -ErrorAction SilentlyContinue
    Write-Host "[OK] AppPool baslatildi."
} catch { }

Write-Host ""
Write-Host "[TAMAMLANDI]"
