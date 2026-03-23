# Script to delete a company by email
# Usage: .\delete-company-by-email.ps1 -Email "info@cephas.com.my"

param(
    [Parameter(Mandatory=$true)]
    [string]$Email
)

$apiBaseUrl = "http://localhost:5000/api"
$encodedEmail = [System.Web.HttpUtility]::UrlEncode($Email)
$endpoint = "$apiBaseUrl/companies/by-email?email=$encodedEmail"

Write-Host "Attempting to delete company with email: $Email" -ForegroundColor Yellow
Write-Host "Endpoint: $endpoint" -ForegroundColor Gray

try {
    # Try without auth first (in case auth is not required for this endpoint)
    $response = Invoke-WebRequest -Uri $endpoint -Method DELETE -ContentType "application/json" -ErrorAction Stop
    
    if ($response.StatusCode -eq 204) {
        Write-Host "Company deleted successfully!" -ForegroundColor Green
    } else {
        Write-Host "Response: $($response.StatusCode) - $($response.Content)" -ForegroundColor Yellow
    }
} catch {
    $statusCode = $_.Exception.Response.StatusCode.value__
    $errorMessage = $_.Exception.Message
    
    if ($statusCode -eq 401) {
        Write-Host "Authentication required. Please:" -ForegroundColor Red
        Write-Host "1. Log in to the application" -ForegroundColor Yellow
        Write-Host "2. Open browser DevTools (F12)" -ForegroundColor Yellow
        Write-Host "3. Go to Application/Storage > Local Storage" -ForegroundColor Yellow
        Write-Host "4. Copy the 'authToken' value" -ForegroundColor Yellow
        Write-Host "5. Run this command with token:" -ForegroundColor Yellow
        Write-Host "   `$headers = @{ 'Authorization' = 'Bearer YOUR_TOKEN_HERE' }" -ForegroundColor Cyan
        Write-Host "   Invoke-WebRequest -Uri '$endpoint' -Method DELETE -Headers `$headers" -ForegroundColor Cyan
    } elseif ($statusCode -eq 404) {
        Write-Host "Company with email '$Email' not found." -ForegroundColor Yellow
    } else {
        Write-Host "Error: $errorMessage (Status: $statusCode)" -ForegroundColor Red
    }
    
    exit 1
}

