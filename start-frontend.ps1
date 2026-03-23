# Start CephasOps Frontend
# This script starts the React frontend with Vite

Write-Host "🚀 Starting CephasOps Frontend..." -ForegroundColor Cyan
Write-Host ""

# Navigate to frontend project
Set-Location "C:\Projects\CephasOps\frontend"

# Start Vite dev server
Write-Host "📂 Location: C:\Projects\CephasOps\frontend" -ForegroundColor Yellow
Write-Host "⚡ Vite dev server starting..." -ForegroundColor Green
Write-Host ""

npm run dev

# Keep window open if there's an error
if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "❌ Frontend failed to start. Press any key to close..." -ForegroundColor Red
    $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
}

