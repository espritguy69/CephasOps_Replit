# ============================================
# CephasOps Frontend-SI (Service Installer) - Start Service
# ============================================
# Run this script from the frontend-si folder
# Usage: .\start.ps1 [--port 5174]

param(
    [int]$Port = 5174
)

$ErrorActionPreference = "Stop"
$scriptRoot = $PSScriptRoot

Write-Host "🚀 Starting CephasOps Frontend-SI (Service Installer App)..." -ForegroundColor Cyan
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

# Check if package.json exists
if (-not (Test-Path "$scriptRoot\package.json")) {
    Write-Host "`n⚠ package.json not found. Initializing React project..." -ForegroundColor Yellow
    
    # Initialize a basic React + Vite project if needed
    Write-Host "Please run: npm create vite@latest . -- --template react-ts" -ForegroundColor Cyan
    Write-Host "Or copy package.json from frontend folder and adapt it." -ForegroundColor Cyan
    exit 1
}

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

Write-Host "`n✅ Frontend-SI running at: http://localhost:$Port" -ForegroundColor Green

