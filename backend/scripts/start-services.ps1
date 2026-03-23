# ============================================
# Start All CephasOps Services
# ============================================
# This script starts all CephasOps services:
# - Backend API (.NET)
# - Frontend Dev Server (Vite)
# ============================================

param(
    [switch]$SkipBackend,
    [switch]$SkipFrontend,
    [switch]$Wait
)

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "Starting CephasOps Services..." -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent (Split-Path -Parent $scriptPath)
$backendPath = Join-Path $projectRoot "backend\src\CephasOps.Api"
$frontendPath = Join-Path $projectRoot "frontend"

# Function to check if a port is in use
function Test-Port {
    param([int]$Port)
    $connection = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue
    return $null -ne $connection
}

# Function to wait for service to be ready
function Wait-ForService {
    param(
        [int]$Port,
        [string]$ServiceName,
        [int]$TimeoutSeconds = 30
    )
    
    $elapsed = 0
    Write-Host "  Waiting for $ServiceName to be ready..." -ForegroundColor Yellow
    
    while ($elapsed -lt $TimeoutSeconds) {
        if (Test-Port -Port $Port) {
            Write-Host "    ✓ $ServiceName is ready on port $Port" -ForegroundColor Green
            return $true
        }
        Start-Sleep -Seconds 1
        $elapsed++
        Write-Host "    ." -NoNewline -ForegroundColor Gray
    }
    
    Write-Host ""
    Write-Host "    ⚠ $ServiceName did not become ready within $TimeoutSeconds seconds" -ForegroundColor Yellow
    return $false
}

# Start Backend API
if (-not $SkipBackend) {
    Write-Host "1. Starting Backend API..." -ForegroundColor Cyan
    
    if (-not (Test-Path $backendPath)) {
        Write-Host "  ✗ Backend path not found: $backendPath" -ForegroundColor Red
    }
    elseif (Test-Port -Port 5000) {
        Write-Host "  ⚠ Port 5000 is already in use. Backend API may already be running." -ForegroundColor Yellow
    }
    else {
        try {
            Write-Host "  Starting Backend API in new window..." -ForegroundColor Yellow
            
            # Start dotnet run in a new PowerShell window
            $backendScript = @"
Set-Location '$backendPath'
Write-Host 'Starting CephasOps Backend API...' -ForegroundColor Cyan
dotnet run
Write-Host 'Backend API stopped. Press any key to close...' -ForegroundColor Yellow
`$null = `$Host.UI.RawUI.ReadKey('NoEcho,IncludeKeyDown')
"@
            
            $backendScriptPath = Join-Path $env:TEMP "cephasops-backend-start.ps1"
            $backendScript | Out-File -FilePath $backendScriptPath -Encoding UTF8
            
            Start-Process powershell -ArgumentList "-NoExit", "-File", "`"$backendScriptPath`"" -WindowStyle Normal
            
            Write-Host "  ✓ Backend API started in new window" -ForegroundColor Green
            
            if ($Wait) {
                Wait-ForService -Port 5000 -ServiceName "Backend API"
            }
            else {
                Write-Host "  ⚠ Backend API is starting. Check http://localhost:5000/swagger when ready." -ForegroundColor Yellow
            }
        }
        catch {
            Write-Host "  ✗ Failed to start Backend API" -ForegroundColor Red
            Write-Host "    Error: $($_.Exception.Message)" -ForegroundColor Red
        }
    }
}
else {
    Write-Host "1. Skipping Backend API (--SkipBackend flag set)" -ForegroundColor Gray
}

Write-Host ""

# Start Frontend Dev Server
if (-not $SkipFrontend) {
    Write-Host "2. Starting Frontend Dev Server..." -ForegroundColor Cyan
    
    if (-not (Test-Path $frontendPath)) {
        Write-Host "  ✗ Frontend path not found: $frontendPath" -ForegroundColor Red
    }
    elseif (Test-Port -Port 5173) {
        Write-Host "  ⚠ Port 5173 is already in use. Frontend Dev Server may already be running." -ForegroundColor Yellow
    }
    else {
        try {
            Write-Host "  Starting Frontend Dev Server in new window..." -ForegroundColor Yellow
            
            # Start npm run dev in a new PowerShell window
            $frontendScript = @"
Set-Location '$frontendPath'
Write-Host 'Starting CephasOps Frontend Dev Server...' -ForegroundColor Cyan
npm run dev
Write-Host 'Frontend Dev Server stopped. Press any key to close...' -ForegroundColor Yellow
`$null = `$Host.UI.RawUI.ReadKey('NoEcho,IncludeKeyDown')
"@
            
            $frontendScriptPath = Join-Path $env:TEMP "cephasops-frontend-start.ps1"
            $frontendScript | Out-File -FilePath $frontendScriptPath -Encoding UTF8
            
            Start-Process powershell -ArgumentList "-NoExit", "-File", "`"$frontendScriptPath`"" -WindowStyle Normal
            
            Write-Host "  ✓ Frontend Dev Server started in new window" -ForegroundColor Green
            
            if ($Wait) {
                Wait-ForService -Port 5173 -ServiceName "Frontend Dev Server"
            }
            else {
                Write-Host "  ⚠ Frontend Dev Server is starting. Check http://localhost:5173 when ready." -ForegroundColor Yellow
            }
        }
        catch {
            Write-Host "  ✗ Failed to start Frontend Dev Server" -ForegroundColor Red
            Write-Host "    Error: $($_.Exception.Message)" -ForegroundColor Red
        }
    }
}
else {
    Write-Host "2. Skipping Frontend Dev Server (--SkipFrontend flag set)" -ForegroundColor Gray
}

Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "Services startup initiated!" -ForegroundColor Green
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Note: Services are running in separate PowerShell windows." -ForegroundColor Yellow
Write-Host "You can view logs directly in those windows." -ForegroundColor Yellow
Write-Host "To stop services, close the windows or run: .\stop-services.ps1" -ForegroundColor Yellow

