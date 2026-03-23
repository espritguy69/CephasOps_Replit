# PowerShell script to apply RemoveCompanyFeature migration
# This script drops company-related tables and makes CompanyId nullable

param(
    [Parameter(Mandatory=$false)]
    [string]$ConnectionString = "Host=db.jgahsbfoydwdgipcjvxe.supabase.co;Database=postgres;Username=postgres;Password=J@saw007;SslMode=Require",
    [Parameter(Mandatory=$false)]
    [switch]$Force
)

Write-Host "Removing Company Feature from Database..." -ForegroundColor Yellow
Write-Host "This will:" -ForegroundColor Yellow
Write-Host "  1. Drop company-related tables (Companies, UserCompanies, CompanyDocuments, Verticals, etc.)" -ForegroundColor Yellow
Write-Host "  2. Make CompanyId nullable in all remaining tables" -ForegroundColor Yellow
Write-Host ""

if (-not $Force) {
    $confirmation = Read-Host "Are you sure you want to proceed? This cannot be undone! (yes/no)"
    if ($confirmation -ne "yes") {
        Write-Host "Migration cancelled." -ForegroundColor Red
        exit
    }
} else {
    Write-Host "Force flag set - proceeding without confirmation" -ForegroundColor Yellow
}

$sqlFile = Join-Path $PSScriptRoot "RemoveCompanyFeature.sql"
$sql = Get-Content $sqlFile -Raw

try {
    # Use psql if available, otherwise use .NET connection
    $passwordPart = ($ConnectionString -split "Password=")[1]
    $env:PGPASSWORD = ($passwordPart -split ";")[0]
    
    $hostPart = ($ConnectionString -split "Host=")[1]
    $dbHost = ($hostPart -split ";")[0]
    
    $databasePart = ($ConnectionString -split "Database=")[1]
    $database = ($databasePart -split ";")[0]
    
    $usernamePart = ($ConnectionString -split "Username=")[1]
    $username = ($usernamePart -split ";")[0]
    
    Write-Host "Connecting to database: $database on $dbHost" -ForegroundColor Cyan
    
    # Save SQL to temp file
    $tempFile = [System.IO.Path]::GetTempFileName()
    Set-Content -Path $tempFile -Value $sql
    
    # Execute using psql
    $psqlPath = "psql"
    if (Get-Command psql -ErrorAction SilentlyContinue) {
        & $psqlPath -h $dbHost -U $username -d $database -f $tempFile
        if ($LASTEXITCODE -eq 0) {
            Write-Host "`nMigration completed successfully!" -ForegroundColor Green
        } else {
            Write-Host "`nMigration failed. Check the error messages above." -ForegroundColor Red
            exit 1
        }
    } else {
        Write-Host "psql not found. Please install PostgreSQL client tools or run the SQL manually." -ForegroundColor Yellow
        Write-Host "SQL file location: $sqlFile" -ForegroundColor Yellow
    }
    
    Remove-Item $tempFile -ErrorAction SilentlyContinue
} catch {
    Write-Host "Error executing migration: $_" -ForegroundColor Red
    exit 1
}

