# ============================================
# CephasOps Frontend (Admin) - Stop Service
# ============================================
# Run this script from the frontend folder

Write-Host "🛑 Stopping CephasOps Frontend..." -ForegroundColor Yellow

# Find and stop Node processes related to Vite/frontend
$processes = Get-Process -Name "node" -ErrorAction SilentlyContinue | Where-Object {
    $cmdLine = (Get-WmiObject Win32_Process -Filter "ProcessId = $($_.Id)" -ErrorAction SilentlyContinue | 
                Select-Object -ExpandProperty CommandLine)
    $cmdLine -like "*vite*" -or 
    $cmdLine -like "*frontend*" -or
    $_.Path -like "*frontend*"
}

if ($processes) {
    $processes | ForEach-Object {
        Write-Host "Stopping process: $($_.Id) - $($_.ProcessName)" -ForegroundColor Yellow
        Stop-Process -Id $_.Id -Force -ErrorAction SilentlyContinue
    }
    Write-Host "✓ Frontend stopped" -ForegroundColor Green
} else {
    Write-Host "ℹ No frontend processes found running" -ForegroundColor Gray
}

# Also check for processes on port 5173
$port5173 = Get-NetTCPConnection -LocalPort 5173 -ErrorAction SilentlyContinue
if ($port5173) {
    $pid = $port5173.OwningProcess
    Write-Host "Found process on port 5173 (PID: $pid), stopping..." -ForegroundColor Yellow
    Stop-Process -Id $pid -Force -ErrorAction SilentlyContinue
}

Write-Host "`n✅ Frontend service stopped" -ForegroundColor Green

