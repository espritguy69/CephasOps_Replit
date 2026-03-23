# ============================================
# CephasOps Backend - Build Project
# ============================================
# Run this script from the backend folder
# Usage: .\build.ps1 [--clean] [--configuration Release|Debug]

param(
    [switch]$Clean,
    [string]$Configuration = "Debug"
)

$ErrorActionPreference = "Stop"
$scriptRoot = $PSScriptRoot

Write-Host "🔨 Building CephasOps Backend..." -ForegroundColor Cyan
Write-Host "Configuration: $Configuration" -ForegroundColor Gray

# Check if .NET SDK is installed
$dotnetVersion = dotnet --version 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ .NET SDK not found. Please install .NET 10 SDK." -ForegroundColor Red
    exit 1
}

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

# Build
Write-Host "`n🔨 Building project (Configuration: $Configuration)..." -ForegroundColor Yellow
dotnet build --configuration $Configuration
if ($LASTEXITCODE -ne 0) {
    Write-Host "`n❌ Build failed!" -ForegroundColor Red
    exit 1
}

Write-Host "`n✅ Build completed successfully!" -ForegroundColor Green
Write-Host "Output location: $scriptRoot\src\CephasOps.Api\bin\$Configuration\net10.0" -ForegroundColor Cyan

