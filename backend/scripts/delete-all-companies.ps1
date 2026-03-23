# ============================================
# Delete All Companies Script
# ============================================
# This script deletes all companies from the database
# WARNING: This will permanently delete all companies!
# ============================================

param(
    [switch]$Confirm
)

$ErrorActionPreference = "Stop"

Write-Host "============================================" -ForegroundColor Yellow
Write-Host "  Delete All Companies" -ForegroundColor Yellow
Write-Host "============================================" -ForegroundColor Yellow
Write-Host ""

if (-not $Confirm) {
    Write-Host "WARNING: This will delete ALL companies from the database!" -ForegroundColor Red
    Write-Host ""
    $response = Read-Host "Are you sure you want to continue? Type 'YES' to confirm"
    if ($response -ne "YES") {
        Write-Host "Operation cancelled." -ForegroundColor Yellow
        exit 0
    }
}

$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent (Split-Path -Parent $scriptPath)
$apiPath = Join-Path $projectRoot "src\CephasOps.Api"
$infraPath = Join-Path $projectRoot "src\CephasOps.Infrastructure"

# Get connection string
$appsettingsPath = Join-Path $apiPath "appsettings.json"
if (-not (Test-Path $appsettingsPath)) {
    Write-Host "ERROR: appsettings.json not found at $appsettingsPath" -ForegroundColor Red
    exit 1
}

$appsettings = Get-Content $appsettingsPath -Raw | ConvertFrom-Json
$connString = $appsettings.ConnectionStrings.DefaultConnection

if (-not $connString) {
    Write-Host "ERROR: Connection string not found" -ForegroundColor Red
    exit 1
}

# Parse connection string
$host = ($connString -split 'Host=')[1] -split ';' | Select-Object -First 1
$db = ($connString -split 'Database=')[1] -split ';' | Select-Object -First 1
$user = ($connString -split 'Username=')[1] -split ';' | Select-Object -First 1
$pass = ($connString -split 'Password=')[1] -split ';' | Select-Object -First 1

Write-Host "Database: $db" -ForegroundColor Cyan
Write-Host "Host: $host" -ForegroundColor Cyan
Write-Host "User: $user" -ForegroundColor Cyan
Write-Host ""

Write-Host "Executing DELETE command..." -ForegroundColor Cyan

# Try using psql
$psqlPath = Get-Command psql -ErrorAction SilentlyContinue
if ($psqlPath) {
    try {
        $env:PGPASSWORD = $pass
        $sqlCommand = 'DELETE FROM "Companies";'
        
        Write-Host "Using psql to execute SQL..." -ForegroundColor Cyan
        
        $result = echo $sqlCommand | psql -h $host -p 5432 -U $user -d $db -t -A 2>&1
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host ""
            Write-Host "============================================" -ForegroundColor Green
            Write-Host "  All companies deleted successfully!" -ForegroundColor Green
            Write-Host "============================================" -ForegroundColor Green
        } else {
            Write-Host "ERROR: Failed to execute SQL command" -ForegroundColor Red
            Write-Host $result -ForegroundColor Red
            Write-Host ""
            Write-Host "Please run this SQL command manually:" -ForegroundColor Yellow
            Write-Host 'DELETE FROM "Companies";' -ForegroundColor Cyan
        }
    }
    catch {
        Write-Host "ERROR: $($_.Exception.Message)" -ForegroundColor Red
        Write-Host ""
        Write-Host "Please run this SQL command manually:" -ForegroundColor Yellow
        Write-Host 'DELETE FROM "Companies";' -ForegroundColor Cyan
    }
    finally {
        Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue
    }
}
else {
    Write-Host "psql not found. Please use one of these methods:" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Method 1: Using pgAdmin or DBeaver, run:" -ForegroundColor Cyan
    Write-Host '  DELETE FROM "Companies";' -ForegroundColor White
    Write-Host ""
    Write-Host "Method 2: Install PostgreSQL client tools and run this script again" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Method 3: Use the API to delete companies one by one" -ForegroundColor Cyan
}
