# PowerShell script to fix duplicate index on BuildingRules.BuildingId
# Usage: .\ApplyBuildingRulesIndexFix.ps1 [-ConnectionString "Host=...;Database=...;Username=...;Password=..."]
#
# If no connection string is provided, it will try to read from appsettings.json

param(
    [Parameter(Mandatory=$false)]
    [string]$ConnectionString = ""
)

$ErrorActionPreference = "Stop"

Write-Host "===========================================" -ForegroundColor Cyan
Write-Host "Fix: BuildingRules Duplicate Index" -ForegroundColor Cyan
Write-Host "===========================================" -ForegroundColor Cyan
Write-Host ""

# If no connection string provided, try to read from appsettings.json
if ([string]::IsNullOrEmpty($ConnectionString)) {
    $appsettingsPath = Join-Path $PSScriptRoot "..\..\..\..\CephasOps.Api\appsettings.json"
    if (Test-Path $appsettingsPath) {
        Write-Host "Reading connection string from appsettings.json..." -ForegroundColor Gray
        $appsettings = Get-Content $appsettingsPath | ConvertFrom-Json
        $ConnectionString = $appsettings.ConnectionStrings.DefaultConnection
        Write-Host "Connection string loaded from appsettings.json" -ForegroundColor Green
    }
    else {
        Write-Host "Error: No connection string provided and appsettings.json not found" -ForegroundColor Red
        Write-Host "Please provide -ConnectionString parameter or ensure appsettings.json exists" -ForegroundColor Yellow
        exit 1
    }
}

# Parse connection string
$dbHost = ""
$database = ""
$username = ""
$password = ""
$port = "5432"

$parts = $ConnectionString -split ";"
foreach ($part in $parts) {
    if ($part -match "Host=(.+)") {
        $dbHost = $matches[1]
    }
    elseif ($part -match "Database=(.+)") {
        $database = $matches[1]
    }
    elseif ($part -match "Username=(.+)") {
        $username = $matches[1]
    }
    elseif ($part -match "Password=(.+)") {
        $password = $matches[1]
    }
    elseif ($part -match "Port=(.+)") {
        $port = $matches[1]
    }
}

if (-not $dbHost -or -not $database -or -not $username -or -not $password) {
    Write-Host "Error: Invalid connection string. Required: Host, Database, Username, Password" -ForegroundColor Red
    Write-Host "Format: Host=hostname;Database=dbname;Username=user;Password=pass;Port=5432" -ForegroundColor Yellow
    exit 1
}

$scriptPath = Join-Path $PSScriptRoot "FixBuildingRulesDuplicateIndex.sql"

if (-not (Test-Path $scriptPath)) {
    Write-Host "Error: Fix script not found at $scriptPath" -ForegroundColor Red
    exit 1
}

Write-Host "Applying BuildingRules duplicate index fix..." -ForegroundColor Yellow
Write-Host "Host: $dbHost" -ForegroundColor Gray
Write-Host "Port: $port" -ForegroundColor Gray
Write-Host "Database: $database" -ForegroundColor Gray
Write-Host "Username: $username" -ForegroundColor Gray
Write-Host ""

# Check if psql is available
$psqlPath = Get-Command psql -ErrorAction SilentlyContinue
if (-not $psqlPath) {
    Write-Host "Error: psql command not found. Please install PostgreSQL client tools." -ForegroundColor Red
    Write-Host "" -ForegroundColor White
    Write-Host "Alternative methods:" -ForegroundColor Yellow
    Write-Host "  1. Use Supabase SQL Editor (recommended for Supabase)" -ForegroundColor White
    Write-Host "  2. Use pgAdmin or DBeaver" -ForegroundColor White
    Write-Host "  3. Install PostgreSQL client: https://www.postgresql.org/download/" -ForegroundColor White
    Write-Host "" -ForegroundColor White
    Write-Host "SQL file location: $scriptPath" -ForegroundColor Cyan
    exit 1
}

# Set PGPASSWORD environment variable
$env:PGPASSWORD = $password

try {
    Write-Host "Executing fix script..." -ForegroundColor Green
    Write-Host ""
    
    # Execute SQL script
    $result = & psql -h $dbHost -p $port -d $database -U $username -f $scriptPath 2>&1
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host "===========================================" -ForegroundColor Green
        Write-Host "Fix applied successfully!" -ForegroundColor Green
        Write-Host "===========================================" -ForegroundColor Green
        Write-Host ""
        Write-Host "The duplicate index 'BuildingRules_BuildingId_key' has been removed." -ForegroundColor Cyan
        Write-Host "Only 'IX_BuildingRules_BuildingId' remains (EF Core managed)." -ForegroundColor Cyan
    }
    else {
        Write-Host ""
        Write-Host "Error: Fix script execution failed" -ForegroundColor Red
        Write-Host $result -ForegroundColor Red
        exit 1
    }
}
catch {
    Write-Host ""
    Write-Host "Error: Failed to execute fix script" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}
finally {
    # Clear password from environment
    Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue
}

Write-Host ""
Write-Host "Verification:" -ForegroundColor Yellow
Write-Host "  Run this query to verify only one index remains:" -ForegroundColor White
Write-Host "  SELECT indexname, indexdef FROM pg_indexes" -ForegroundColor Gray
Write-Host "    WHERE schemaname = 'public' AND tablename = 'buildingrules';" -ForegroundColor Gray
Write-Host ""

