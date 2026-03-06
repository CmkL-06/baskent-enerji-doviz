$projectPath = "D:\QuantaQuokka\SmileMedical"
$apiPath = "$projectPath\SmileMedical.API"
$dataPath = "$projectPath\SmileMedical.Data"

Write-Host "Configuring D: Drive Environment for SQLite..."

# 1. Add SQLite Package
Write-Host "Adding SQLite support..."
dotnet add "$dataPath\SmileMedical.Data.csproj" package Microsoft.EntityFrameworkCore.Sqlite

# 2. Update appsettings.json
$appsettings = "$apiPath\appsettings.json"
$json = Get-Content $appsettings -Raw | ConvertFrom-Json
$json.ConnectionStrings.SQL = "Data Source=SmileMedical.db"
$json | ConvertTo-Json -Depth 10 | Set-Content $appsettings
Write-Host "Updated appsettings.json."

# 3. Update Program.cs
$programCs = "$apiPath\Program.cs"
$content = Get-Content $programCs -Raw

# Regex to find the AddDbContext block and replace it with SQLite version
# We replace the specific SQL Server configuration block
$newBlock = @"
builder.Services.AddDbContext<SmileMedicalDbContext>(options =>
{
    options.UseSqlite(builder.Configuration.GetConnectionString("SQL"));
    options.EnableServiceProviderCaching();
});
"@

# Pattern matching the existing UseSqlServer block roughly
# We match from AddDbContext... up to the closing }); 
# This is tricky with Regex, so we'll use a simpler replacement if possible, or just exact match of the start line if standard.
# Given the file content I saw, I'll attempt a specific replacement.

if ($content -match "options.UseSqlServer") {
    # Replace the whole Services.AddDbContext block is safer if we can match it. 
    # But for simplicity and robustness in script, let's identify the range.
    # Actually, simpler: Comment out UseSqlServer and add UseSqlite? No, clean replacement is best.
    
    # We will use a placeholder approach if regex checks out, otherwise alert user.
    # Let's try to replace the inner part.
    $content = $content -replace 'options\.UseSqlServer\(builder\.Configuration\.GetConnectionString\("SQL"\),[\s\S]*?\}\);', 'options.UseSqlite(builder.Configuration.GetConnectionString("SQL"));'
    Set-Content $programCs $content
    Write-Host "Updated Program.cs to UseSqlite."
} else {
    Write-Warning "Could not find UseSqlServer in Program.cs to replace. Please check manually."
}

# 4. Clean Migrations and Update DB
Write-Host "Resetting Migrations for SQLite..."
Remove-Item "$dataPath\Migrations" -Recurse -Force -ErrorAction SilentlyContinue
# dotnet ef migrations add InitialCreateSQLite --project "$dataPath" --startup-project "$apiPath" --output-dir Migrations
# dotnet ef database update --project "$dataPath" --startup-project "$apiPath"

Write-Host "Configuration Complete. Please run migrations manually if needed, or I will run them next."
