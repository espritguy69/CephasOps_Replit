# quick-db-sync.ps1 - Quick database-only sync
# Use this when only database migrations changed

Write-Host "🗄️  Quick Database Sync" -ForegroundColor Cyan
Write-Host "========================`n" -ForegroundColor Cyan

$originalLocation = Get-Location

try {
    Set-Location backend
    
    Write-Host "Applying database migrations..." -ForegroundColor Yellow
    dotnet ef database update --project src/CephasOps.Infrastructure --startup-project src/CephasOps.Api
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "`n✅ Database synced successfully!" -ForegroundColor Green
        Write-Host "If backend is running with 'dotnet watch', it will auto-reload.`n" -ForegroundColor White
    } else {
        Write-Host "`n❌ Database sync failed!" -ForegroundColor Red
        Write-Host "Check PostgreSQL service and connection string.`n" -ForegroundColor Yellow
        exit 1
    }
} finally {
    Set-Location $originalLocation
}

