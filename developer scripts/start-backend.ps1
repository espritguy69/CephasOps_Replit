# ================================
# CephasOps Backend Start Script
# ================================

Write-Host "🔄 Restoring dependencies..."
dotnet restore .\src\CephasOps.Api

Write-Host "🔍 Checking for pending EF migrations..."
dotnet ef database update --project .\src\CephasOps.Infrastructure --startup-project .\src\CephasOps.Api

Write-Host "🚀 Starting CephasOps Backend API..."
dotnet run --project .\src\CephasOps.Api --urls "http://localhost:5000"

Write-Host "✔ Backend running at http://localhost:5000"
