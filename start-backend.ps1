# Start CephasOps Backend API
# This script starts the backend with hot reload

Write-Host "🚀 Starting CephasOps Backend API..." -ForegroundColor Cyan
Write-Host ""

# Navigate to API project
Set-Location "C:\Projects\CephasOps\backend\src\CephasOps.Api"

# Start with dotnet watch for hot reload
Write-Host "📂 Location: C:\Projects\CephasOps\backend\src\CephasOps.Api" -ForegroundColor Yellow
Write-Host "🔥 Hot reload enabled" -ForegroundColor Green
Write-Host ""

dotnet watch run

# Keep window open if there's an error
if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "❌ Backend failed to start. Press any key to close..." -ForegroundColor Red
    $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
}

