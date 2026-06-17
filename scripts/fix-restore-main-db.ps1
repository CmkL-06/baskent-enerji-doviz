# ============================================================
# mtt-moneyexchangeturkey FORCE RESTORE
# ============================================================

$SqlInstance = ".\SQLEXPRESS"
$BakPath     = "C:\MSSQL_Backups\mtt-moneyexchangeturkey.bak"
$DbName      = "mtt-moneyexchangeturkey"
$DataDir     = "C:\Program Files\Microsoft SQL Server\MSSQL16.SQLEXPRESS\MSSQL\DATA"

# --- 1. DB durumunu kontrol et ---
Write-Host "[INFO] DB durumu..."
$checkSql = "SELECT name, state_desc, user_access_desc FROM sys.databases WHERE name = N'mtt-moneyexchangeturkey'"
try {
    $dbInfo = Invoke-Sqlcmd -ServerInstance $SqlInstance -Database "master" -Query $checkSql
    if ($dbInfo) {
        Write-Host "  name=$($dbInfo.name)  state=$($dbInfo.state_desc)  access=$($dbInfo.user_access_desc)"
    } else {
        Write-Host "  DB bulunamadi."
    }
} catch {
    Write-Host "  [WARN] $($_)"
}

# --- 2. Aktif session'lari PowerShell'den tek tek oldur ---
Write-Host ""
Write-Host "[INFO] Aktif baglantilari oldur..."
$sessionsSql = "SELECT session_id FROM sys.dm_exec_sessions WHERE database_id = DB_ID(N'mtt-moneyexchangeturkey')"
try {
    $sessions = Invoke-Sqlcmd -ServerInstance $SqlInstance -Database "master" -Query $sessionsSql -ErrorAction SilentlyContinue
    if ($sessions) {
        foreach ($s in $sessions) {
            $spid = $s.session_id
            $killSql = "KILL " + $spid
            Invoke-Sqlcmd -ServerInstance $SqlInstance -Database "master" -Query $killSql -ErrorAction SilentlyContinue
            Write-Host "  KILL $spid"
        }
    } else {
        Write-Host "  Aktif session yok."
    }
} catch {
    Write-Host "  [WARN] $($_)"
}

# --- 3. DB drop (varsa) ---
Write-Host ""
Write-Host "[INFO] DB drop ediliyor (varsa)..."
$dropSql = "IF EXISTS (SELECT 1 FROM sys.databases WHERE name = N'mtt-moneyexchangeturkey') DROP DATABASE [mtt-moneyexchangeturkey]"
try {
    Invoke-Sqlcmd -ServerInstance $SqlInstance -Database "master" -Query $dropSql -QueryTimeout 30 -ErrorAction SilentlyContinue
    Write-Host "  [OK] Drop tamamlandi (veya DB yoktu)."
} catch {
    Write-Host "  [WARN] $($_)"
}
Start-Sleep -Seconds 3

# --- 4. AppPool durdur ---
Write-Host ""
Write-Host "[INFO] AppPool durduruluyor..."
try {
    Import-Module WebAdministration -ErrorAction SilentlyContinue
    Stop-WebAppPool -Name "BaskentEnerji-API" -ErrorAction SilentlyContinue
    Write-Host "  [OK] AppPool durduruldu."
} catch {
    Write-Host "  [WARN] $($_)"
}
Start-Sleep -Seconds 2

# --- 5. Fresh RESTORE WITH MOVE ---
Write-Host ""
Write-Host "[RESTORE] $DbName..."
$mdf = $DataDir + "\mtt-moneyexchangeturkey.mdf"
$ldf = $DataDir + "\mtt-moneyexchangeturkey_log.ldf"

$restoreSql = "RESTORE DATABASE [mtt-moneyexchangeturkey] FROM DISK = N'" + $BakPath + "' WITH RECOVERY, STATS = 10, MOVE N'mtturkey_exchange' TO N'" + $mdf + "', MOVE N'mtturkey_exchange_log' TO N'" + $ldf + "'"

Write-Host "SQL: $restoreSql"
Write-Host ""

try {
    Invoke-Sqlcmd -ServerInstance $SqlInstance -Database "master" -Query $restoreSql -QueryTimeout 600 -ErrorAction Stop
    Write-Host "[OK] RESTORE BASARILI!"
} catch {
    Write-Host "[HATA] $_"
    Write-Host ""
    Write-Host "SSMS ile manuel restore:"
    Write-Host "  RESTORE DATABASE [mtt-moneyexchangeturkey]"
    Write-Host "  FROM DISK = N'$BakPath'"
    Write-Host "  WITH RECOVERY,"
    Write-Host "  MOVE N'mtturkey_exchange' TO N'$mdf',"
    Write-Host "  MOVE N'mtturkey_exchange_log' TO N'$ldf'"
}

# --- 6. Satir dogrulama ---
Write-Host ""
Write-Host "[DOGRULAMA]..."
$verifySql = "SELECT t.TABLE_NAME, p.rows FROM INFORMATION_SCHEMA.TABLES t JOIN sys.tables st ON st.name = t.TABLE_NAME JOIN sys.partitions p ON p.object_id = st.object_id AND p.index_id IN (0,1) WHERE t.TABLE_TYPE = 'BASE TABLE' AND p.rows > 0 ORDER BY p.rows DESC"
try {
    $rows = Invoke-Sqlcmd -ServerInstance $SqlInstance -Database "mtt-moneyexchangeturkey" -Query $verifySql -QueryTimeout 30 -ErrorAction SilentlyContinue
    if ($rows) {
        $rows | Select-Object -First 20 | Format-Table -AutoSize
        $total = ($rows | Measure-Object -Property rows -Sum).Sum
        Write-Host "[OK] Toplam satir: $total"
    } else {
        Write-Host "[WARN] Veri yok."
    }
} catch {
    Write-Host "[WARN] Dogrulama: $_"
}

# --- 7. AppPool basiat ---
Write-Host ""
try {
    Start-WebAppPool -Name "BaskentEnerji-API" -ErrorAction SilentlyContinue
    Write-Host "[OK] AppPool baslatildi."
} catch { }

Write-Host ""
Write-Host "[BITTI]"
