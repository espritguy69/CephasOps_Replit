# PowerShell script to apply BillingRatecard migration
# Usage: .\ApplyBillingRatecardMigration.ps1

param(
    [string]$ConnectionString = ""
)

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Apply BillingRatecard Migration Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Load connection string from appsettings.json if not provided
if ([string]::IsNullOrWhiteSpace($ConnectionString)) {
    $appSettingsPath = Join-Path $PSScriptRoot "..\..\..\CephasOps.Api\appsettings.json"
    
    if (Test-Path $appSettingsPath) {
        $appSettings = Get-Content $appSettingsPath | ConvertFrom-Json
        $ConnectionString = $appSettings.ConnectionStrings.DefaultConnection
        
        if ([string]::IsNullOrWhiteSpace($ConnectionString)) {
            Write-Host "Error: Connection string not found in appsettings.json" -ForegroundColor Red
            Write-Host "Please provide connection string as parameter:" -ForegroundColor Yellow
            Write-Host "  .\ApplyBillingRatecardMigration.ps1 -ConnectionString 'Host=...;Database=...;Username=...;Password=...'" -ForegroundColor White
            exit 1
        }
    } else {
        Write-Host "Error: appsettings.json not found at $appSettingsPath" -ForegroundColor Red
        Write-Host "Please provide connection string as parameter:" -ForegroundColor Yellow
        Write-Host "  .\ApplyBillingRatecardMigration.ps1 -ConnectionString 'Host=...;Database=...;Username=...;Password=...'" -ForegroundColor White
        exit 1
    }
}

# SQL migration file path
$sqlFile = Join-Path $PSScriptRoot "AddBillingRatecardTable.sql"

if (-not (Test-Path $sqlFile)) {
    Write-Host "Error: SQL migration file not found at $sqlFile" -ForegroundColor Red
    exit 1
}

Write-Host "Reading SQL migration file..." -ForegroundColor Green
$sqlContent = Get-Content $sqlFile -Raw

Write-Host "Connecting to PostgreSQL database..." -ForegroundColor Green

try {
    # Extract connection details from connection string
    $connParams = @{}
    $ConnectionString -split ';' | ForEach-Object {
        if ($_ -match '^([^=]+)=(.*)$') {
            $key = $matches[1].Trim()
            $value = $matches[2].Trim()
            $connParams[$key] = $value
        }
    }

    $dbHost = $connParams['Host'] -replace 'localhost|127\.0\.0\.1', 'localhost'
    $dbPort = if ($connParams['Port']) { $connParams['Port'] } else { 5432 }
    $dbDatabase = $connParams['Database']
    $dbUsername = $connParams['Username']
    $dbPassword = $connParams['Password']

    if (-not $dbHost -or -not $dbDatabase -or -not $dbUsername -or -not $dbPassword) {
        Write-Host "Error: Invalid connection string format" -ForegroundColor Red
        Write-Host "Required: Host, Database, Username, Password" -ForegroundColor Yellow
        exit 1
    }

    Write-Host "Host: $dbHost" -ForegroundColor Gray
    Write-Host "Port: $dbPort" -ForegroundColor Gray
    Write-Host "Database: $dbDatabase" -ForegroundColor Gray
    Write-Host "Username: $dbUsername" -ForegroundColor Gray
    Write-Host ""

    # Check if psql is available
    $psqlPath = Get-Command psql -ErrorAction SilentlyContinue
    if (-not $psqlPath) {
        Write-Host "Error: psql command not found. Please install PostgreSQL client tools." -ForegroundColor Red
        Write-Host "Alternatively, you can run the SQL file manually in pgAdmin or another PostgreSQL client." -ForegroundColor Yellow
        Write-Host ""
        Write-Host "SQL file location: $sqlFile" -ForegroundColor White
        exit 1
    }

    # Set PGPASSWORD environment variable for psql
    $env:PGPASSWORD = $dbPassword

    Write-Host "Executing SQL migration..." -ForegroundColor Green
    Write-Host ""

    # Execute SQL using psql
    $sqlContent | & psql -h $dbHost -p $dbPort -U $dbUsername -d $dbDatabase -v ON_ERROR_STOP=1

    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host "========================================" -ForegroundColor Green
        Write-Host "Migration applied successfully!" -ForegroundColor Green
        Write-Host "========================================" -ForegroundColor Green
    } else {
        Write-Host ""
        Write-Host "Error: Migration failed with exit code $LASTEXITCODE" -ForegroundColor Red
        exit 1
    }
}
catch {
    Write-Host ""
    Write-Host "Error: $_" -ForegroundColor Red
    exit 1
}
finally {
    # Clear password from environment
    Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue
}

Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "  1. Verify the BillingRatecards table was created" -ForegroundColor White
Write-Host "  2. Test the Partner Rates API endpoints" -ForegroundColor White
Write-Host "  3. Test the Partner Rates frontend page" -ForegroundColor White

