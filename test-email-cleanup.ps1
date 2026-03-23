# Test Email Cleanup Endpoint
# Usage: .\test-email-cleanup.ps1

Write-Host "🧹 Testing Email Cleanup Endpoint" -ForegroundColor Cyan
Write-Host ""

# Check if backend is running
Write-Host "Checking if backend is running..." -ForegroundColor Yellow
try {
    $healthCheck = Invoke-WebRequest -Uri "http://localhost:5000/api/admin/health" -Method GET -TimeoutSec 2 -ErrorAction Stop
    Write-Host "✅ Backend is running" -ForegroundColor Green
} catch {
    Write-Host "❌ Backend is not running on port 5000" -ForegroundColor Red
    Write-Host "Please start the backend first using: .\start-backend.ps1" -ForegroundColor Yellow
    exit 1
}

Write-Host ""

# Get auth token (if available)
$headers = @{}
if (Test-Path ".\token.txt") {
    $token = (Get-Content ".\token.txt" -Raw).Trim()
    $headers["Authorization"] = "Bearer $token"
    Write-Host "✅ Using authentication token" -ForegroundColor Green
} else {
    Write-Host "⚠️  No token file found. Testing without authentication..." -ForegroundColor Yellow
    Write-Host "   (You may need to login first to get a token)" -ForegroundColor Gray
}

Write-Host ""
Write-Host "Calling cleanup endpoint: POST /api/email-accounts/cleanup" -ForegroundColor Cyan
Write-Host ""

try {
    $response = Invoke-RestMethod -Uri "http://localhost:5000/api/email-accounts/cleanup" `
        -Method POST `
        -Headers $headers `
        -ContentType "application/json" `
        -ErrorAction Stop

    Write-Host "✅ Cleanup job completed successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Results:" -ForegroundColor Cyan
    Write-Host "  - Deleted Emails: $($response.data.DeletedEmails)" -ForegroundColor White
    Write-Host "  - Deleted Attachments: $($response.data.DeletedAttachments)" -ForegroundColor White
    Write-Host "  - Deleted Blobs: $($response.data.DeletedBlobs)" -ForegroundColor White
    Write-Host "  - Success: $($response.data.Success)" -ForegroundColor White
    
    if ($response.message) {
        Write-Host ""
        Write-Host "Message: $($response.message)" -ForegroundColor Yellow
    }

    if ($response.data.DeletedEmails -eq 0) {
        Write-Host ""
        Write-Host "ℹ️  No expired emails found to clean up." -ForegroundColor Cyan
    }

} catch {
    Write-Host "❌ Error calling cleanup endpoint" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    
    if ($_.Exception.Response) {
        $statusCode = $_.Exception.Response.StatusCode.value__
        Write-Host "Status Code: $statusCode" -ForegroundColor Yellow
        
        if ($statusCode -eq 401) {
            Write-Host ""
            Write-Host "⚠️  Authentication required." -ForegroundColor Yellow
            Write-Host "Please login first and save your token to token.txt" -ForegroundColor Yellow
        } elseif ($statusCode -eq 405) {
            Write-Host ""
            Write-Host "⚠️  Method not allowed. The endpoint may not be registered yet." -ForegroundColor Yellow
            Write-Host "Try restarting the backend to pick up the new endpoint." -ForegroundColor Yellow
        } elseif ($statusCode -eq 500) {
            Write-Host ""
            Write-Host "⚠️  Server error. Check backend logs for details." -ForegroundColor Yellow
            Write-Host "The DeletedByUserId column error should be resolved if migrations are applied." -ForegroundColor Yellow
        }
    }
}

Write-Host ""

