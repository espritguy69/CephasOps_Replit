# PowerShell script to apply Phase 5 database migration
# Usage: .\ApplyPhase5EntitiesMigration.ps1 -ConnectionString "Host=db.jgahsbfoydwdgipcjvxe.supabase.co;Database=postgres;Username=postgres;Password=YOUR_PASSWORD;SslMode=Require"

param(
    [Parameter(Mandatory=$false)]
    [string]$ConnectionString = "Host=db.jgahsbfoydwdgipcjvxe.supabase.co;Database=postgres;Username=postgres;Password=J@saw007;SslMode=Require"
)

$ErrorActionPreference = "Stop"

Write-Host "===========================================" -ForegroundColor Cyan
Write-Host "Phase 5 Database Migration" -ForegroundColor Cyan
Write-Host "===========================================" -ForegroundColor Cyan
Write-Host ""

# Parse connection string
$dbHost = ""
$database = ""
$username = ""
$password = ""

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
}

if (-not $dbHost -or -not $database -or -not $username) {
    Write-Host "Error: Invalid connection string. Required: Host, Database, Username, Password" -ForegroundColor Red
    exit 1
}

$scriptPath = Join-Path $PSScriptRoot "AddPhase5Entities.sql"

if (-not (Test-Path $scriptPath)) {
    Write-Host "Error: Migration script not found at $scriptPath" -ForegroundColor Red
    exit 1
}

Write-Host "Applying Phase 5 migration..." -ForegroundColor Yellow
Write-Host "Host: $dbHost" -ForegroundColor Gray
Write-Host "Database: $database" -ForegroundColor Gray
Write-Host "Username: $username" -ForegroundColor Gray
Write-Host ""

# Set PGPASSWORD environment variable
$env:PGPASSWORD = $password

try {
    # Execute SQL script
    $result = & psql -h $dbHost -d $database -U $username -f $scriptPath 2>&1
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host "===========================================" -ForegroundColor Green
        Write-Host "Migration completed successfully!" -ForegroundColor Green
        Write-Host "===========================================" -ForegroundColor Green
        Write-Host ""
        Write-Host "Created tables:" -ForegroundColor Cyan
        Write-Host "  - GlobalSettings" -ForegroundColor White
        Write-Host "  - MaterialTemplates" -ForegroundColor White
        Write-Host "  - MaterialTemplateItems" -ForegroundColor White
        Write-Host "  - DocumentTemplates" -ForegroundColor White
        Write-Host "  - GeneratedDocuments" -ForegroundColor White
        Write-Host "  - DocumentPlaceholderDefinitions" -ForegroundColor White
        Write-Host "  - KpiProfiles" -ForegroundColor White
    }
    else {
        Write-Host ""
        Write-Host "Error: Migration failed" -ForegroundColor Red
        Write-Host $result -ForegroundColor Red
        exit 1
    }
}
catch {
    Write-Host ""
    Write-Host "Error: Failed to execute migration" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}
finally {
    # Clear password from environment
    Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue
}

Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  1. Verify tables were created correctly" -ForegroundColor White
Write-Host "  2. Test API endpoints via Swagger" -ForegroundColor White
Write-Host "  3. Create frontend integration" -ForegroundColor White

