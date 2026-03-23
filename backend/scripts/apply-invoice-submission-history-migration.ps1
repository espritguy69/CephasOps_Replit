# PowerShell script to apply InvoiceSubmissionHistory migration
# Usage: .\apply-invoice-submission-history-migration.ps1 -ConnectionString "Host=...;Database=...;Username=...;Password=..."

param(
    [Parameter(Mandatory=$true)]
    [string]$ConnectionString
)

Write-Host "`n=== Applying InvoiceSubmissionHistory Migration ===" -ForegroundColor Cyan

# Parse connection string
$dbHost = ""
$database = ""
$username = ""
$password = ""
$port = 5432

$parts = $ConnectionString -split ";"
foreach ($part in $parts) {
    if ($part -match "Host=(.+)") {
        $dbHost = $matches[1]
    }
    elseif ($part -match "Database=(.+)") {
        $database = $matches[1]
    }
    elseif ($part -match "Port=(\d+)") {
        $port = [int]$matches[1]
    }
    elseif ($part -match "Username=(.+)") {
        $username = $matches[1]
    }
    elseif ($part -match "Password=(.+)") {
        $password = $matches[1]
    }
}

if (-not $dbHost -or -not $database -or -not $username) {
    Write-Host "Error: Invalid connection string. Required: Host, Database, Username" -ForegroundColor Red
    Write-Host "Example: Host=localhost;Database=cephasops;Username=postgres;Password=password" -ForegroundColor Yellow
    exit 1
}

Write-Host "Host: $dbHost" -ForegroundColor Gray
Write-Host "Database: $database" -ForegroundColor Gray
Write-Host "Username: $username" -ForegroundColor Gray
Write-Host "Port: $port" -ForegroundColor Gray

# Check if psql is available
$psqlPath = Get-Command psql -ErrorAction SilentlyContinue
if (-not $psqlPath) {
    Write-Host "Error: psql command not found. Please install PostgreSQL client tools." -ForegroundColor Red
    Write-Host "Download from: https://www.postgresql.org/download/" -ForegroundColor Yellow
    exit 1
}

# Set PGPASSWORD environment variable
$env:PGPASSWORD = $password

try {
    $scriptPath = Join-Path $PSScriptRoot "..\migrations\20241201_AddInvoiceSubmissionHistory.sql"
    $scriptPath = Resolve-Path $scriptPath -ErrorAction Stop
    
    if (-not (Test-Path $scriptPath)) {
        Write-Host "Error: Migration file not found at $scriptPath" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "`nMigration file: $scriptPath" -ForegroundColor Gray
    Write-Host "`nApplying migration..." -ForegroundColor Cyan
    
    $result = & psql -h $dbHost -p $port -d $database -U $username -f $scriptPath 2>&1
    
    Write-Host $result
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "`n✓ Migration applied successfully!" -ForegroundColor Green
        
        # Verify table exists
        Write-Host "`nVerifying table creation..." -ForegroundColor Cyan
        $verifyQuery = "SELECT EXISTS (SELECT FROM information_schema.tables WHERE table_name = 'InvoiceSubmissionHistory');"
        $verifyResult = & psql -h $dbHost -p $port -d $database -U $username -t -c $verifyQuery 2>&1
        
        if ($verifyResult -match "t") {
            Write-Host "✓ InvoiceSubmissionHistory table exists" -ForegroundColor Green
        } else {
            Write-Host "⚠ Warning: Table verification returned unexpected result" -ForegroundColor Yellow
        }
    }
    else {
        Write-Host "`n✗ Migration failed with exit code $LASTEXITCODE" -ForegroundColor Red
        exit 1
    }
}
catch {
    Write-Host "Error: $_" -ForegroundColor Red
    exit 1
}
finally {
    # Clear password from environment
    Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue
}

Write-Host "`n=== Migration Complete ===" -ForegroundColor Cyan

