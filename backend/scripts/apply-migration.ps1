# PowerShell script to apply EF Core migration
# Usage: .\scripts\apply-migration.ps1

$ErrorActionPreference = "Stop"

Write-Host "Applying EF Core migrations..." -ForegroundColor Green

# Navigate to Infrastructure project
$infraPath = Join-Path $PSScriptRoot "..\src\CephasOps.Infrastructure"
$apiPath = Join-Path $PSScriptRoot "..\src\CephasOps.Api"

if (-not (Test-Path $infraPath)) {
    Write-Host "Error: Infrastructure project not found at $infraPath" -ForegroundColor Red
    exit 1
}

if (-not (Test-Path $apiPath)) {
    Write-Host "Error: API project not found at $apiPath" -ForegroundColor Red
    exit 1
}

# Check if dotnet-ef is installed
$efTool = dotnet tool list -g | Select-String "dotnet-ef"
if (-not $efTool) {
    Write-Host "Installing dotnet-ef tool..." -ForegroundColor Yellow
    dotnet tool install --global dotnet-ef
}

# Apply migration
Write-Host "Applying migrations to database..." -ForegroundColor Yellow
Set-Location $infraPath

try {
    dotnet ef database update `
        --startup-project $apiPath `
        --context ApplicationDbContext

    if ($LASTEXITCODE -eq 0) {
        Write-Host "Migrations applied successfully!" -ForegroundColor Green
    } else {
        Write-Host "Migration application failed!" -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "Error applying migration: $_" -ForegroundColor Red
    exit 1
} finally {
    Set-Location $PSScriptRoot
}

