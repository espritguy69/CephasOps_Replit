# PowerShell script to apply Phase 4 entities migration
# Usage: .\ApplyPhase4EntitiesMigration.ps1 -ConnectionString "Host=localhost;Database=cephasops;Username=postgres;Password=YOUR_PASSWORD"

param(
    [Parameter(Mandatory=$true)]
    [string]$ConnectionString
)

$ErrorActionPreference = "Stop"

Write-Host "=== Applying Phase 4 Entities Migration ===" -ForegroundColor Green
Write-Host ""

# Parse connection string
$host = ""
$database = ""
$username = ""
$password = ""

$ConnectionString -split ';' | ForEach-Object {
    $parts = $_ -split '=', 2
    $key = $parts[0].Trim()
    $value = $parts[1].Trim()
    
    switch ($key) {
        "Host" { $host = $value }
        "Database" { $database = $value }
        "Username" { $username = $value }
        "Password" { $password = $value }
    }
}

if (-not $host -or -not $database -or -not $username -or -not $password) {
    Write-Host "Error: Invalid connection string. Must include Host, Database, Username, and Password." -ForegroundColor Red
    exit 1
}

# Get script directory
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$sqlFile = Join-Path $scriptDir "AddPhase4Entities.sql"

if (-not (Test-Path $sqlFile)) {
    Write-Host "Error: SQL file not found at $sqlFile" -ForegroundColor Red
    exit 1
}

Write-Host "SQL File: $sqlFile" -ForegroundColor Yellow
Write-Host "Host: $host" -ForegroundColor Yellow
Write-Host "Database: $database" -ForegroundColor Yellow
Write-Host "Username: $username" -ForegroundColor Yellow
Write-Host ""

# Check if psql is available
$psqlPath = Get-Command psql -ErrorAction SilentlyContinue
if (-not $psqlPath) {
    Write-Host "Error: psql command not found. Please ensure PostgreSQL client tools are installed." -ForegroundColor Red
    exit 1
}

# Set password environment variable
$env:PGPASSWORD = $password

Write-Host "Applying migration..." -ForegroundColor Cyan

try {
    $sqlContent = Get-Content $sqlFile -Raw
    $sqlContent | & psql -h $host -d $database -U $username -f $sqlFile 2>&1
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host "=== Migration Applied Successfully ===" -ForegroundColor Green
        Write-Host ""
        Write-Host "Created tables:" -ForegroundColor Yellow
        Write-Host "  - PayrollPeriods, PayrollRuns, PayrollLines, JobEarningRecords, SiRatePlans" -ForegroundColor Cyan
        Write-Host "  - PnlPeriods, PnlFacts, PnlDetailPerOrders, OverheadEntries" -ForegroundColor Cyan
        Write-Host "  - Departments, MaterialAllocations" -ForegroundColor Cyan
        Write-Host ""
        Write-Host "Total: 11 tables with 20+ indexes" -ForegroundColor Yellow
    } else {
        Write-Host ""
        Write-Host "Error: Migration failed with exit code $LASTEXITCODE" -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host ""
    Write-Host "Error: $_" -ForegroundColor Red
    exit 1
} finally {
    # Clear password from environment
    Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue
}

