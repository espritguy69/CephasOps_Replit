# Start Both Backend and Frontend
# This script launches both services in separate windows

Write-Host "🚀 Starting CephasOps - Backend + Frontend" -ForegroundColor Cyan
Write-Host ""

$projectRoot = "C:\Projects\CephasOps"

# Start Backend in new window
Write-Host "🔵 Starting Backend in new window..." -ForegroundColor Blue
Start-Process powershell -ArgumentList "-NoExit", "-File", "$projectRoot\start-backend.ps1"

# Wait 2 seconds
Start-Sleep -Seconds 2

# Start Frontend in new window
Write-Host "🟢 Starting Frontend in new window..." -ForegroundColor Green
Start-Process powershell -ArgumentList "-NoExit", "-File", "$projectRoot\start-frontend.ps1"

Write-Host ""
Write-Host "✅ Both services starting in separate windows!" -ForegroundColor Green
Write-Host ""
Write-Host "📋 Next Steps:" -ForegroundColor Yellow
Write-Host "  1. Wait for backend: 'Now listening on: http://localhost:5000'" -ForegroundColor White
Write-Host "  2. Wait for frontend: 'Local: http://localhost:5173/'" -ForegroundColor White
Write-Host "  3. Open browser: http://localhost:5173" -ForegroundColor White
Write-Host "  4. Login: simon@cephas.com.my / J@saw007" -ForegroundColor White
Write-Host ""
Write-Host "🧪 Testing Guide: See 🎯_ENHANCED_TESTING_REPORT.md" -ForegroundColor Cyan
Write-Host ""
Write-Host "Press any key to close this window..." -ForegroundColor Gray
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")

