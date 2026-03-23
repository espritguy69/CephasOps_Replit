# PowerShell script to migrate Service Installer levels and types
# This script updates existing Service Installers to use the new InstallerLevel enum
# and ensures proper InstallerType classification

param(
    [string]$Host = "localhost",
    [int]$Port = 5432,
    [string]$Database = "cephasops",
    [string]$Username = "postgres",
    [string]$Password = "J@saw007"
)

$ErrorActionPreference = "Stop"

Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "Service Installer Level Migration" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""

# Set PGPASSWORD environment variable
$env:PGPASSWORD = $Password

try {
    Write-Host "Connecting to database: $Database on $Host:$Port" -ForegroundColor Yellow
    
    # Check connection
    $connectionTest = psql -h $Host -p $Port -U $Username -d $Database -c "SELECT 1;" 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Host "ERROR: Cannot connect to database!" -ForegroundColor Red
        Write-Host $connectionTest -ForegroundColor Red
        exit 1
    }
    
    Write-Host "✓ Database connection successful" -ForegroundColor Green
    Write-Host ""
    
    # Get current counts before migration
    Write-Host "Current Service Installer counts:" -ForegroundColor Yellow
    $beforeCounts = psql -h $Host -p $Port -U $Username -d $Database -t -c @"
SELECT 
    'Total: ' || COUNT(*)::text || 
    ' | InHouse: ' || COUNT(CASE WHEN "InstallerType" = 'InHouse' THEN 1 END)::text ||
    ' | Subcontractor: ' || COUNT(CASE WHEN "InstallerType" = 'Subcontractor' THEN 1 END)::text ||
    ' | Junior: ' || COUNT(CASE WHEN "SiLevel" = 'Junior' THEN 1 END)::text ||
    ' | Senior: ' || COUNT(CASE WHEN "SiLevel" = 'Senior' THEN 1 END)::text ||
    ' | Subcon (old): ' || COUNT(CASE WHEN "SiLevel" = 'Subcon' THEN 1 END)::text
FROM "ServiceInstallers"
WHERE "IsDeleted" = false;
"@
    Write-Host $beforeCounts.Trim() -ForegroundColor Cyan
    Write-Host ""
    
    # Ask for confirmation
    $confirmation = Read-Host "Do you want to proceed with the migration? (yes/no)"
    if ($confirmation -ne "yes") {
        Write-Host "Migration cancelled." -ForegroundColor Yellow
        exit 0
    }
    
    Write-Host ""
    Write-Host "Running migration script..." -ForegroundColor Yellow
    
    # Run the migration SQL script
    $scriptPath = Join-Path $PSScriptRoot "migrate-service-installer-levels.sql"
    if (-not (Test-Path $scriptPath)) {
        Write-Host "ERROR: Migration script not found at: $scriptPath" -ForegroundColor Red
        exit 1
    }
    
    $result = Get-Content $scriptPath | psql -h $Host -p $Port -U $Username -d $Database 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Host "ERROR: Migration failed!" -ForegroundColor Red
        Write-Host $result -ForegroundColor Red
        exit 1
    }
    
    Write-Host "✓ Migration completed successfully" -ForegroundColor Green
    Write-Host ""
    
    # Get counts after migration
    Write-Host "Service Installer counts after migration:" -ForegroundColor Yellow
    $afterCounts = psql -h $Host -p $Port -U $Username -d $Database -t -c @"
SELECT 
    'Total: ' || COUNT(*)::text || 
    ' | InHouse: ' || COUNT(CASE WHEN "InstallerType" = 'InHouse' THEN 1 END)::text ||
    ' | Subcontractor: ' || COUNT(CASE WHEN "InstallerType" = 'Subcontractor' THEN 1 END)::text ||
    ' | Junior: ' || COUNT(CASE WHEN "SiLevel" = 'Junior' THEN 1 END)::text ||
    ' | Senior: ' || COUNT(CASE WHEN "SiLevel" = 'Senior' THEN 1 END)::text ||
    ' | Subcon (old): ' || COUNT(CASE WHEN "SiLevel" = 'Subcon' THEN 1 END)::text
FROM "ServiceInstallers"
WHERE "IsDeleted" = false;
"@
    Write-Host $afterCounts.Trim() -ForegroundColor Cyan
    Write-Host ""
    
    Write-Host "=========================================" -ForegroundColor Cyan
    Write-Host "Migration completed successfully!" -ForegroundColor Green
    Write-Host "=========================================" -ForegroundColor Cyan
    
} catch {
    Write-Host ""
    Write-Host "ERROR: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
} finally {
    # Clear password from environment
    Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue
}

