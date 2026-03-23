# ============================================
# Reset (Stop + Start) All CephasOps Services
# ============================================

Write-Host "🔄 Resetting CephasOps services..." -ForegroundColor Cyan

$root = $PSScriptRoot

# Stop services
Write-Host "🛑 Stopping services..."
& "$root\stop-services.ps1"

Write-Host "⏳ Waiting for cleanup..."
Start-Sleep -Seconds 5

# Start services
Write-Host "🚀 Starting services..."
& "$root\start-services.ps1"

Write-Host "✨ Reset complete."
