# ============================================================
# MSSQL Otomatik Yedekleme Scripti
# Veritabanlari: mtt-moneyexchangeturkey, mtturkey_exchange
# Hedef: C:\MSSQL_Backups\auto\
# Saklama: Son 7 gun
# sqlcmd.exe kullanir (SYSTEM hesabiyla calisir)
# ============================================================

param(
    [string]$SqlInstance  = ".\SQLEXPRESS",
    [string]$BackupRoot   = "C:\MSSQL_Backups\auto",
    [int]$RetainDays      = 7
)

$Databases = @("mtturkey_exchange", "mtturkey_mtt", "mtturkey_telegram")
$DateStamp = Get-Date -Format "yyyyMMdd_HHmm"
$LogFile   = "$BackupRoot\backup-log.txt"

# sqlcmd.exe yolunu bul
$sqlcmdPaths = @(
    "C:\Program Files\Microsoft SQL Server\Client SDK\ODBC\170\Tools\Binn\sqlcmd.exe",
    "C:\Program Files\Microsoft SQL Server\Client SDK\ODBC\130\Tools\Binn\sqlcmd.exe",
    "C:\Program Files\Microsoft SQL Server\110\Tools\Binn\sqlcmd.exe",
    "sqlcmd.exe"
)
$sqlcmd = $null
foreach ($p in $sqlcmdPaths) {
    if ($p -eq "sqlcmd.exe") {
        $found = Get-Command "sqlcmd.exe" -ErrorAction SilentlyContinue
        if ($found) { $sqlcmd = "sqlcmd.exe"; break }
    } elseif (Test-Path $p) {
        $sqlcmd = $p; break
    }
}
if (-not $sqlcmd) {
    # sqlcmd bulunamazsa Invoke-Sqlcmd dene
    $sqlcmd = $null
}

# --- Klasor olustur ---
if (-not (Test-Path $BackupRoot)) {
    New-Item -ItemType Directory -Path $BackupRoot | Out-Null
}

function Write-Log {
    param($msg)
    $line = "[$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')] $msg"
    Write-Host $line
    Add-Content -Path $LogFile -Value $line -ErrorAction SilentlyContinue
}

function Run-Sql {
    param([string]$query)
    if ($sqlcmd) {
        $result = & $sqlcmd -S $SqlInstance -E -Q $query 2>&1
        if ($LASTEXITCODE -ne 0) {
            throw "sqlcmd hata kodu: $LASTEXITCODE`n$result"
        }
        return $result
    } else {
        # Invoke-Sqlcmd fallback
        Import-Module SqlServer -ErrorAction SilentlyContinue
        return Invoke-Sqlcmd -ServerInstance $SqlInstance -Database "master" -Query $query -QueryTimeout 300 -ErrorAction Stop
    }
}

Write-Log "====== Backup basliyor: $DateStamp ======"
Write-Log "sqlcmd: $sqlcmd"

# --- Backup klasorune SQL Server servis hesabina yazma izni ver ---
try {
    icacls $BackupRoot /grant "NT Service\MSSQL`$SQLEXPRESS:(OI)(CI)F" /T | Out-Null
    icacls $BackupRoot /grant "NT AUTHORITY\SYSTEM:(OI)(CI)F" /T | Out-Null
    Write-Log "[INFO] Klasor izinleri guncellendi: $BackupRoot"
} catch {
    Write-Log "[WARN] Izin guncelleme: $_"
}

# --- Her DB icin backup al ---
foreach ($db in $Databases) {
    $bakFile = "$BackupRoot\${db}_${DateStamp}.bak"
    Write-Log "Yedekleniyor: $db -> $bakFile"

    $backupSql = "BACKUP DATABASE [" + $db + "] TO DISK = N'" + $bakFile + "' WITH STATS = 10, NAME = N'" + $db + " - " + $DateStamp + "'"

    try {
        $result = Run-Sql $backupSql
        if ($result) { Write-Log "  sqlcmd: $result" }
        if (Test-Path $bakFile) {
            $sizeMB = [math]::Round((Get-Item $bakFile).Length / 1MB, 1)
            Write-Log "[OK] Tamamlandi: $db ($sizeMB MB)"
        } else {
            Write-Log "[HATA] $db : .bak dosyasi olusturulmadi. sqlcmd cikti: $result"
        }
    } catch {
        Write-Log "[HATA] $db : $_"
    }
}

# --- Eski yedekleri sil (RetainDays'den eski) ---
Write-Log "Eski yedekler temizleniyor (>${RetainDays} gun)..."
$cutoff = (Get-Date).AddDays(-$RetainDays)
$oldFiles = Get-ChildItem -Path $BackupRoot -Filter "*.bak" | Where-Object { $_.LastWriteTime -lt $cutoff }
foreach ($f in $oldFiles) {
    Remove-Item $f.FullName -Force
    Write-Log "  Silindi: $($f.Name)"
}

# --- Mevcut yedek listesi ---
Write-Log "Mevcut yedekler:"
Get-ChildItem -Path $BackupRoot -Filter "*.bak" | Sort-Object LastWriteTime | ForEach-Object {
    $sizeMB = [math]::Round($_.Length / 1MB, 1)
    Write-Log "  $($_.Name) ($sizeMB MB)"
}

Write-Log "====== Backup tamamlandi ======"
