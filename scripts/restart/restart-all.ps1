# Restart both backend and frontend with hot reload
# Usage: .\scripts\restart\restart-all.ps1

Write-Host "🔄 Restarting all services with hot reload..." -ForegroundColor Cyan

# Stop backend
Write-Host "Stopping backend on port 5000..." -ForegroundColor Yellow
Get-NetTCPConnection -LocalPort 5000 -ErrorAction SilentlyContinue | 
  Select-Object -ExpandProperty OwningProcess | 
  ForEach-Object { 
    Stop-Process -Id $_ -Force -ErrorAction SilentlyContinue 
  }

# Stop frontend
Write-Host "Stopping frontend on port 5173..." -ForegroundColor Yellow
Get-NetTCPConnection -LocalPort 5173 -ErrorAction SilentlyContinue | 
  Select-Object -ExpandProperty OwningProcess | 
  ForEach-Object { 
    Stop-Process -Id $_ -Force -ErrorAction SilentlyContinue 
  }

Start-Sleep -Seconds 2

Write-Host "✅ Starting services in watch mode..." -ForegroundColor Green

# Start backend in new window with watch mode
Write-Host "Starting backend with hot reload..." -ForegroundColor Cyan
Start-Process powershell -ArgumentList @(
  "-NoExit",
  "-Command",
  "Write-Host '🔥 Backend API - Hot Reload Enabled' -ForegroundColor Green; cd '$PSScriptRoot\..\..\backend\src\CephasOps.Api'; dotnet watch run"
)

Start-Sleep -Seconds 5

# Start frontend in new window
Write-Host "Starting frontend with Vite HMR..." -ForegroundColor Cyan
Start-Process powershell -ArgumentList @(
  "-NoExit", 
  "-Command",
  "Write-Host '⚡ Frontend - Vite HMR Enabled' -ForegroundColor Green; cd '$PSScriptRoot\..\..\frontend'; npm run dev"
)

Write-Host ""
Write-Host "✅ Services started in separate windows!" -ForegroundColor Green
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Gray
Write-Host "📍 Backend:  http://localhost:5000" -ForegroundColor Cyan
Write-Host "📍 Frontend: http://localhost:5173" -ForegroundColor Cyan
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Gray
Write-Host "🔥 Hot reload is enabled - code changes auto-reload!" -ForegroundColor Yellow
Write-Host "⚡ No manual restarts needed for code changes!" -ForegroundColor Yellow
Write-Host ""
