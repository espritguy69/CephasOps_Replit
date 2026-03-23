# Run Order Types hierarchy fix using connection string from appsettings.Development.json
$ErrorActionPreference = "Stop"
$apiDir = Join-Path $PSScriptRoot "..\src\CephasOps.Api"
$jsonPath = Join-Path $apiDir "appsettings.Development.json"
$sqlPath = Join-Path $PSScriptRoot "order-types-hierarchy-fix.sql"

if (-not (Test-Path $jsonPath)) { throw "Not found: $jsonPath" }
if (-not (Test-Path $sqlPath)) { throw "Not found: $sqlPath" }

$json = Get-Content -Raw $jsonPath | ConvertFrom-Json
$conn = $json.ConnectionStrings.DefaultConnection
if ($conn -match 'Password=([^;]+)') {
    $env:PGPASSWORD = $matches[1].Trim()
}
& psql -h localhost -p 5432 -d cephasops -U postgres -f $sqlPath
