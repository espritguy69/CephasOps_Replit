# Fix Duplicate Splitter Types
# This script identifies and removes duplicate 1:8 splitter types

param(
    [string]$Host = "localhost",
    [int]$Port = 5432,
    [string]$Database = "cephasops",
    [string]$Username = "postgres",
    [string]$Password = "J@saw007"
)

$ErrorActionPreference = "Stop"

Write-Host "=== Splitter Type Duplicate Fix ===" -ForegroundColor Cyan
Write-Host ""

# Set PGPASSWORD environment variable
$env:PGPASSWORD = $Password

try {
    Write-Host "Step 1: Finding duplicate splitter types..." -ForegroundColor Yellow
    $findScript = Join-Path $PSScriptRoot "find-duplicate-splitter-types.sql"
    psql -h $Host -p $Port -U $Username -d $Database -f $findScript
    
    Write-Host ""
    Write-Host "Step 2: Fixing duplicates (keeping oldest record)..." -ForegroundColor Yellow
    $fixScript = Join-Path $PSScriptRoot "fix-duplicate-splitter-types.sql"
    psql -h $Host -p $Port -U $Username -d $Database -f $fixScript
    
    Write-Host ""
    Write-Host "Step 3: Adding unique constraint..." -ForegroundColor Yellow
    $constraintScript = Join-Path $PSScriptRoot "add-unique-constraint-splitter-types.sql"
    psql -h $Host -p $Port -U $Username -d $Database -f $constraintScript
    
    Write-Host ""
    Write-Host "=== Fix Complete ===" -ForegroundColor Green
    Write-Host "Duplicate splitter types have been removed and unique constraint added."
}
catch {
    Write-Host "Error: $_" -ForegroundColor Red
    exit 1
}
finally {
    # Clear password from environment
    Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue
}

