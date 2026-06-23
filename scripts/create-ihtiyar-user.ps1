$sqlcmd = "C:\Program Files\Microsoft SQL Server\Client SDK\ODBC\170\Tools\Binn\sqlcmd.exe"

# SHA-256 hash of "owner1"
$sha = [System.Security.Cryptography.SHA256]::Create()
$bytes = [System.Text.Encoding]::UTF8.GetBytes("owner1")
$hashBytes = $sha.ComputeHash($bytes)
$hash = [BitConverter]::ToString($hashBytes).Replace("-","").ToLower()

Write-Host "SHA256('owner1') = $hash" -ForegroundColor Cyan

$newId = [System.Guid]::NewGuid().ToString().ToUpper()
Write-Host "Yeni ID: $newId" -ForegroundColor Cyan

$sql = @"
USE [mtturkey_exchange];
IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Username = 'ihtiyar')
BEGIN
    INSERT INTO dbo.Users (Id, Mail, Username, Password, Rank, IsEmailVerified, Firstname, Lastname, Gender, CreatedDate)
    VALUES ('$newId', 'ihtiyar@baskentenerji.com', 'ihtiyar', '$hash', 100, 1, 'ihtiyar', 'owner', 0, GETDATE());
    PRINT 'Kullanici olusturuldu: ihtiyar (Owner)';
END
ELSE
BEGIN
    PRINT 'ihtiyar zaten mevcut.';
END
"@

& $sqlcmd -S ".\SQLEXPRESS" -E -Q $sql 2>&1

Write-Host ""
Write-Host "=== Dogrulama ===" -ForegroundColor Cyan
& $sqlcmd -S ".\SQLEXPRESS" -E -Q "USE [mtturkey_exchange]; SELECT Username, Mail, Rank FROM dbo.Users WHERE Username='ihtiyar';" 2>&1

pause
