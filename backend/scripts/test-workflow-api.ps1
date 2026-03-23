# PowerShell script to test WorkflowDefinitions API endpoint
# Usage: .\test-workflow-api.ps1

param(
    [string]$ApiBaseUrl = "http://localhost:5000",
    [string]$AuthToken = ""
)

$ErrorActionPreference = "Stop"

Write-Host "===========================================" -ForegroundColor Cyan
Write-Host "WorkflowDefinitions API Test" -ForegroundColor Cyan
Write-Host "===========================================" -ForegroundColor Cyan
Write-Host ""

# Check if API is running
Write-Host "Checking if API is running on port 5000..." -ForegroundColor Yellow
$portCheck = netstat -ano | findstr ":5000" | Select-Object -First 1
if ($portCheck) {
    Write-Host "✓ API appears to be running (port 5000 is in use)" -ForegroundColor Green
} else {
    Write-Host "⚠ Port 5000 is not in use - API may not be running" -ForegroundColor Yellow
    Write-Host "  Please start the API first:" -ForegroundColor Yellow
    Write-Host "    cd backend\src\CephasOps.Api" -ForegroundColor White
    Write-Host "    dotnet run" -ForegroundColor White
}

Write-Host ""

# If no token provided, try to get from localStorage (if running from browser context)
if ([string]::IsNullOrEmpty($AuthToken)) {
    Write-Host "⚠ No auth token provided. Testing without authentication..." -ForegroundColor Yellow
    Write-Host "  (This will likely fail with 401 Unauthorized)" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "To test with authentication, get the token from browser localStorage and run:" -ForegroundColor Yellow
    Write-Host "  .\test-workflow-api.ps1 -AuthToken 'your-token-here'" -ForegroundColor White
    Write-Host ""
}

# Test 1: Get all workflow definitions
Write-Host "Test 1: GET /api/workflow-definitions (no filters)" -ForegroundColor Cyan
Write-Host "---------------------------------------------------" -ForegroundColor Gray

try {
    $headers = @{
        "Content-Type" = "application/json"
    }
    
    if (-not [string]::IsNullOrEmpty($AuthToken)) {
        $headers["Authorization"] = "Bearer $AuthToken"
    }
    
    $response = Invoke-RestMethod -Uri "$ApiBaseUrl/api/workflow-definitions" -Method GET -Headers $headers -ErrorAction Stop
    
    Write-Host "✓ Request successful" -ForegroundColor Green
    Write-Host "  Status: 200 OK" -ForegroundColor Green
    Write-Host "  Response type: $($response.GetType().Name)" -ForegroundColor Gray
    
    if ($response -is [Array]) {
        Write-Host "  Workflows found: $($response.Count)" -ForegroundColor Cyan
        if ($response.Count -gt 0) {
            Write-Host ""
            Write-Host "  Workflows:" -ForegroundColor Yellow
            foreach ($wf in $response) {
                Write-Host "    - $($wf.name) (ID: $($wf.id), Type: $($wf.entityType), Active: $($wf.isActive))" -ForegroundColor White
            }
        } else {
            Write-Host "  ⚠ No workflows returned" -ForegroundColor Yellow
        }
    } elseif ($response -is [PSCustomObject]) {
        if ($response.data) {
            Write-Host "  Workflows found: $($response.data.Count)" -ForegroundColor Cyan
            if ($response.data.Count -gt 0) {
                Write-Host ""
                Write-Host "  Workflows:" -ForegroundColor Yellow
                foreach ($wf in $response.data) {
                    Write-Host "    - $($wf.name) (ID: $($wf.id), Type: $($wf.entityType), Active: $($wf.isActive))" -ForegroundColor White
                }
            }
        } else {
            Write-Host "  Response: $($response | ConvertTo-Json -Depth 3)" -ForegroundColor Gray
        }
    } else {
        Write-Host "  Response: $($response | ConvertTo-Json -Depth 3)" -ForegroundColor Gray
    }
    
} catch {
    $statusCode = $_.Exception.Response.StatusCode.value__
    $errorMessage = $_.Exception.Message
    
    Write-Host "✗ Request failed" -ForegroundColor Red
    Write-Host "  Status: $statusCode" -ForegroundColor Red
    
    if ($statusCode -eq 401) {
        Write-Host "  Error: Unauthorized - Authentication required" -ForegroundColor Yellow
        Write-Host "  Please provide an auth token:" -ForegroundColor Yellow
        Write-Host "    .\test-workflow-api.ps1 -AuthToken 'your-token-here'" -ForegroundColor White
    } else {
        Write-Host "  Error: $errorMessage" -ForegroundColor Red
        try {
            $errorResponse = $_.ErrorDetails.Message | ConvertFrom-Json
            Write-Host "  Details: $($errorResponse | ConvertTo-Json -Depth 3)" -ForegroundColor Red
        } catch {
            Write-Host "  Details: $($_.ErrorDetails.Message)" -ForegroundColor Red
        }
    }
}

