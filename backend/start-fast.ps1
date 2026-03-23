# ============================================
# CephasOps Backend - Start Service (fast, skip migrations)
# ============================================
# Run this script from the backend folder
# This script skips migrations for faster startup

$ErrorActionPreference = "Stop"
$scriptRoot = $PSScriptRoot

Write-Host "🚀 Starting CephasOps Backend (fast mode, skipping migrations)..." -ForegroundColor Cyan
Write-Host "Location: $scriptRoot" -ForegroundColor Gray

# Check if .NET SDK is installed
$dotnetVersion = dotnet --version 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ .NET SDK not found. Please install .NET 10 SDK." -ForegroundColor Red
    exit 1
}
Write-Host "✓ .NET SDK: $dotnetVersion" -ForegroundColor Green

# Restore dependencies
Write-Host "`n📦 Restoring dependencies..." -ForegroundColor Yellow
Set-Location "$scriptRoot\src\CephasOps.Api"
dotnet restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Failed to restore dependencies" -ForegroundColor Red
    exit 1
}

# Skip migrations
Write-Host "`n⏩ Skipping migrations (fast mode)" -ForegroundColor Yellow

# Note about seeding
Write-Host "`n💡 Note: Database seeding runs automatically on startup" -ForegroundColor Cyan

# Start the API
Write-Host "`n🚀 Starting Backend API..." -ForegroundColor Cyan
Set-Location "$scriptRoot\src\CephasOps.Api"

$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run --urls "http://localhost:5000"

Write-Host "`n✅ Backend API running at: http://localhost:5000" -ForegroundColor Green
Write-Host "📚 Swagger UI: http://localhost:5000/swagger" -ForegroundColor Cyan

