# Quick frontend restart script
# Usage: .\restart-frontend.ps1

Write-Host "🔄 Restarting frontend..." -ForegroundColor Cyan

# Kill existing frontend process on port 5173
Write-Host "Stopping existing frontend..." -ForegroundColor Yellow
Get-NetTCPConnection -LocalPort 5173 -ErrorAction SilentlyContinue | 
  Select-Object -ExpandProperty OwningProcess | 
  ForEach-Object { 
    Stop-Process -Id $_ -Force -ErrorAction SilentlyContinue 
  }

Start-Sleep -Seconds 2

# Navigate to frontend folder
Set-Location "$PSScriptRoot\frontend"

Write-Host "✅ Starting frontend with Vite HMR..." -ForegroundColor Green
Write-Host "Frontend will auto-reload on code changes!" -ForegroundColor Cyan
Write-Host "Press Ctrl+C to stop." -ForegroundColor Gray

# Start Vite dev server (has built-in HMR)
npm run dev

