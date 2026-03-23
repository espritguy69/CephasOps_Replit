# PowerShell script to import Service Installers from CSV
# This script reads service-installers-template.csv and imports data via SQL
# Do not commit connection strings. Pass -ConnectionString or set $env:DefaultConnection.

param(
    [string]$ConnectionString = $env:DefaultConnection
)

$ErrorActionPreference = "Stop"
if ([string]::IsNullOrWhiteSpace($ConnectionString)) {
    Write-Host "ERROR: Connection string required. Set env DefaultConnection or pass -ConnectionString." -ForegroundColor Red
    exit 1
}
Write-Host "Importing Service Installers from CSV..." -ForegroundColor Cyan

# Check if psql is available
$psqlPath = Get-Command psql -ErrorAction SilentlyContinue
if (-not $psqlPath) {
    Write-Host "ERROR: psql command not found. Please install PostgreSQL client tools." -ForegroundColor Red
    exit 1
}

# Path to SQL script
$sqlScript = Join-Path $PSScriptRoot "import-service-installers-from-csv.sql"

if (-not (Test-Path $sqlScript)) {
    Write-Host "ERROR: SQL script not found at: $sqlScript" -ForegroundColor Red
    exit 1
}

Write-Host "Executing SQL script..." -ForegroundColor Yellow

# Extract connection details from connection string
$host = ($ConnectionString -split "Host=")[1] -split ";")[0]
$port = ($ConnectionString -split "Port=")[1] -split ";")[0]
$database = ($ConnectionString -split "Database=")[1] -split ";")[0]
$username = ($ConnectionString -split "Username=")[1] -split ";")[0]
$password = ($ConnectionString -split "Password=")[1] -split ";")[0]

# Set PGPASSWORD environment variable
$env:PGPASSWORD = $password

try {
    # Execute SQL script
    $result = & psql -h $host -p $port -U $username -d $database -f $sqlScript 2>&1
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Service Installers imported successfully!" -ForegroundColor Green
        Write-Host $result
    } else {
        Write-Host "ERROR: Import failed!" -ForegroundColor Red
        Write-Host $result
        exit 1
    }
} finally {
    # Clear password from environment
    Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue
}

