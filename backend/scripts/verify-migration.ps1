# PowerShell script to verify database migration
# Usage: .\verify-migration.ps1 -ConnectionString "Host=...;Database=...;Username=...;Password=..."

param(
    [string]$ConnectionString = ""
)

if ([string]::IsNullOrEmpty($ConnectionString)) {
    Write-Host "Error: ConnectionString is required" -ForegroundColor Red
    Write-Host "Usage: .\verify-migration.ps1 -ConnectionString 'Host=...;Database=...;Username=...;Password=...'" -ForegroundColor Yellow
    exit 1
}

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
    exit 1
}

Write-Host "Verifying database migration..." -ForegroundColor Cyan
Write-Host "Host: $dbHost" -ForegroundColor Gray
Write-Host "Database: $database" -ForegroundColor Gray
Write-Host "Username: $username" -ForegroundColor Gray

# Check if psql is available
$psqlPath = Get-Command psql -ErrorAction SilentlyContinue
if (-not $psqlPath) {
    Write-Host "Error: psql command not found. Please install PostgreSQL client tools." -ForegroundColor Red
    exit 1
}

# Set PGPASSWORD environment variable
$env:PGPASSWORD = $password

try {
    $scriptPath = Join-Path $PSScriptRoot "verify-migration.sql"
    if (-not (Test-Path $scriptPath)) {
        Write-Host "Error: verify-migration.sql not found at $scriptPath" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "`nRunning verification script..." -ForegroundColor Cyan
    $result = & psql -h $dbHost -p $port -d $database -U $username -f $scriptPath 2>&1
    
    Write-Host $result
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "`n✓ Verification complete" -ForegroundColor Green
    }
    else {
        Write-Host "`n✗ Verification failed" -ForegroundColor Red
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

