# PowerShell script to import Materials from CSV via API
# This script reads materials-default.csv and imports data via the CephasOps API
#
# Usage:
#   .\import-materials.ps1 -ApiBaseUrl "http://localhost:5000" -Email "simon@cephas.com.my" -Password "J@saw007"
#
# Or set environment variables:
#   $env:CEPHASOPS_API_URL = "http://localhost:5000"
#   $env:CEPHASOPS_EMAIL = "simon@cephas.com.my"
#   $env:CEPHASOPS_PASSWORD = "J@saw007"
#   .\import-materials.ps1

param(
    [string]$ApiBaseUrl = $env:CEPHASOPS_API_URL ?? "http://localhost:5000",
    [string]$Email = $env:CEPHASOPS_EMAIL ?? "simon@cephas.com.my",
    [string]$Password = $env:CEPHASOPS_PASSWORD ?? "J@saw007",
    [string]$CsvFile = (Join-Path $PSScriptRoot "materials-default.csv")
)

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "CephasOps Materials Import Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check if CSV file exists
if (-not (Test-Path $CsvFile)) {
    Write-Host "ERROR: CSV file not found at: $CsvFile" -ForegroundColor Red
    Write-Host ""
    Write-Host "Please ensure the materials-default.csv file exists in the scripts directory." -ForegroundColor Yellow
    exit 1
}

Write-Host "CSV File: $CsvFile" -ForegroundColor Gray
Write-Host "API URL: $ApiBaseUrl" -ForegroundColor Gray
Write-Host ""

# Step 1: Authenticate and get JWT token
Write-Host "Step 1: Authenticating..." -ForegroundColor Yellow
try {
    $loginBody = @{
        email = $Email
        password = $Password
    } | ConvertTo-Json

    $loginResponse = Invoke-RestMethod -Uri "$ApiBaseUrl/api/auth/login" `
        -Method Post `
        -ContentType "application/json" `
        -Body $loginBody `
        -ErrorAction Stop

    if (-not $loginResponse.data -or -not $loginResponse.data.token) {
        Write-Host "ERROR: Authentication failed - no token received" -ForegroundColor Red
        Write-Host "Response: $($loginResponse | ConvertTo-Json)" -ForegroundColor Gray
        exit 1
    }

    $token = $loginResponse.data.token
    Write-Host "✓ Authentication successful" -ForegroundColor Green
} catch {
    Write-Host "ERROR: Authentication failed" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $responseBody = $reader.ReadToEnd()
        Write-Host "Response: $responseBody" -ForegroundColor Gray
    }
    exit 1
}

# Step 2: Upload CSV file
Write-Host ""
Write-Host "Step 2: Uploading CSV file..." -ForegroundColor Yellow
try {
    $boundary = [System.Guid]::NewGuid().ToString()
    $fileBytes = [System.IO.File]::ReadAllBytes($CsvFile)
    $fileName = Split-Path $CsvFile -Leaf

    $bodyLines = @(
        "--$boundary",
        "Content-Disposition: form-data; name=`"file`"; filename=`"$fileName`"",
        "Content-Type: text/csv",
        "",
        [System.Text.Encoding]::UTF8.GetString($fileBytes),
        "--$boundary--"
    )
    $body = $bodyLines -join "`r`n"
    $bodyBytes = [System.Text.Encoding]::GetEncoding("iso-8859-1").GetBytes($body)

    $headers = @{
        "Authorization" = "Bearer $token"
    }

    $importResponse = Invoke-RestMethod -Uri "$ApiBaseUrl/api/inventory/materials/import" `
        -Method Post `
        -Headers $headers `
        -ContentType "multipart/form-data; boundary=$boundary" `
        -Body $bodyBytes `
        -ErrorAction Stop

    Write-Host "✓ File uploaded successfully" -ForegroundColor Green
    Write-Host ""
    Write-Host "Import Results:" -ForegroundColor Cyan
    Write-Host "  Total Rows: $($importResponse.data.totalRows)" -ForegroundColor Gray
    Write-Host "  Success: $($importResponse.data.successCount)" -ForegroundColor Green
    Write-Host "  Errors: $($importResponse.data.errorCount)" -ForegroundColor $(if ($importResponse.data.errorCount -gt 0) { "Red" } else { "Gray" })

    if ($importResponse.data.errors -and $importResponse.data.errors.Count -gt 0) {
        Write-Host ""
        Write-Host "Errors:" -ForegroundColor Red
        foreach ($error in $importResponse.data.errors) {
            Write-Host "  Row $($error.rowNumber): $($error.message)" -ForegroundColor Red
        }
    }

    Write-Host ""
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "Materials import completed!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Cyan

} catch {
    Write-Host "ERROR: Import failed" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $responseBody = $reader.ReadToEnd()
        Write-Host "Response: $responseBody" -ForegroundColor Gray
    }
    Write-Host ""
    Write-Host "Note: If the API import endpoint is not yet implemented, you can:" -ForegroundColor Yellow
    Write-Host "  1. Use the web UI: Settings > Materials > Import" -ForegroundColor Yellow
    Write-Host "  2. Or manually import via SQL (see documentation)" -ForegroundColor Yellow
    exit 1
}

