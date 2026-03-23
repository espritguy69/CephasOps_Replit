# Run E2E smoke tests: starts backend (optional), then Playwright (which starts frontend via webServer).
# Usage: .\run-e2e.ps1 [-SkipBackend]  (use -SkipBackend if backend is already running)
# Requires: Node in frontend, .NET in backend, PostgreSQL running with cephasops DB.

param(
    [switch]$SkipBackend
)

$ErrorActionPreference = "Stop"
$projectRoot = $PSScriptRoot
$backendApi = Join-Path $projectRoot "backend\src\CephasOps.Api"
$frontendDir = Join-Path $projectRoot "frontend"

Write-Host "E2E Smoke – Playwright" -ForegroundColor Cyan
Write-Host ""

$backendJob = $null
if (-not $SkipBackend) {
    Write-Host "Starting backend in background..." -ForegroundColor Yellow
    Push-Location $backendApi
    try {
        $backendJob = Start-Job -ScriptBlock {
            Set-Location $using:backendApi
            $env:ASPNETCORE_ENVIRONMENT = "Development"
            dotnet run --urls "http://localhost:5000" 2>&1
        }
        Pop-Location
    } catch {
        Pop-Location
        throw
    }
    Write-Host "Waiting for backend at http://localhost:5000/api/admin/health ..." -ForegroundColor Gray
    $maxAttempts = 30
    $attempt = 0
    while ($attempt -lt $maxAttempts) {
        try {
            $r = Invoke-WebRequest -Uri "http://localhost:5000/api/admin/health" -UseBasicParsing -TimeoutSec 3 -ErrorAction SilentlyContinue
            if ($r.StatusCode -eq 200 -or $r.StatusCode -eq 401) {
                Write-Host "Backend ready." -ForegroundColor Green
                break
            }
        } catch { }
        $attempt++
        Start-Sleep -Seconds 2
    }
    if ($attempt -ge $maxAttempts) {
        if ($backendJob) { Stop-Job $backendJob; Remove-Job $backendJob }
        Write-Host "Backend did not become ready. Run without -SkipBackend after starting backend manually, or fix DB/port." -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "Skipping backend start (using existing backend)." -ForegroundColor Gray
}

Write-Host "Running Playwright E2E (frontend will start automatically)..." -ForegroundColor Cyan
Push-Location $frontendDir
try {
    npx playwright test --reporter=list
    $exitCode = $LASTEXITCODE
} finally {
    Pop-Location
    if ($backendJob) {
        Write-Host "Stopping backend job..." -ForegroundColor Gray
        Stop-Job $backendJob
        Remove-Job $backendJob
    }
}
exit $exitCode
