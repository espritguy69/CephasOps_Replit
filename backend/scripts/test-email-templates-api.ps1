# Test script for Email Templates API endpoints
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

Write-Host "`n=== Testing Email Templates API ===" -ForegroundColor Cyan
Write-Host "Base URL: $BaseUrl`n" -ForegroundColor Gray

# Test 1: Get All Templates
Write-Host "Test 1: Get All Templates" -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "$BaseUrl/api/email-templates" -Method Get -Headers $headers
    Write-Host "✓ Success: Found $($response.Count) templates" -ForegroundColor Green
    $response | ForEach-Object {
        Write-Host "  - $($_.Code): $($_.Name) (Active: $($_.IsActive))" -ForegroundColor Gray
    }
} catch {
    Write-Host "✗ Failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 2: Get Template by Code
Write-Host "`nTest 2: Get Template by Code (RESCHEDULE_TIME_ONLY)" -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "$BaseUrl/api/email-templates/by-code/RESCHEDULE_TIME_ONLY" -Method Get -Headers $headers
    Write-Host "✓ Success: Template found" -ForegroundColor Green
    Write-Host "  Name: $($response.Name)" -ForegroundColor Gray
    Write-Host "  Code: $($response.Code)" -ForegroundColor Gray
    Write-Host "  AutoProcessReplies: $($response.AutoProcessReplies)" -ForegroundColor Gray
} catch {
    Write-Host "✗ Failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 3: Get Templates by Entity Type
Write-Host "`nTest 3: Get Templates by Entity Type (Order)" -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "$BaseUrl/api/email-templates/by-entity-type/Order" -Method Get -Headers $headers
    Write-Host "✓ Success: Found $($response.Count) templates for Order" -ForegroundColor Green
} catch {
    Write-Host "✗ Failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 4: Get Emails (Inbox)
Write-Host "`nTest 4: Get Inbox Emails" -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "$BaseUrl/api/emails?direction=Inbound" -Method Get -Headers $headers
    Write-Host "✓ Success: Found $($response.Count) inbound emails" -ForegroundColor Green
} catch {
    Write-Host "✗ Failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 5: Get Emails (Sent)
Write-Host "`nTest 5: Get Sent Emails" -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "$BaseUrl/api/emails?direction=Outbound" -Method Get -Headers $headers
    Write-Host "✓ Success: Found $($response.Count) outbound emails" -ForegroundColor Green
} catch {
    Write-Host "✗ Failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n=== API Tests Complete ===" -ForegroundColor Cyan