Write-Host ""

# Test 2: Get workflow definitions with filters
Write-Host "Test 2: GET /api/workflow-definitions?entityType=Order" -ForegroundColor Cyan
Write-Host "---------------------------------------------------" -ForegroundColor Gray

try {
    $headers = @{
        "Content-Type" = "application/json"
    }
    
    if (-not [string]::IsNullOrEmpty($AuthToken)) {
        $headers["Authorization"] = "Bearer $AuthToken"
    }
    
    $response = Invoke-RestMethod -Uri "$ApiBaseUrl/api/workflow-definitions?entityType=Order" -Method GET -Headers $headers -ErrorAction Stop
    
    Write-Host "✓ Request successful" -ForegroundColor Green
    if ($response -is [Array]) {
        Write-Host "  Workflows found: $($response.Count)" -ForegroundColor Cyan
    } elseif ($response.data) {
        Write-Host "  Workflows found: $($response.data.Count)" -ForegroundColor Cyan
    }
    
} catch {
    $statusCode = $_.Exception.Response.StatusCode.value__
    Write-Host "✗ Request failed (Status: $statusCode)" -ForegroundColor Red
}

Write-Host ""

# Test 3: Get workflow definitions with isActive filter
Write-Host "Test 3: GET /api/workflow-definitions?isActive=true" -ForegroundColor Cyan
Write-Host "---------------------------------------------------" -ForegroundColor Gray

try {
    $headers = @{
        "Content-Type" = "application/json"
    }
    
    if (-not [string]::IsNullOrEmpty($AuthToken)) {
        $headers["Authorization"] = "Bearer $AuthToken"
    }
    
    $response = Invoke-RestMethod -Uri "$ApiBaseUrl/api/workflow-definitions?isActive=true" -Method GET -Headers $headers -ErrorAction Stop
    
    Write-Host "✓ Request successful" -ForegroundColor Green
    if ($response -is [Array]) {
        Write-Host "  Active workflows found: $($response.Count)" -ForegroundColor Cyan
    } elseif ($response.data) {
        Write-Host "  Active workflows found: $($response.data.Count)" -ForegroundColor Cyan
    }
    
} catch {
    $statusCode = $_.Exception.Response.StatusCode.value__
    Write-Host "✗ Request failed (Status: $statusCode)" -ForegroundColor Red
}

Write-Host ""
Write-Host "===========================================" -ForegroundColor Cyan
Write-Host "Test complete" -ForegroundColor Cyan
Write-Host "===========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  1. If you got 401 errors, get your auth token from browser DevTools:" -ForegroundColor White
Write-Host "     - Open DevTools (F12) → Application → Local Storage" -ForegroundColor Gray
Write-Host "     - Copy the 'authToken' value" -ForegroundColor Gray
Write-Host "     - Run: .\test-workflow-api.ps1 -AuthToken 'your-token'" -ForegroundColor Gray
Write-Host "  2. Check API logs for the query execution" -ForegroundColor White
Write-Host "  3. Verify the response matches what's in the database" -ForegroundColor White

