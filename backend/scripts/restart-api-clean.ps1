# Clean restart of the API
# This script ensures port 5000 is free and starts the API

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Clean API Restart" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Kill any process using port 5000
Write-Host "Step 1: Freeing port 5000..." -ForegroundColor Yellow
$connection = Get-NetTCPConnection -LocalPort 5000 -ErrorAction SilentlyContinue
if ($connection) {
    $processId = $connection.OwningProcess
    $process = Get-Process -Id $processId -ErrorAction SilentlyContinue
    if ($process) {
        Write-Host "  Stopping process $($process.ProcessName) (PID: $processId)..." -ForegroundColor Yellow
        Stop-Process -Id $processId -Force -ErrorAction SilentlyContinue
        Start-Sleep -Seconds 2
    }
}

# Step 2: Kill any CephasOps.Api processes
Write-Host "Step 2: Stopping CephasOps.Api processes..." -ForegroundColor Yellow
$cephasProcesses = Get-Process -Name "CephasOps.Api" -ErrorAction SilentlyContinue
if ($cephasProcesses) {
    foreach ($proc in $cephasProcesses) {
        Write-Host "  Stopping process (PID: $($proc.Id))..." -ForegroundColor Yellow
        Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
    }
    Start-Sleep -Seconds 2
}

# Step 3: Kill any dotnet processes that might be holding the port
Write-Host "Step 3: Checking for dotnet processes..." -ForegroundColor Yellow
$dotnetProcesses = Get-Process -Name "dotnet" -ErrorAction SilentlyContinue | Where-Object {
    try {
        $cmdLine = (Get-CimInstance Win32_Process -Filter "ProcessId = $($_.Id)").CommandLine
        $cmdLine -like "*CephasOps.Api*" -or $cmdLine -like "*--urls*5000*"
    } catch {
        $false
    }
}

if ($dotnetProcesses) {
    foreach ($proc in $dotnetProcesses) {
        Write-Host "  Stopping dotnet process (PID: $($proc.Id))..." -ForegroundColor Yellow
        Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
    }
    Start-Sleep -Seconds 3
}

# Step 4: Verify port is free
Write-Host "Step 4: Verifying port 5000 is free..." -ForegroundColor Yellow
$stillInUse = Get-NetTCPConnection -LocalPort 5000 -ErrorAction SilentlyContinue
if ($stillInUse) {
    Write-Host "  ⚠️  Port 5000 is still in use. Trying one more time..." -ForegroundColor Yellow
    $processId = $stillInUse.OwningProcess
    Stop-Process -Id $processId -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 3
}

$finalCheck = Get-NetTCPConnection -LocalPort 5000 -ErrorAction SilentlyContinue
if ($finalCheck) {
    Write-Host "  ❌ Port 5000 is still in use by process $($finalCheck.OwningProcess)" -ForegroundColor Red
    Write-Host "  Please manually stop the process or use a different port." -ForegroundColor Yellow
    exit 1
} else {
    Write-Host "  ✅ Port 5000 is free" -ForegroundColor Green
}

# Step 5: Start the API
Write-Host ""
Write-Host "Step 5: Starting API..." -ForegroundColor Yellow
Write-Host ""

$apiPath = Join-Path $PSScriptRoot "..\src\CephasOps.Api"
if (-not (Test-Path $apiPath)) {
    Write-Host "❌ API project not found at: $apiPath" -ForegroundColor Red
    exit 1
}

Write-Host "Starting API at: $apiPath" -ForegroundColor Cyan
Write-Host ""

# Change to API directory and start
Set-Location $apiPath
dotnet run

