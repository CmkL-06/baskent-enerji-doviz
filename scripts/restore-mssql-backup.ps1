# ============================================================
# MSSQL Backup Restore Script - BaskentEnerji / MTT
# Kaynak: GoDaddy backups-repo/fresh_backups_20260320.zip
# Hedef: Windows Server SQLEXPRESS (.\SQLEXPRESS)
# Tarih: 2026-06-17
# ============================================================

param(
    [string]$BackupDir = "C:\MSSQL_Backups",
    [string]$SqlInstance = ".\SQLEXPRESS"
)

$ErrorActionPreference = "Stop"

# --- 0. SSL dogrulama bypass (GoDaddy HTTP->HTTPS redirect icin) ---
[System.Net.ServicePointManager]::ServerCertificateValidationCallback = [System.Net.Security.RemoteCertificateValidationCallback]{ $true }
[System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]::Tls12

# --- 1. Backup klasoru olustur ---
if (-not (Test-Path $BackupDir)) {
    New-Item -ItemType Directory -Path $BackupDir | Out-Null
    Write-Host "[OK] Backup klasoru olusturuldu: $BackupDir"
}

# --- 2. .bak dosyalarini GoDaddy'den indir ---
$BaseUrl = "https://alanyayatirim.com/tmp_dl_6f63275a5b9486cfada3fb7b0689aba0"
$Files = @(
    @{ Name="mtt-moneyexchangeturkey.bak"; DB="mtt-moneyexchangeturkey" },
    @{ Name="mtturkey_exchange.bak"; DB="mtturkey_exchange" }
)

foreach ($f in $Files) {
    $dest = Join-Path $BackupDir $f.Name
    $minSizeBytes = 10 * 1024 * 1024  # 10MB minimum
    if ((Test-Path $dest) -and ((Get-Item $dest).Length -gt $minSizeBytes)) {
        $sizeMB = [math]::Round((Get-Item $dest).Length / 1MB, 1)
        Write-Host "[SKIP] Zaten var ($sizeMB MB): $dest"
    } else {
        if (Test-Path $dest) { Remove-Item $dest -Force; Write-Host "[DEL] Eksik dosya silindi: $dest" }
        Write-Host "[DL] Indiriliyor: $($f.Name) ..."
        $url = "$BaseUrl/$($f.Name)"
        $wc = New-Object System.Net.WebClient
        $wc.DownloadFile($url, $dest)
        $sizeMB = [math]::Round((Get-Item $dest).Length / 1MB, 1)
        Write-Host "[OK] Indirildi: $dest ($sizeMB MB)"
    }
}

# --- 3. AppPool'u durdur ---
Write-Host ""
Write-Host "[INFO] IIS AppPool durduruluyor..."
try {
    Import-Module WebAdministration -ErrorAction SilentlyContinue
    Stop-WebAppPool -Name "BaskentEnerji-API" -ErrorAction SilentlyContinue
    Write-Host "[OK] AppPool durduruldu."
} catch {
    Write-Host "[WARN] AppPool durdurulamadi: $_"
}
Start-Sleep -Seconds 2

# --- 4. Her veritabanini restore et ---
foreach ($f in $Files) {
    $bakPath = Join-Path $BackupDir $f.Name
    $dbName  = $f.DB

    Write-Host ""
    Write-Host "========================================"
    Write-Host "[RESTORE] $dbName"
    Write-Host "  Kaynak: $bakPath"
    Write-Host "  Sunucu: $SqlInstance"
    Write-Host "========================================"

    try {
        # Mevcut baglantilar kes
        $killSql = "IF EXISTS (SELECT name FROM sys.databases WHERE name = N'" + $dbName + "') BEGIN ALTER DATABASE [" + $dbName + "] SET SINGLE_USER WITH ROLLBACK IMMEDIATE END"
        Invoke-Sqlcmd -ServerInstance $SqlInstance -Query $killSql -ErrorAction SilentlyContinue

        # Restore komutu
        $restoreSql = "RESTORE DATABASE [" + $dbName + "] FROM DISK = N'" + $bakPath + "' WITH REPLACE, RECOVERY, STATS = 10"
        Invoke-Sqlcmd -ServerInstance $SqlInstance -Query $restoreSql -QueryTimeout 300
        Write-Host "[OK] Restore tamamlandi: $dbName"

        # Cok kullanicili moda geri al
        $multiSql = "ALTER DATABASE [" + $dbName + "] SET MULTI_USER"
        Invoke-Sqlcmd -ServerInstance $SqlInstance -Query $multiSql
        Write-Host "[OK] Multi-user moda alindi: $dbName"

    } catch {
        Write-Host "[HATA] Restore basarisiz: $_"
        Write-Host "  Manuel restore: SSMS -> Databases -> Restore Database -> Device -> $bakPath"
    }
}

# --- 5. Satir sayilarini dogrula ---
Write-Host ""
Write-Host "========================================"
Write-Host "[DOGRULAMA] Satir sayilari kontrol ediliyor..."
Write-Host "========================================"

$verifySql = "SELECT t.TABLE_SCHEMA + '.' + t.TABLE_NAME AS Tablo, p.rows AS Satir FROM INFORMATION_SCHEMA.TABLES t JOIN sys.tables st ON st.name = t.TABLE_NAME JOIN sys.partitions p ON p.object_id = st.object_id AND p.index_id IN (0,1) WHERE t.TABLE_TYPE = 'BASE TABLE' AND t.TABLE_SCHEMA = 'mtturkey_exchange' AND p.rows > 0 ORDER BY p.rows DESC"

try {
    $rows = Invoke-Sqlcmd -ServerInstance $SqlInstance -Database "mtt-moneyexchangeturkey" -Query $verifySql -ErrorAction SilentlyContinue
    if ($rows) {
        $rows | Format-Table -AutoSize
        $total = ($rows | Measure-Object -Property Satir -Sum).Sum
        Write-Host "[OK] Toplam satir: $total"
    } else {
        Write-Host "[WARN] Veri bulunamadi - restore basarisiz olabilir."
    }
} catch {
    Write-Host "[WARN] Dogrulama sorgusu basarisiz: $_"
}

# --- 6. AppPool'u yeniden basiat ---
Write-Host ""
Write-Host "[INFO] IIS AppPool yeniden baslatiliyor..."
try {
    Start-WebAppPool -Name "BaskentEnerji-API" -ErrorAction SilentlyContinue
    Write-Host "[OK] AppPool baslatildi."
} catch {
    Write-Host "[WARN] AppPool baslatilmadi: $_"
}

# --- 7. Hatirlatma ---
Write-Host ""
Write-Host "========================================"
Write-Host "[HATIRLATMA] GoDaddy gecici dosyalari sil:"
Write-Host "  rm -rf ~/public_html/tmp_dl_6f63275a5b9486cfada3fb7b0689aba0"
Write-Host "  rm -rf /tmp/mssql_restore"
Write-Host "========================================"
Write-Host ""
Write-Host "[TAMAMLANDI] Restore islemi bitti."
