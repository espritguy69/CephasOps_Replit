# Run Building Import via CephasOps API
# Prerequisites: API running (e.g. http://localhost:5000), admin credentials.
# Usage: .\Run-BuildingImport.ps1 -ApiBaseUrl "http://localhost:5000" -Email "simon@cephas.com.my" -Password "J@saw007"

param(
    [string]$ApiBaseUrl = "http://localhost:5000",
    [string]$Email = "simon@cephas.com.my",
    [string]$Password = "J@saw007",
    [string]$CsvPath = ""
)

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
if ([string]::IsNullOrWhiteSpace($CsvPath)) {
    $CsvPath = Join-Path $ScriptDir "buildings_import_normalized.csv"
}

if (-not (Test-Path $CsvPath)) {
    Write-Error "CSV not found: $CsvPath"
    exit 1
}

# Login
$loginBody = @{ email = $Email; password = $Password } | ConvertTo-Json
$loginUri = "$ApiBaseUrl/api/auth/login"
Write-Host "Logging in to $loginUri ..."
try {
    $loginResponse = Invoke-RestMethod -Uri $loginUri -Method Post -Body $loginBody -ContentType "application/json"
} catch {
    Write-Error "Login failed: $_"
    exit 1
}

$token = $loginResponse.data.accessToken
if (-not $token) {
    Write-Error "No access token in login response"
    exit 1
}
Write-Host "Login OK."

# Upload CSV (multipart/form-data)
$importUri = "$ApiBaseUrl/api/buildings/import"
$fileBytes = [System.IO.File]::ReadAllBytes($CsvPath)
$fileName = [System.IO.Path]::GetFileName($CsvPath)

Add-Type -AssemblyName System.Net.Http
$client = New-Object System.Net.Http.HttpClient
$client.DefaultRequestHeaders.Add("Authorization", "Bearer $token")
$content = New-Object System.Net.Http.MultipartFormDataContent
$fileContent = New-Object System.Net.Http.ByteArrayContent (,[byte[]]$fileBytes)
$fileContent.Headers.ContentType = [System.Net.Http.Headers.MediaTypeHeaderValue]::Parse("text/csv")
[void]$content.Add($fileContent, "file", $fileName)

Write-Host "Uploading $CsvPath to $importUri ..."
$response = $client.PostAsync($importUri, $content).Result
$responseBody = $response.Content.ReadAsStringAsync().Result
if (-not $response.IsSuccessStatusCode) {
    Write-Error "Import failed: $($response.StatusCode) $responseBody"
    exit 1
}
$importResponse = $responseBody | ConvertFrom-Json

Write-Host "Import result:"
$importResponse | ConvertTo-Json -Depth 5
$data = if ($importResponse.data) { $importResponse.data } else { $importResponse }
if ($data) {
    Write-Host "TotalRows: $($data.totalRows), SuccessCount: $($data.successCount), ErrorCount: $($data.errorCount)"
    $skipped = @($data.errors | Where-Object { $_.message -like "Skipped (duplicate)*" }).Count
    if ($skipped -gt 0) { Write-Host "Skipped (duplicates): $skipped" }
}
Write-Host "Done."
