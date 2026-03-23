# Quick migration script - reads connection string from appsettings.json or $env:DefaultConnection
# Do not commit connection strings. Set $env:DefaultConnection or use -ConnectionString on the target script.
# Usage: .\apply-migration-quick.ps1

$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent (Split-Path -Parent $scriptPath)
$appsettingsPath = Join-Path $projectRoot "src\CephasOps.Api\appsettings.json"

$connectionString = $env:DefaultConnection
if ([string]::IsNullOrWhiteSpace($connectionString) -and (Test-Path $appsettingsPath)) {
    try {
        $appsettings = Get-Content $appsettingsPath | ConvertFrom-Json
        $connectionString = $appsettings.ConnectionStrings.DefaultConnection
    } catch { }
}
if ([string]::IsNullOrWhiteSpace($connectionString)) {
    Write-Host "Error: Connection string required. Set env DefaultConnection or ensure appsettings.json has DefaultConnection (do not commit secrets)." -ForegroundColor Red
    Write-Host "  `$env:DefaultConnection = 'Host=...;Database=cephasops;Username=...;Password=...'" -ForegroundColor Yellow
    exit 1
}
Write-Host "Connection string from env or appsettings" -ForegroundColor Green
Write-Host "`nApplying migration..." -ForegroundColor Cyan
$migrationScript = Join-Path $scriptPath "apply-invoice-submission-history-migration.ps1"
& $migrationScript -ConnectionString $connectionString

