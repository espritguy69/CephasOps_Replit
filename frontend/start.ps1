# ============================================
# CephasOps Frontend (Admin) - Start Service
# ============================================
# Run this script from the frontend folder
# Usage: .\start.ps1 [--port 5173]

param(
    [int]$Port = 5173
)

$ErrorActionPreference = "Stop"
$scriptRoot = $PSScriptRoot

Write-Host "🚀 Starting CephasOps Frontend (Admin)..." -ForegroundColor Cyan
Write-Host "Location: $scriptRoot" -ForegroundColor Gray
Write-Host "Port: $Port" -ForegroundColor Gray

# Check if Node.js is installed
$nodeVersion = node --version 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Node.js not found. Please install Node.js." -ForegroundColor Red
    exit 1
}
Write-Host "✓ Node.js: $nodeVersion" -ForegroundColor Green

# Check if npm is installed
$npmVersion = npm --version 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ npm not found. Please install npm." -ForegroundColor Red
    exit 1
}
Write-Host "✓ npm: $npmVersion" -ForegroundColor Green

# Check if node_modules exists, if not install dependencies
if (-not (Test-Path "$scriptRoot\node_modules")) {
    Write-Host "`n📦 Installing dependencies (first time setup)..." -ForegroundColor Yellow
    npm install
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Failed to install dependencies" -ForegroundColor Red
        exit 1
    }
    Write-Host "✓ Dependencies installed" -ForegroundColor Green
}

# Start the dev server
Write-Host "`n🚀 Starting Vite dev server..." -ForegroundColor Cyan

$env:PORT = $Port
npm run dev -- --port $Port

Write-Host "`n✅ Frontend running at: http://localhost:$Port" -ForegroundColor Green

