# ============================================
# Restart All CephasOps Services
# ============================================
# This script restarts all CephasOps services:
# 1. Stops all running services
# 2. Waits for services to fully stop
# 3. Starts all services again
# ============================================

param(
    [switch]$SkipBackend,
    [switch]$SkipFrontend,
    [int]$WaitSeconds = 3
)

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "Restarting CephasOps Services..." -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path

# Step 1: Stop all services
Write-Host "Step 1: Stopping all services..." -ForegroundColor Yellow
Write-Host ""
& (Join-Path $scriptPath "stop-services.ps1")

Write-Host ""
Write-Host "Waiting $WaitSeconds seconds before starting services..." -ForegroundColor Gray
Start-Sleep -Seconds $WaitSeconds
Write-Host ""

# Step 2: Start all services
Write-Host "Step 2: Starting all services..." -ForegroundColor Yellow
Write-Host ""

$startScript = Join-Path $scriptPath "start-services.ps1"
$startParams = @{ Wait = $true }

if ($SkipBackend) {
    $startParams['SkipBackend'] = $true
}

if ($SkipFrontend) {
    $startParams['SkipFrontend'] = $true
}

& $startScript @startParams

Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "Service restart completed!" -ForegroundColor Green
Write-Host "============================================" -ForegroundColor Cyan

