# ============================================
# Stop All CephasOps Services
# ============================================

Write-Host "🛑 Stopping CephasOps services..." -ForegroundColor Yellow

# Stop backend (.NET)
$dotnetProcesses = Get-Process -Name "dotnet" -ErrorAction SilentlyContinue
if ($dotnetProcesses) {
    $dotnetProcesses | Stop-Process -Force
    Write-Host "✔ Backend stopped (dotnet processes terminated)"
} else {
    Write-Host "ℹ No backend processes running"
}

# Stop frontend (Node/Vite)
$nodeProcesses = Get-Process -Name "node" -ErrorAction SilentlyContinue
if ($nodeProcesses) {
    $nodeProcesses | Stop-Process -Force
    Write-Host "✔ Frontend stopped (node processes terminated)"
} else {
    Write-Host "ℹ No frontend processes running"
}

Write-Host "🟢 All services fully stopped."
