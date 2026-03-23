# PowerShell script to apply Phase 3 entities migration
# Usage: .\ApplyPhase3EntitiesMigration.ps1 -ConnectionString "Host=localhost;Database=cephasops;Username=postgres;Password=password"

param(
    [Parameter(Mandatory=$true)]
    [string]$ConnectionString
)

Write-Host "Applying Phase 3 Entities Migration..." -ForegroundColor Green

# Read the SQL migration file
$migrationPath = Join-Path $PSScriptRoot "AddPhase3Entities.sql"
$sql = Get-Content $migrationPath -Raw

if (-not $sql) {
    Write-Host "Error: Could not read migration file at $migrationPath" -ForegroundColor Red
    exit 1
}

try {
    # Load Npgsql assembly (if available) or use psql
    $psqlPath = Get-Command psql -ErrorAction SilentlyContinue
    
    if ($psqlPath) {
        Write-Host "Using psql to apply migration..." -ForegroundColor Yellow
        
        # Extract connection details from connection string
        $host = ($ConnectionString -split 'Host=')[1] -split ';' | Select-Object -First 1
        $database = ($ConnectionString -split 'Database=')[1] -split ';' | Select-Object -First 1
        $username = ($ConnectionString -split 'Username=')[1] -split ';' | Select-Object -First 1
        $password = ($ConnectionString -split 'Password=')[1] -split ';' | Select-Object -First 1
        
        # Set PGPASSWORD environment variable
        $env:PGPASSWORD = $password
        
        # Execute SQL using psql
        $sql | & psql -h $host -d $database -U $username -f -
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "Migration applied successfully!" -ForegroundColor Green
        } else {
            Write-Host "Error applying migration. Exit code: $LASTEXITCODE" -ForegroundColor Red
            exit 1
        }
    } else {
        Write-Host "psql not found. Please install PostgreSQL client tools or apply migration manually." -ForegroundColor Yellow
        Write-Host "Connection String: $ConnectionString" -ForegroundColor Cyan
        Write-Host "SQL File: $migrationPath" -ForegroundColor Cyan
        Write-Host ""
        Write-Host "You can apply the migration using:" -ForegroundColor Yellow
        Write-Host "  psql -h <host> -d <database> -U <username> -f $migrationPath" -ForegroundColor White
        exit 1
    }
} catch {
    Write-Host "Error applying migration: $_" -ForegroundColor Red
    exit 1
} finally {
    # Clear password from environment
    if ($env:PGPASSWORD) {
        Remove-Item Env:\PGPASSWORD
    }
}

Write-Host "Migration completed!" -ForegroundColor Green

