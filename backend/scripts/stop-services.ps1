param(
    # Root folder of your CephasOps project
    [string]$ProjectRoot = "C:\Projects\CephasOps"
)

# ============================================
# Stop All CephasOps Services
# ============================================
# This script stops all running CephasOps services:
# - Backend API (.NET)
# - Frontend Dev Server (Vite)
# ============================================

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "Stopping CephasOps Services..." -ForegroundColor Cyan
Write-Host "Project root: $ProjectRoot" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# Resolve script path (if you still want to infer relative stuff later)
$scriptPath  = Split-Path -Parent $MyInvocation.MyCommand.Path

# Function to stop processes by name pattern
function Stop-ServiceProcess {
    param (
        [string]$ProcessName,
        [string]$Description
    )
    
    $processes = Get-Process -Name $ProcessName -ErrorAction SilentlyContinue
    
    if ($processes) {
        Write-Host "  Stopping $Description..." -ForegroundColor Yellow
        foreach ($proc in $processes) {
            try {
                Stop-Process -Id $proc.Id -Force -ErrorAction Stop
                Write-Host "    ✓ Stopped process: $($proc.ProcessName) (PID: $($proc.Id))" -ForegroundColor Green
            }
            catch {
                Write-Host "    ✗ Failed to stop process: $($proc.ProcessName) (PID: $($proc.Id))" -ForegroundColor Red
                Write-Host "      Error: $($_.Exception.Message)" -ForegroundColor Red
            }
        }
    }
    else {
        Write-Host "  $Description is not running." -ForegroundColor Gray
    }
}

# Function to stop processes running on specific ports
function Stop-PortProcess {
    param (
        [int]$Port,
        [string]$Description
    )
    
    $connections = Get-NetTCPConnection -LocalPort $Port -ErrorAction SilentlyContinue | 
                   Where-Object { $_.State -eq "Listen" }
    
    if ($connections) {
        foreach ($conn in $connections) {
            $process = Get-Process -Id $conn.OwningProcess -ErrorAction SilentlyContinue
            if ($process) {
                Write-Host "  Stopping $Description on port $Port..." -ForegroundColor Yellow
                try {
                    Stop-Process -Id $process.Id -Force -ErrorAction Stop
                    Write-Host "    ✓ Stopped process: $($process.ProcessName) (PID: $($process.Id)) on port $Port" -ForegroundColor Green
                }
                catch {
                    Write-Host "    ✗ Failed to stop process on port $Port" -ForegroundColor Red
                }
            }
        }
    }
    else {
        Write-Host "  No process found on port $Port ($Description)" -ForegroundColor Gray
    }
}

# Normalize project root for matching
$normalizedProjectRoot = $ProjectRoot.TrimEnd('\')

# Stop Backend API (dotnet processes)
Write-Host "1. Checking Backend API..." -ForegroundColor Cyan
$backendProcesses = Get-Process -Name "dotnet","CephasOps.Api" -ErrorAction SilentlyContinue | Where-Object {
    # Some dotnet processes may not expose Path, so wrap in try/catch
    $match = $false
    try {
        if ($_.Path -and $_.Path -like "*$normalizedProjectRoot*") {
            $match = $true
        }
    } catch { }

    try {
        $cmd = (Get-CimInstance Win32_Process -Filter "ProcessId = $($_.Id)").CommandLine
        if ($cmd -and $cmd -like "*CephasOps.Api*" -or $cmd -like "*$normalizedProjectRoot*") {
            $match = $true
        }
    } catch { }

    $match
}

if ($backendProcesses) {
    foreach ($proc in $backendProcesses) {
        Write-Host "  Stopping Backend API (PID: $($proc.Id))..." -ForegroundColor Yellow
        try {
            Stop-Process -Id $proc.Id -Force -ErrorAction Stop
            Write-Host "    ✓ Stopped Backend API process" -ForegroundColor Green
        }
        catch {
            Write-Host "    ✗ Failed to stop Backend API" -ForegroundColor Red
        }
    }
}
else {
    Write-Host "  Backend API is not running." -ForegroundColor Gray
}

# Stop processes on backend port (default: 5000 or 5001)
Write-Host "2. Checking Backend API ports (5000, 5001)..." -ForegroundColor Cyan
Stop-PortProcess -Port 5000 -Description "Backend API (HTTP)"
Stop-PortProcess -Port 5001 -Description "Backend API (HTTPS)"

# Stop Frontend Dev Server (Node/Vite processes)
Write-Host "3. Checking Frontend Dev Server..." -ForegroundColor Cyan
$nodeProcesses = Get-Process -Name "node" -ErrorAction SilentlyContinue | Where-Object {
    $match = $false
    try {
        if ($_.Path -and $_.Path -like "*$normalizedProjectRoot*frontend*") {
            $match = $true
        }
    } catch { }

    try {
        $cmd = (Get-CimInstance Win32_Process -Filter "ProcessId = $($_.Id)").CommandLine
        if ($cmd -and ($cmd -like "*vite*" -or $cmd -like "*$normalizedProjectRoot*frontend*")) {
            $match = $true
        }
    } catch { }

    $match
}

if ($nodeProcesses) {
    foreach ($proc in $nodeProcesses) {
        Write-Host "  Stopping Frontend Dev Server (PID: $($proc.Id))..." -ForegroundColor Yellow
        try {
            Stop-Process -Id $proc.Id -Force -ErrorAction Stop
            Write-Host "    ✓ Stopped Frontend Dev Server process" -ForegroundColor Green
        }
        catch {
            Write-Host "    ✗ Failed to stop Frontend Dev Server" -ForegroundColor Red
        }
    }
}
else {
    Write-Host "  Frontend Dev Server is not running." -ForegroundColor Gray
}

# Stop processes on frontend port (default: 5173)
Write-Host "4. Checking Frontend Dev Server port (5173)..." -ForegroundColor Cyan
Stop-PortProcess -Port 5173 -Description "Frontend Dev Server"

# Stop processes on frontend preview port (default: 4173)
Write-Host "5. Checking Frontend Preview port (4173)..." -ForegroundColor Cyan
Stop-PortProcess -Port 4173 -Description "Frontend Preview"

# Wait a moment for processes to fully stop
Write-Host ""
Write-Host "Waiting 2 seconds for processes to fully terminate..." -ForegroundColor Gray
Start-Sleep -Seconds 2

Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "All services stopped successfully!" -ForegroundColor Green
Write-Host "============================================" -ForegroundColor Cyan
