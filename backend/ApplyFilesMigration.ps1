# Apply Files Table Migration
# This script will prompt you for database credentials and apply the migration

param(
    [string]$Host = "localhost",
    [int]$Port = 5432,
    [string]$Database = "cephasops",
    [string]$Username = "postgres",
    [string]$Password
)

$ErrorActionPreference = "Stop"

Write-Host "=== Files Table Migration ===" -ForegroundColor Green
Write-Host ""

# Get password if not provided
if (-not $Password) {
    $securePassword = Read-Host "Enter database password" -AsSecureString
    $BSTR = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePassword)
    $Password = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($BSTR)
}

# Set password environment variable
$env:PGPASSWORD = $Password

# Get psql path
$psqlPath = "C:\Program Files\PostgreSQL\18\bin\psql.exe"
if (-not (Test-Path $psqlPath)) {
    $psqlPath = Get-Command psql -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Source
    if (-not $psqlPath) {
        Write-Host "Error: psql not found. Please install PostgreSQL client tools." -ForegroundColor Red
        exit 1
    }
}

Write-Host "Connecting to: $Database on $Host`:$Port as $Username" -ForegroundColor Cyan
Write-Host "Applying migration..." -ForegroundColor Yellow
Write-Host ""

# Get migration script path
$scriptPath = Join-Path $PSScriptRoot "src\CephasOps.Infrastructure\Persistence\Migrations\AddFilesTable.sql"

if (-not (Test-Path $scriptPath)) {
    Write-Host "Error: Migration script not found at: $scriptPath" -ForegroundColor Red
    exit 1
}

# Execute migration
try {
    Get-Content $scriptPath | & $psqlPath -h $Host -p $Port -U $Username -d $Database
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host "✓ Migration applied successfully!" -ForegroundColor Green
        Write-Host ""
        Write-Host "Verifying table creation..." -ForegroundColor Cyan
        
        # Verify table exists
        $verifyQuery = "SELECT table_name FROM information_schema.tables WHERE table_name = 'Files';"
        $result = $verifyQuery | & $psqlPath -h $Host -p $Port -U $Username -d $Database -t -A
        
        if ($result -match "Files") {
            Write-Host "✓ Files table verified!" -ForegroundColor Green
        } else {
            Write-Host "⚠ Warning: Could not verify table creation" -ForegroundColor Yellow
        }
    } else {
        Write-Host ""
        Write-Host "✗ Migration failed with exit code: $LASTEXITCODE" -ForegroundColor Red
        exit $LASTEXITCODE
    }
} catch {
    Write-Host ""
    Write-Host "✗ Error applying migration: $_" -ForegroundColor Red
    exit 1
} finally {
    # Clear password from environment
    Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue
}

Write-Host ""
Write-Host "Migration complete!" -ForegroundColor Green

