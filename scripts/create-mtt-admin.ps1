# create-mtt-admin.ps1 - MTT ilk admin kullanicisi
# Yonetici GEREKMEZ - normal PS yeterli

$connStr = [System.Environment]::GetEnvironmentVariable("MTT_DB_CONNECTION", "Machine")
if (-not $connStr) {
    Write-Host "MTT_DB_CONNECTION tanimli degil!" -ForegroundColor Red; pause; exit
}

$username  = Read-Host "Admin kullanici adi (varsayilan: admin)"
if (-not $username) { $username = "admin" }

$password  = Read-Host "Admin sifresi (en az 8 karakter)"
if ($password.Length -lt 8) { Write-Host "Sifre cok kisa!" -ForegroundColor Red; pause; exit }

$fullName  = Read-Host "Ad Soyad (varsayilan: Sistem Yoneticisi)"
if (-not $fullName) { $fullName = "Sistem Yoneticisi" }

# -- BCrypt hash: gecici dotnet projesi olustur --
Write-Host ""
Write-Host "BCrypt hash hesaplaniyor..." -ForegroundColor Yellow
$tmpDir = "$env:TEMP\mtt_hash_tool"
if (Test-Path $tmpDir) { Remove-Item $tmpDir -Recurse -Force }
New-Item $tmpDir -ItemType Directory | Out-Null

@'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="BCrypt.Net-Next" Version="4.0.3" />
  </ItemGroup>
</Project>
'@ | Set-Content "$tmpDir\HashTool.csproj" -Encoding UTF8

@"
Console.WriteLine(BCrypt.Net.BCrypt.HashPassword(args[0], 12));
"@ | Set-Content "$tmpDir\Program.cs" -Encoding UTF8

$rawOut = dotnet run --project $tmpDir -- $password 2>$null
$hash = ($rawOut | Where-Object { $_ -like '$2*' } | Select-Object -Last 1)
if (-not $hash -or -not $hash.StartsWith('$2')) {
    Write-Host "Hash olusturulamadi! dotnet 8 kurulu mu? Cikti: $rawOut" -ForegroundColor Red
    pause; exit
}
Write-Host "Hash olusturuldu." -ForegroundColor Green

# -- SQL insert --
Write-Host "Veritabanina yaziliyor..." -ForegroundColor Yellow
Add-Type -AssemblyName System.Data

$conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
try {
    $conn.Open()
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = @"
IF NOT EXISTS (SELECT 1 FROM Users WHERE Username = @u)
BEGIN
    INSERT INTO Users (Username, PasswordHash, Role, Name, IsActive, CreatedAt)
    VALUES (@u, @h, 'admin', @n, 1, GETDATE())
    SELECT 'OLUSTURULDU' AS Sonuc
END
ELSE
    SELECT 'ZATEN_VAR' AS Sonuc
"@
    $cmd.Parameters.AddWithValue("@u", $username) | Out-Null
    $cmd.Parameters.AddWithValue("@h", $hash)     | Out-Null
    $cmd.Parameters.AddWithValue("@n", $fullName) | Out-Null
    $result = $cmd.ExecuteScalar()
    if ($result -eq "OLUSTURULDU") {
        Write-Host "Admin kullanici olusturuldu: $username" -ForegroundColor Green
    } else {
        Write-Host "Kullanici zaten mevcut: $username" -ForegroundColor Yellow
    }
} catch {
    Write-Host "SQL HATASI: $($_.Exception.Message)" -ForegroundColor Red
    pause; exit
} finally {
    $conn.Close()
}

# -- Login testi --
Write-Host ""
Write-Host "=== Login testi ===" -ForegroundColor Cyan
try {
    $body = @{ username = $username; password = $password; panelType = "admin" } | ConvertTo-Json
    $resp = Invoke-RestMethod -Uri "http://localhost:5000/api/auth/login" `
        -Method POST -ContentType "application/json" -Body $body
    Write-Host "LOGIN BASARILI!" -ForegroundColor Green
    Write-Host "  Role  : $($resp.role)"  -ForegroundColor Gray
    Write-Host "  Token : $($resp.token.Substring(0,40))..." -ForegroundColor Gray
} catch {
    $code = $_.Exception.Response.StatusCode.value__
    Write-Host "Login $code - $($_.ErrorDetails.Message)" -ForegroundColor Red
}

# Temizlik
Remove-Item $tmpDir -Recurse -Force -ErrorAction SilentlyContinue
pause
