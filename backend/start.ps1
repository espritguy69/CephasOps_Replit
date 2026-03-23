# ============================================
# CephasOps Backend - Start Service (with migrations)
# ============================================
# Run this script from the backend folder
# Usage: .\start.ps1 [--clean] [--build-only]

param(
    [switch]$Clean,
    [switch]$BuildOnly
)

$ErrorActionPreference = "Stop"
$scriptRoot = $PSScriptRoot

Write-Host "🚀 Starting CephasOps Backend (with migrations)..." -ForegroundColor Cyan
Write-Host "Location: $scriptRoot" -ForegroundColor Gray

# Check if .NET SDK is installed
$dotnetVersion = dotnet --version 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ .NET SDK not found. Please install .NET 10 SDK." -ForegroundColor Red
    exit 1
}
Write-Host "✓ .NET SDK: $dotnetVersion" -ForegroundColor Green

# Clean if requested
if ($Clean) {
    Write-Host "`n🧹 Cleaning build artifacts..." -ForegroundColor Yellow
    Set-Location "$scriptRoot\src\CephasOps.Api"
    dotnet clean
    if ($LASTEXITCODE -ne 0) {
        Write-Host "⚠ Clean had issues, but continuing..." -ForegroundColor Yellow
    }
}

# Restore dependencies
Write-Host "`n📦 Restoring dependencies..." -ForegroundColor Yellow
Set-Location "$scriptRoot\src\CephasOps.Api"
dotnet restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Failed to restore dependencies" -ForegroundColor Red
    exit 1
}

# Build (optional, but good for catching errors early)
if ($Clean -or $BuildOnly) {
    Write-Host "`n🔨 Building project..." -ForegroundColor Yellow
    dotnet build
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Build failed!" -ForegroundColor Red
        exit 1
    }
    Write-Host "✓ Build successful" -ForegroundColor Green
    
    if ($BuildOnly) {
        Write-Host "`n✅ Build completed. Exiting (--BuildOnly flag)." -ForegroundColor Green
        exit 0
    }
}

# Apply migrations
Write-Host "`n🔍 Checking for pending migrations..." -ForegroundColor Yellow
Set-Location "$scriptRoot"

# Check if dotnet-ef is installed
$efTool = dotnet tool list -g | Select-String "dotnet-ef"
if (-not $efTool) {
    Write-Host "Installing dotnet-ef tool..." -ForegroundColor Yellow
    dotnet tool install --global dotnet-ef
}

try {
    dotnet ef database update `
        --project ".\src\CephasOps.Infrastructure" `
        --startup-project ".\src\CephasOps.Api"
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✓ Migrations applied successfully" -ForegroundColor Green
    } else {
        Write-Host "⚠ Migration application had issues, but continuing..." -ForegroundColor Yellow
    }
} catch {
    Write-Host "⚠ Migration error (non-fatal): $_" -ForegroundColor Yellow
}

# Note about seeding
Write-Host "`n💡 Note: Database seeding runs automatically on startup" -ForegroundColor Cyan

# Start the API
Write-Host "`n🚀 Starting Backend API..." -ForegroundColor Cyan
Set-Location "$scriptRoot\src\CephasOps.Api"

$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run --urls "http://localhost:5000"

Write-Host "`n✅ Backend API running at: http://localhost:5000" -ForegroundColor Green
Write-Host "📚 Swagger UI: http://localhost:5000/swagger" -ForegroundColor Cyan

