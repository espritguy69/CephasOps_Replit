# Script to list all companies
# This will try to get companies from the API

$apiBaseUrl = "http://localhost:5000/api"
$endpoint = "$apiBaseUrl/companies"

Write-Host "Fetching companies from API..." -ForegroundColor Cyan
Write-Host "Endpoint: $endpoint" -ForegroundColor Gray
Write-Host ""

try {
    $response = Invoke-WebRequest -Uri $endpoint -Method GET -ContentType "application/json" -ErrorAction Stop
    
    if ($response.StatusCode -eq 200) {
        $companies = $response.Content | ConvertFrom-Json
        
        if ($companies.Count -eq 0) {
            Write-Host "No companies found in the database." -ForegroundColor Yellow
        } else {
            Write-Host "Found $($companies.Count) company/companies:" -ForegroundColor Green
            Write-Host ""
            
            $companies | ForEach-Object {
                $status = if ($_.IsActive -or $_.isActive) { "Active" } else { "Inactive" }
                $statusColor = if ($_.IsActive -or $_.isActive) { "Green" } else { "Yellow" }
                
                Write-Host "  Company: $($_.LegalName -or $_.legalName)" -ForegroundColor Cyan
                Write-Host "    Short Name: $($_.ShortName -or $_.shortName -or 'N/A')" -ForegroundColor Gray
                Write-Host "    Email: $($_.Email -or $_.email -or 'N/A')" -ForegroundColor Gray
                Write-Host "    Status: $status" -ForegroundColor $statusColor
                Write-Host "    ID: $($_.Id -or $_.id)" -ForegroundColor DarkGray
                Write-Host ""
            }
        }
    }
} catch {
    $statusCode = $_.Exception.Response.StatusCode.value__
    $errorMessage = $_.Exception.Message
    
    if ($statusCode -eq 401) {
        Write-Host "⚠ Authentication required." -ForegroundColor Red
        Write-Host ""
        Write-Host "To list companies, you can:" -ForegroundColor Yellow
        Write-Host "1. Open the app at http://localhost:5173/settings/company" -ForegroundColor Cyan
        Write-Host "2. Or query the database directly" -ForegroundColor Cyan
    } elseif ($statusCode -eq 404) {
        Write-Host "⚠ Endpoint not found" -ForegroundColor Yellow
    } else {
        Write-Host "Error: $errorMessage (Status: $statusCode)" -ForegroundColor Red
        Write-Host ""
        Write-Host "Response content:" -ForegroundColor Yellow
        try {
            $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
            $responseBody = $reader.ReadToEnd()
            Write-Host $responseBody -ForegroundColor Gray
        } catch {
            Write-Host "Could not read response body" -ForegroundColor Gray
        }
    }
}

