# ============================================
# CephasOps Backend - Apply Migrations
# ============================================
# Run this script from the backend folder
# Usage: .\migrate.ps1

$ErrorActionPreference = "Stop"
$scriptRoot = $PSScriptRoot

Write-Host "🔍 Applying Database Migrations..." -ForegroundColor Cyan
Write-Host "Location: $scriptRoot" -ForegroundColor Gray

# Check if dotnet-ef is installed
$efTool = dotnet tool list -g | Select-String "dotnet-ef"
if (-not $efTool) {
    Write-Host "Installing dotnet-ef tool..." -ForegroundColor Yellow
    dotnet tool install --global dotnet-ef
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Failed to install dotnet-ef tool" -ForegroundColor Red
        exit 1
    }
}

# Apply migrations
Write-Host "`n📊 Applying migrations to database..." -ForegroundColor Yellow

try {
    dotnet ef database update `
        --project ".\src\CephasOps.Infrastructure" `
        --startup-project ".\src\CephasOps.Api" `
        --context ApplicationDbContext
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "`n✅ Migrations applied successfully!" -ForegroundColor Green
    } else {
        Write-Host "`n❌ Migration application failed!" -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "`n❌ Error applying migrations: $_" -ForegroundColor Red
    exit 1
}

