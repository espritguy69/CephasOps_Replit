# Phase 11: Parser governance script (drift report + profile-pack regression gate).
# Requires: $env:DefaultConnection or $env:ConnectionStrings__DefaultConnection (DB connection string).
# Do NOT print the connection string. Exit 0 = PASS, 1 = FAIL.

param(
    [string]$OutDir = ".",
    [int]$DriftDays = 7
)

$ErrorActionPreference = "Stop"
$ApiProject = Join-Path $PSScriptRoot "..\src\CephasOps.Api\CephasOps.Api.csproj"

# Resolve connection string: .NET reads ConnectionStrings__DefaultConnection
if (-not $env:ConnectionStrings__DefaultConnection -and $env:DefaultConnection) {
    $env:ConnectionStrings__DefaultConnection = $env:DefaultConnection
}
if (-not $env:ConnectionStrings__DefaultConnection) {
    Write-Error "Missing required environment variable. Set DefaultConnection or ConnectionStrings__DefaultConnection (do not log the value)."
    exit 1
}

$driftOut = Join-Path $OutDir "drift-weekly.md"

Write-Host "Build (Release)..."
& dotnet build $ApiProject -c Release --verbosity quiet
if ($LASTEXITCODE -ne 0) { Write-Error "Build failed."; exit 1 }

Write-Host "=== Parser Governance ==="
Write-Host "Step 1: Drift report (last $DriftDays days)..."
$step1 = & dotnet run --project $ApiProject --no-build -c Release -- drift-report --days $DriftDays --format markdown --out $driftOut 2>&1
$exit1 = $LASTEXITCODE
Write-Host $step1
if ($exit1 -ne 0) {
    Write-Host "Step 1 (drift-report) failed with exit code $exit1 (informational; continuing)."
}

Write-Host ""
Write-Host "Step 2: Replay all profile packs (regression gate)..."
$step2 = & dotnet run --project $ApiProject --no-build -c Release -- replay-all-profile-packs --ci-mode 2>&1
$exit2 = $LASTEXITCODE
Write-Host $step2

Write-Host ""
Write-Host "=== Result ==="
if ($exit2 -eq 0) {
    Write-Host "PASS: No regressions detected."
    exit 0
} else {
    Write-Host "FAIL: One or more profile packs reported regressions (exit code $exit2)."
    exit 1
}
