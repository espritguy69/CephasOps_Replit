# ============================================
# CephasOps Backend - Stop Service
# ============================================
# Run this script from the backend folder

Write-Host "🛑 Stopping CephasOps Backend..." -ForegroundColor Yellow

# Find and stop dotnet processes related to CephasOps.Api
$processes = Get-Process -Name "dotnet" -ErrorAction SilentlyContinue | Where-Object {
    $_.Path -like "*CephasOps*" -or
    (Get-WmiObject Win32_Process -Filter "ProcessId = $($_.Id)" -ErrorAction SilentlyContinue | 
     Select-Object -ExpandProperty CommandLine) -like "*CephasOps.Api*"
}

if ($processes) {
    $processes | ForEach-Object {
        Write-Host "Stopping process: $($_.Id) - $($_.ProcessName)" -ForegroundColor Yellow
        Stop-Process -Id $_.Id -Force -ErrorAction SilentlyContinue
    }
    Write-Host "✓ Backend stopped" -ForegroundColor Green
} else {
    Write-Host "ℹ No backend processes found running" -ForegroundColor Gray
}

# Also check for processes on port 5000
$port5000 = Get-NetTCPConnection -LocalPort 5000 -ErrorAction SilentlyContinue
if ($port5000) {
    $pid = $port5000.OwningProcess
    Write-Host "Found process on port 5000 (PID: $pid), stopping..." -ForegroundColor Yellow
    Stop-Process -Id $pid -Force -ErrorAction SilentlyContinue
}

Write-Host "`n✅ Backend service stopped" -ForegroundColor Green

