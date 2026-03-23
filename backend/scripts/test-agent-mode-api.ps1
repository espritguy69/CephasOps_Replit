# Test script for Agent Mode API endpoints
# Requires: Backend running, valid JWT token

param(
    [Parameter(Mandatory=$true)]
    [string]$BaseUrl = "http://localhost:5000",
    
    [Parameter(Mandatory=$true)]
    [string]$Token
)

$headers = @{
    "Authorization" = "Bearer $Token"
    "Content-Type" = "application/json"
}

Write-Host "`n=== Testing Agent Mode API ===" -ForegroundColor Cyan
Write-Host "Base URL: $BaseUrl`n" -ForegroundColor Gray

# Test 1: Calculate KPIs
Write-Host "Test 1: Calculate Smart KPIs" -ForegroundColor Yellow
try {
    $fromDate = (Get-Date).AddDays(-30).ToString("yyyy-MM-dd")
    $toDate = (Get-Date).ToString("yyyy-MM-dd")
    $response = Invoke-RestMethod -Uri "$BaseUrl/api/agent/calculate-kpis?fromDate=$fromDate&toDate=$toDate" -Method Get -Headers $headers
    Write-Host "✓ Success: KPIs calculated" -ForegroundColor Green
    Write-Host "  Total Orders: $($response.metrics.TotalOrders)" -ForegroundColor Gray
    Write-Host "  Completion Rate: $($response.metrics.CompletionRate)%" -ForegroundColor Gray
    Write-Host "  Reschedule Count: $($response.metrics.RescheduleCount)" -ForegroundColor Gray
} catch {
    Write-Host "✗ Failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 2: Process Pending Tasks
Write-Host "`nTest 2: Process Pending Tasks" -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "$BaseUrl/api/agent/process-pending-tasks" -Method Post -Headers $headers
    Write-Host "✓ Success: Processed $($response.Count) tasks" -ForegroundColor Green
    $response | ForEach-Object {
        Write-Host "  - $($_.Action): $($_.Success)" -ForegroundColor Gray
    }
} catch {
    Write-Host "✗ Failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 3: Get Order Statuses (Verify Reinvoice)
Write-Host "`nTest 3: Verify Reinvoice Status" -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "$BaseUrl/api/order-statuses" -Method Get -Headers $headers
    $reinvoiceStatus = $response | Where-Object { $_.Code -eq "Reinvoice" }
    if ($reinvoiceStatus) {
        Write-Host "✓ Success: Reinvoice status found" -ForegroundColor Green
        Write-Host "  Code: $($reinvoiceStatus.Code)" -ForegroundColor Gray
        Write-Host "  Name: $($reinvoiceStatus.Name)" -ForegroundColor Gray
        Write-Host "  Order: $($reinvoiceStatus.Order)" -ForegroundColor Gray
    } else {
        Write-Host "✗ Warning: Reinvoice status not found in list" -ForegroundColor Yellow
    }
} catch {
    Write-Host "✗ Failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n=== Agent Mode API Tests Complete ===" -ForegroundColor Cyan

