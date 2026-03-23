# Kill process using port 5000
# This script finds and stops the process using port 5000

$ErrorActionPreference = "Stop"

Write-Host "Finding process using port 5000..." -ForegroundColor Yellow

# Find process using port 5000
$connection = Get-NetTCPConnection -LocalPort 5000 -ErrorAction SilentlyContinue

if ($connection) {
    $processId = $connection.OwningProcess
    $process = Get-Process -Id $processId -ErrorAction SilentlyContinue
    
    if ($process) {
        Write-Host "Found process: $($process.ProcessName) (PID: $processId)" -ForegroundColor Yellow
        Write-Host "Stopping process..." -ForegroundColor Yellow
        
        try {
            Stop-Process -Id $processId -Force
            Start-Sleep -Seconds 2
            
            # Verify it's stopped
            $verify = Get-Process -Id $processId -ErrorAction SilentlyContinue
            if ($verify) {
                Write-Host "⚠️  Process still running, trying again..." -ForegroundColor Yellow
                Stop-Process -Id $processId -Force -ErrorAction Stop
                Start-Sleep -Seconds 2
            }
            
            Write-Host "✅ Process stopped successfully!" -ForegroundColor Green
        } catch {
            Write-Host "❌ Error stopping process: $_" -ForegroundColor Red
            exit 1
        }
    } else {
        Write-Host "⚠️  Process ID $processId not found (may have already stopped)" -ForegroundColor Yellow
    }
} else {
    Write-Host "✅ No process found using port 5000" -ForegroundColor Green
}

# Also check for any CephasOps.Api processes
Write-Host ""
Write-Host "Checking for CephasOps.Api processes..." -ForegroundColor Yellow
$cephasProcesses = Get-Process -Name "CephasOps.Api" -ErrorAction SilentlyContinue
if ($cephasProcesses) {
    foreach ($proc in $cephasProcesses) {
        Write-Host "Stopping CephasOps.Api process (PID: $($proc.Id))..." -ForegroundColor Yellow
        Stop-Process -Id $proc.Id -Force
    }
    Write-Host "✅ All CephasOps.Api processes stopped" -ForegroundColor Green
} else {
    Write-Host "✅ No CephasOps.Api processes found" -ForegroundColor Green
}

Write-Host ""
Write-Host "Port 5000 is now free. You can start the API." -ForegroundColor Cyan

