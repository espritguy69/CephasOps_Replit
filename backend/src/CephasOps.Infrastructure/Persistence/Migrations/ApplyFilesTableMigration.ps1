# PowerShell script to apply Files table migration
# Usage: .\ApplyFilesTableMigration.ps1 -ConnectionString "Host=localhost;Database=cephasops;Username=postgres;Password=password"

param(
    [Parameter(Mandatory=$true)]
    [string]$ConnectionString
)

$ErrorActionPreference = "Stop"

Write-Host "Applying Files table migration..." -ForegroundColor Green

# Read the SQL script
$sqlScript = Get-Content -Path "$PSScriptRoot\AddFilesTable.sql" -Raw

try {
    # Parse connection string to extract connection details
    $connParts = @{}
    $ConnectionString -split ';' | ForEach-Object {
        if ($_ -match '^([^=]+)=(.*)$') {
            $connParts[$matches[1].Trim()] = $matches[2].Trim()
        }
    }

    $host = $connParts['Host'] -replace '^.*://', '' -replace ':\d+$', ''
    $port = if ($connParts['Port']) { $connParts['Port'] } else { "5432" }
    $database = $connParts['Database']
    $username = $connParts['Username']
    $password = $connParts['Password']

    Write-Host "Connecting to database: $database on $host`:$port" -ForegroundColor Cyan

    # Use psql if available, otherwise provide instructions
    $psqlPath = Get-Command psql -ErrorAction SilentlyContinue
    
    if ($psqlPath) {
        Write-Host "Using psql to apply migration..." -ForegroundColor Cyan
        
        # Set PGPASSWORD environment variable
        $env:PGPASSWORD = $password
        
        # Execute SQL script
        $sqlScript | & psql -h $host -p $port -U $username -d $database -f -
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "Migration applied successfully!" -ForegroundColor Green
        } else {
            Write-Host "Migration failed with exit code $LASTEXITCODE" -ForegroundColor Red
            exit $LASTEXITCODE
        }
    } else {
        Write-Host "psql not found. Please install PostgreSQL client tools or use one of the following methods:" -ForegroundColor Yellow
        Write-Host ""
        Write-Host "Option 1: Install PostgreSQL client tools and run this script again" -ForegroundColor Cyan
        Write-Host "Option 2: Use pgAdmin or another PostgreSQL client to execute the SQL script:" -ForegroundColor Cyan
        Write-Host "  File: $PSScriptRoot\AddFilesTable.sql" -ForegroundColor White
        Write-Host ""
        Write-Host "Option 3: Use .NET EF Core migration:" -ForegroundColor Cyan
        Write-Host "  dotnet ef migrations add AddFilesTable --project <InfrastructureProject> --startup-project <ApiProject>" -ForegroundColor White
        Write-Host "  dotnet ef database update --project <InfrastructureProject> --startup-project <ApiProject>" -ForegroundColor White
        Write-Host ""
        Write-Host "SQL Script Content:" -ForegroundColor Cyan
        Write-Host $sqlScript -ForegroundColor White
    }
} catch {
    Write-Host "Error applying migration: $_" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}

