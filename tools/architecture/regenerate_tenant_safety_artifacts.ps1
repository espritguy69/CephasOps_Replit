# Regenerate all tenant-safety artifacts (dashboard, JSON, history, diagram).
# Run from repo root: ./tools/architecture/regenerate_tenant_safety_artifacts.ps1
# Use this before committing to avoid CI failures for out-of-date dashboard or diagram.
# Optional: pass -ChangedFiles @("path1","path2") to match CI behavior for "changed in branch" detection.

param(
    [string[]]$ChangedFiles = @()
)

$ErrorActionPreference = "Stop"
$ScriptDir = if ($PSScriptRoot) { $PSScriptRoot } else { Split-Path -Parent $MyInvocation.MyCommand.Path }
$RepoRoot = (Resolve-Path (Join-Path $ScriptDir "..\..")).Path

Write-Host "Regenerating tenant-safety diagram..."
& (Join-Path $ScriptDir "generate_tenant_safety_diagram.ps1") -RepoRoot $RepoRoot
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "Regenerating tenant-safety health dashboard and JSON..."
& (Join-Path $ScriptDir "generate_tenant_safety_health.ps1") -RepoRoot $RepoRoot -EmitJson -ChangedFiles $ChangedFiles
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "Running Platform Guardian..."
& (Join-Path $ScriptDir "run_platform_guardian.ps1") -RepoRoot $RepoRoot
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "Running Platform Safety Drift Monitor..."
& (Join-Path $ScriptDir "run_platform_safety_drift.ps1") -RepoRoot $RepoRoot
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "Done. Commit these if changed: backend/docs/operations/TENANT_SAFETY_HEALTH_DASHBOARD.md, backend/docs/operations/PLATFORM_GUARDIAN_REPORT.md, backend/docs/operations/PLATFORM_SAFETY_DRIFT_REPORT.md, backend/docs/architecture/SECURITY_AND_TENANT_SAFETY_ARCHITECTURE.md, tools/architecture/tenant_safety_health.json, tools/architecture/tenant_safety_history.json, tools/architecture/platform_guardian_report.json, tools/architecture/platform_safety_drift_report.json, tools/architecture/platform_guardian_baseline.json"
exit 0
