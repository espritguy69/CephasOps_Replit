# ============================================
# CephasOps Backend - Clean Build Artifacts
# ============================================
# Run this script from the backend folder

$ErrorActionPreference = "Stop"
$scriptRoot = $PSScriptRoot

Write-Host "🧹 Cleaning CephasOps Backend build artifacts..." -ForegroundColor Yellow

# Clean API project
Write-Host "`nCleaning CephasOps.Api..." -ForegroundColor Cyan
Set-Location "$scriptRoot\src\CephasOps.Api"
dotnet clean
if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ CephasOps.Api cleaned" -ForegroundColor Green
} else {
    Write-Host "⚠ CephasOps.Api clean had issues" -ForegroundColor Yellow
}

# Clean Infrastructure project
Write-Host "`nCleaning CephasOps.Infrastructure..." -ForegroundColor Cyan
Set-Location "$scriptRoot\src\CephasOps.Infrastructure"
dotnet clean
if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ CephasOps.Infrastructure cleaned" -ForegroundColor Green
} else {
    Write-Host "⚠ CephasOps.Infrastructure clean had issues" -ForegroundColor Yellow
}

# Clean Application project
Write-Host "`nCleaning CephasOps.Application..." -ForegroundColor Cyan
Set-Location "$scriptRoot\src\CephasOps.Application"
dotnet clean
if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ CephasOps.Application cleaned" -ForegroundColor Green
} else {
    Write-Host "⚠ CephasOps.Application clean had issues" -ForegroundColor Yellow
}

# Clean Domain project
Write-Host "`nCleaning CephasOps.Domain..." -ForegroundColor Cyan
Set-Location "$scriptRoot\src\CephasOps.Domain"
dotnet clean
if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ CephasOps.Domain cleaned" -ForegroundColor Green
} else {
    Write-Host "⚠ CephasOps.Domain clean had issues" -ForegroundColor Yellow
}

Write-Host "`n✅ Clean completed!" -ForegroundColor Green
Write-Host "All bin/ and obj/ folders have been removed." -ForegroundColor Cyan

