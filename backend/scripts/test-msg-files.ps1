# Test script to upload and parse all .msg files from test-data directory
# This script tests the email parser with Outlook .msg files

param(
    [string]$ApiBaseUrl = "http://localhost:5000",
    [string]$TestDataPath = "C:\Projects\CephasOps\backend\test-data",
    [string]$Username = "",
    [string]$Password = ""
)

Write-Host "=== Parser MSG Files Test ===" -ForegroundColor Cyan
Write-Host ""

# Check if test-data directory exists
if (-not (Test-Path $TestDataPath)) {
    Write-Host "✗ Test data directory not found: $TestDataPath" -ForegroundColor Red
    exit 1
}

# Find all .msg files
$msgFiles = Get-ChildItem -Path $TestDataPath -Filter "*.msg" -File

if ($msgFiles.Count -eq 0) {
    Write-Host "⚠ No .msg files found in $TestDataPath" -ForegroundColor Yellow
    exit 0
}

Write-Host "Found $($msgFiles.Count) .msg file(s) to test:" -ForegroundColor Green
foreach ($file in $msgFiles) {
    Write-Host "  - $($file.Name)" -ForegroundColor Gray
}
Write-Host ""

# Get authentication token if credentials provided
$token = $null
if ($Username -and $Password) {
    Write-Host "Authenticating..." -ForegroundColor Cyan
    try {
        $loginBody = @{
            email = $Username
            password = $Password
        } | ConvertTo-Json

        $loginResponse = Invoke-RestMethod -Uri "$ApiBaseUrl/api/auth/login" `
            -Method Post `
            -ContentType "application/json" `
            -Body $loginBody `
            -ErrorAction Stop

        if ($loginResponse.success -and $loginResponse.data.accessToken) {
            $token = $loginResponse.data.accessToken
            Write-Host "✓ Authentication successful" -ForegroundColor Green
        } elseif ($loginResponse.data -and $loginResponse.data.accessToken) {
            $token = $loginResponse.data.accessToken
            Write-Host "✓ Authentication successful" -ForegroundColor Green
        } else {
            $errorMsg = if ($loginResponse.message) { $loginResponse.message } else { "Unknown error" }
            Write-Host "✗ Authentication failed: $errorMsg" -ForegroundColor Red
            Write-Host "  Response: $($loginResponse | ConvertTo-Json -Depth 3)" -ForegroundColor Yellow
            exit 1
        }
    } catch {
        Write-Host "✗ Authentication error: $($_.Exception.Message)" -ForegroundColor Red
        Write-Host "  Note: You can test without authentication if the API allows it" -ForegroundColor Yellow
    }
} else {
    Write-Host "⚠ No credentials provided. Testing without authentication..." -ForegroundColor Yellow
    Write-Host "  (Provide -Username and -Password parameters for authenticated requests)" -ForegroundColor Yellow
}

Write-Host ""

# Test each .msg file
$results = @()
$successCount = 0
$failCount = 0

foreach ($file in $msgFiles) {
    Write-Host "Testing: $($file.Name)..." -ForegroundColor Cyan
    
    try {
        # Prepare multipart form data
        $boundary = [System.Guid]::NewGuid().ToString()
        $fileBytes = [System.IO.File]::ReadAllBytes($file.FullName)
        $fileName = $file.Name
        
                # Use simpler approach with Invoke-WebRequest for multipart form data
        $form = @{
            files = Get-Item $file.FullName
        }
        
        # Prepare headers
        $headers = @{}
        
        if ($token) {
            $headers["Authorization"] = "Bearer $token"
        }
        
        # Upload file using multipart form
        $uploadResponse = Invoke-RestMethod -Uri "$ApiBaseUrl/api/parser/upload" `
            -Method Post `
            -Headers $headers `
            -Form $form `
            -ErrorAction Stop
        
        if ($uploadResponse.success -or $uploadResponse.id) {
            $sessionId = if ($uploadResponse.data.id) { $uploadResponse.data.id } elseif ($uploadResponse.id) { $uploadResponse.id } else { $null }
            $status = if ($uploadResponse.data.status) { $uploadResponse.data.status } elseif ($uploadResponse.status) { $uploadResponse.status } else { "Unknown" }
            $ordersCount = if ($uploadResponse.data.parsedOrdersCount) { $uploadResponse.data.parsedOrdersCount } elseif ($uploadResponse.parsedOrdersCount) { $uploadResponse.parsedOrdersCount } else { 0 }
            
            Write-Host "  ✓ Upload successful" -ForegroundColor Green
            Write-Host "    Session ID: $sessionId" -ForegroundColor Gray
            Write-Host "    Status: $status" -ForegroundColor Gray
            Write-Host "    Parsed Orders: $ordersCount" -ForegroundColor Gray
            
            $results += @{
                File = $file.Name
                Status = "Success"
                SessionId = $sessionId
                ParseStatus = $status
                OrdersCount = $ordersCount
                Error = $null
            }
            $successCount++
        } else {
            $errorMsg = if ($uploadResponse.message) { $uploadResponse.message } else { "Unknown error" }
            Write-Host "  ✗ Upload failed: $errorMsg" -ForegroundColor Red
            
            $results += @{
                File = $file.Name
                Status = "Failed"
                SessionId = $null
                ParseStatus = $null
                OrdersCount = 0
                Error = $errorMsg
            }
            $failCount++
        }
    } catch {
        $errorMsg = $_.Exception.Message
        if ($_.ErrorDetails) {
            try {
                $errorDetails = $_.ErrorDetails.Message | ConvertFrom-Json
                if ($errorDetails.message) { $errorMsg = $errorDetails.message }
                elseif ($errorDetails.error) { $errorMsg = $errorDetails.error }
            } catch {
                # Ignore JSON parse errors
            }
        }
        
        Write-Host "  ✗ Error: $errorMsg" -ForegroundColor Red
        
        $results += @{
            File = $file.Name
            Status = "Error"
            SessionId = $null
            ParseStatus = $null
            OrdersCount = 0
            Error = $errorMsg
        }
        $failCount++
    }
    
    Write-Host ""
}

# Summary
Write-Host "=== Test Summary ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "Total files tested: $($msgFiles.Count)" -ForegroundColor White
Write-Host "Successful: $successCount" -ForegroundColor Green
Write-Host "Failed: $failCount" -ForegroundColor $(if ($failCount -gt 0) { "Red" } else { "Green" })
Write-Host ""

if ($results.Count -gt 0) {
    Write-Host "Detailed Results:" -ForegroundColor Cyan
    Write-Host ""
    foreach ($result in $results) {
        $statusColor = if ($result.Status -eq "Success") { "Green" } else { "Red" }
        Write-Host "  $($result.File):" -ForegroundColor White
        Write-Host "    Status: $($result.Status)" -ForegroundColor $statusColor
        if ($result.SessionId) {
            Write-Host "    Session ID: $($result.SessionId)" -ForegroundColor Gray
            Write-Host "    Parse Status: $($result.ParseStatus)" -ForegroundColor Gray
            Write-Host "    Orders Found: $($result.OrdersCount)" -ForegroundColor Gray
        }
        if ($result.Error) {
            Write-Host "    Error: $($result.Error)" -ForegroundColor Red
        }
        Write-Host ""
    }
}

Write-Host "=== Test Complete ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "1. Check parser review queue: http://localhost:5173/orders/parser" -ForegroundColor White
Write-Host "2. View parse sessions in the UI" -ForegroundColor White
Write-Host "3. Review parsed order drafts" -ForegroundColor White
Write-Host ""

