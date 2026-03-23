# Quick backend restart script with watch mode
# Usage: .\restart-backend.ps1

Write-Host "🔄 Restarting backend with hot reload..." -ForegroundColor Cyan

# Kill existing backend process on port 5000
Write-Host "Stopping existing backend..." -ForegroundColor Yellow
Get-NetTCPConnection -LocalPort 5000 -ErrorAction SilentlyContinue | 
  Select-Object -ExpandProperty OwningProcess | 
  ForEach-Object { 
    Stop-Process -Id $_ -Force -ErrorAction SilentlyContinue 
  }

Start-Sleep -Seconds 2

# Navigate to backend folder (go up two levels from scripts/restart to project root)
Set-Location "$PSScriptRoot\..\..\backend\src\CephasOps.Api"

Write-Host "✅ Starting backend with dotnet watch (hot reload enabled)..." -ForegroundColor Green
Write-Host "Backend will auto-reload on code changes!" -ForegroundColor Cyan
Write-Host "Press Ctrl+C to stop." -ForegroundColor Gray

# Start with watch mode for hot reload
dotnet watch run

