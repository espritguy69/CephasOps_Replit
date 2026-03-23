# ============================================
# Check Email Services Status
# ============================================

Write-Host "🔍 Checking CephasOps Email Services Status..." -ForegroundColor Cyan
Write-Host ""

# Check if backend is running
Write-Host "1. Checking Backend API..." -ForegroundColor Yellow
try {
    $healthResponse = Invoke-RestMethod -Uri "http://localhost:5000/api/admin/health" -Method Get -ErrorAction Stop
    Write-Host "   ✅ Backend API is running" -ForegroundColor Green
    Write-Host "   Status: $($healthResponse.isHealthy)" -ForegroundColor Gray
} catch {
    Write-Host "   ❌ Backend API is not responding" -ForegroundColor Red
    Write-Host "   Error: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "2. Checking Database Connection..." -ForegroundColor Yellow
try {
    $diagnosticResponse = Invoke-RestMethod -Uri "http://localhost:5000/api/diagnostic/check-seeding" -Method Get -ErrorAction Stop
    if ($diagnosticResponse.databaseConnected) {
        Write-Host "   ✅ Database is connected" -ForegroundColor Green
    } else {
        Write-Host "   ❌ Database connection failed" -ForegroundColor Red
    }
} catch {
    Write-Host "   ⚠️  Could not check database status" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "📧 Email Parser Status:" -ForegroundColor Cyan
Write-Host ""
Write-Host "Note: To check email accounts, you need to be authenticated." -ForegroundColor Yellow
Write-Host "Available endpoints:" -ForegroundColor Yellow
Write-Host "  - GET  /api/email-accounts              (List all email accounts)" -ForegroundColor Gray
Write-Host "  - POST /api/email-accounts/{id}/poll    (Poll specific mailbox)" -ForegroundColor Gray
Write-Host "  - POST /api/email-accounts/poll-all     (Poll all active mailboxes)" -ForegroundColor Gray
Write-Host ""
Write-Host "To manually trigger email polling, use:" -ForegroundColor Cyan
Write-Host '  curl -X POST http://localhost:5000/api/email-accounts/poll-all \' -ForegroundColor White
Write-Host '       -H "Authorization: Bearer <your-jwt-token>"' -ForegroundColor White
Write-Host ""

Write-Host "✅ Service check complete!" -ForegroundColor Green

