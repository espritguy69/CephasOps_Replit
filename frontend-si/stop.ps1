# ============================================
# CephasOps Frontend-SI (Service Installer) - Stop Service
# ============================================
# Run this script from the frontend-si folder

Write-Host "🛑 Stopping CephasOps Frontend-SI..." -ForegroundColor Yellow

# Find and stop Node processes related to Vite/frontend-si
$processes = Get-Process -Name "node" -ErrorAction SilentlyContinue | Where-Object {
    $cmdLine = (Get-WmiObject Win32_Process -Filter "ProcessId = $($_.Id)" -ErrorAction SilentlyContinue | 
                Select-Object -ExpandProperty CommandLine)
    $cmdLine -like "*vite*" -or 
    $cmdLine -like "*frontend-si*" -or
    $_.Path -like "*frontend-si*"
}

if ($processes) {
    $processes | ForEach-Object {
        Write-Host "Stopping process: $($_.Id) - $($_.ProcessName)" -ForegroundColor Yellow
        Stop-Process -Id $_.Id -Force -ErrorAction SilentlyContinue
    }
    Write-Host "✓ Frontend-SI stopped" -ForegroundColor Green
} else {
    Write-Host "ℹ No frontend-si processes found running" -ForegroundColor Gray
}

# Also check for processes on port 5174
$port5174 = Get-NetTCPConnection -LocalPort 5174 -ErrorAction SilentlyContinue
if ($port5174) {
    $pid = $port5174.OwningProcess
    Write-Host "Found process on port 5174 (PID: $pid), stopping..." -ForegroundColor Yellow
    Stop-Process -Id $pid -Force -ErrorAction SilentlyContinue
}

Write-Host "`n✅ Frontend-SI service stopped" -ForegroundColor Green

