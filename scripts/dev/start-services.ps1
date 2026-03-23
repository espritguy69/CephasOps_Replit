# ============================================
# Start All CephasOps Services
# ============================================

Write-Host "🚀 Starting CephasOps services..." -ForegroundColor Cyan

$root = $PSScriptRoot

# Start Backend
Write-Host "➡ Starting Backend API..."
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$root\backend'; .\start-backend.ps1"

Start-Sleep -Seconds 2

# Start Frontend
Write-Host "➡ Starting Frontend (Vite)..."
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$root\frontend'; npm run dev"

Write-Host "✅ All services started."
Write-Host "Backend: http://localhost:5000"
Write-Host "Frontend: http://localhost:5173"
