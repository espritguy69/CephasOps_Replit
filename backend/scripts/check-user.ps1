# Script to check and fix admin user in database
# This script verifies the admin user exists and has the correct password hash
# Do not commit connection strings. Pass -ConnectionString or set $env:DefaultConnection.

param(
    [string]$ConnectionString = $env:DefaultConnection
)

if ([string]::IsNullOrWhiteSpace($ConnectionString)) {
    Write-Host "ERROR: Connection string required. Set env DefaultConnection or pass -ConnectionString." -ForegroundColor Red
    exit 1
}
Write-Host "Checking admin user in database..." -ForegroundColor Cyan

# Load the .NET assembly to use DatabaseSeeder
$projectPath = Join-Path $PSScriptRoot "..\src\CephasOps.Infrastructure\CephasOps.Infrastructure.csproj"
$dllPath = Join-Path $PSScriptRoot "..\src\CephasOps.Infrastructure\bin\Debug\net10.0\CephasOps.Infrastructure.dll"

if (-not (Test-Path $dllPath)) {
    Write-Host "Building project first..." -ForegroundColor Yellow
    Push-Location (Split-Path $projectPath)
    dotnet build --no-incremental
    Pop-Location
}

# Use psql to check the user directly
Write-Host "`nChecking user in database..." -ForegroundColor Cyan
Write-Host "Connection: $($ConnectionString -replace 'Password=[^;]+', 'Password=***')" -ForegroundColor Gray

# Extract connection details
$host = if ($ConnectionString -match "Host=([^;]+)") { $matches[1] } else { "localhost" }
$port = if ($ConnectionString -match "Port=([^;]+)") { $matches[1] } else { "5432" }
$database = if ($ConnectionString -match "Database=([^;]+)") { $matches[1] } else { "cephasops" }
$username = if ($ConnectionString -match "Username=([^;]+)") { $matches[1] } else { "postgres" }
$password = if ($ConnectionString -match "Password=([^;]+)") { $matches[1] } else { "postgres" }

$env:PGPASSWORD = $password

$sql = @"
-- Check if user exists
SELECT 
    "Id",
    "Email",
    "Name",
    "IsActive",
    CASE 
        WHEN "PasswordHash" IS NULL THEN 'NULL'
        WHEN "PasswordHash" = '' THEN 'EMPTY'
        ELSE 'HAS_HASH'
    END as "PasswordStatus",
    LENGTH("PasswordHash") as "HashLength",
    "CreatedAt"
FROM "Users"
WHERE "Email" = 'simon@cephas.com.my';

-- Check user roles
SELECT 
    u."Email",
    r."Name" as "RoleName",
    r."Scope" as "RoleScope"
FROM "UserRoles" ur
JOIN "Users" u ON ur."UserId" = u."Id"
JOIN "Roles" r ON ur."RoleId" = r."Id"
WHERE u."Email" = 'simon@cephas.com.my';
"@

Write-Host "`nExecuting SQL query..." -ForegroundColor Cyan
$result = psql -h $host -p $port -U $username -d $database -c $sql 2>&1

if ($LASTEXITCODE -eq 0) {
    Write-Host $result
} else {
    Write-Host "Error executing query: $result" -ForegroundColor Red
    Write-Host "`nTrying alternative method..." -ForegroundColor Yellow
    
    # Try with connection string format
    $result2 = psql "$ConnectionString" -c $sql 2>&1
    Write-Host $result2
}

Write-Host "`nTo fix the user, run the backend application - it will automatically update the password hash." -ForegroundColor Green
Write-Host "Or use the fix-user-password.ps1 script." -ForegroundColor Green

