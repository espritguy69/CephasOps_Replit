# Classify current EF migration state for governance (A/B/C/D/E).
# Read-only; no DB. Run from repo root or backend/: .\backend\scripts\classify-migration-state.ps1
# Optional: -ReportPath "docs/operations/EF_MIGRATION_LAST_CLASSIFICATION.txt" to write a short report.
# See docs/operations/EF_MIGRATION_AUTO_CLASSIFICATION_AUDIT.md and ef-migration-governance.mdc

param(
    [string]$ReportPath   # If set, write classification report to this path (relative to repo root or backend).
)

$ErrorActionPreference = "Stop"

# Baseline (must match validate-migration-hygiene.ps1 and docs; includes 20260310031127_AddExternalIntegrationBus)
$ExpectedNoDesignerCount = 47
$ExpectedWithDesignerCount = 95
$ExpectedTotalMainCount = 142
$SuspiciousLineCount = 500

$MigrationsDir = Join-Path $PSScriptRoot ".." "src" "CephasOps.Infrastructure" "Persistence" "Migrations"
if (-not (Test-Path $MigrationsDir)) {
    $MigrationsDir = Join-Path (Get-Location) "src" "CephasOps.Infrastructure" "Persistence" "Migrations"
    if (-not (Test-Path $MigrationsDir)) {
        Write-Host "CLASSIFICATION: ERROR (Migrations folder not found)" -ForegroundColor Red
        if ($ReportPath) { Set-Content -Path $ReportPath -Value "CLASSIFICATION: ERROR (Migrations folder not found)" }
        exit 1
    }
}

$mainFiles = Get-ChildItem -Path $MigrationsDir -Filter "*.cs" -File |
    Where-Object { $_.Name -notmatch "\.Designer\.cs$" -and $_.Name -ne "ApplicationDbContextModelSnapshot.cs" }

$missing = @()
foreach ($f in $mainFiles) {
    $base = $f.BaseName
    $designerPath = Join-Path $MigrationsDir "$base.Designer.cs"
    if (-not (Test-Path $designerPath)) {
        $missing += $base
    }
}

$total = $mainFiles.Count
$withDesigner = $total - $missing.Count
$noDesigner = $missing.Count

# Newest migration by filename (lexicographic = timestamp order)
$newest = $mainFiles | Sort-Object Name -Descending | Select-Object -First 1
$newestHasDesigner = $false
$newestLines = 0
$newestId = ""
if ($newest) {
    $newestId = $newest.BaseName
    $designerPath = Join-Path $MigrationsDir "$($newest.BaseName).Designer.cs"
    $newestHasDesigner = Test-Path $designerPath
    $newestLines = (Get-Content $newest.FullName -ErrorAction SilentlyContinue | Measure-Object -Line).Lines
}

# Classify
$classification = ""
$action = ""
$details = ""

$countsDiffer = ($total -ne $ExpectedTotalMainCount -or $withDesigner -ne $ExpectedWithDesignerCount -or $noDesigner -ne $ExpectedNoDesignerCount)

if ($countsDiffer) {
    if ($noDesigner -gt $ExpectedNoDesignerCount) {
        $classification = "E. BASELINE DRIFT + B/C. NEW SCRIPT-ONLY MIGRATION(S) (intentional vs accidental)"
        $action = "Counts differ: no-Designer count $noDesigner exceeds expected $ExpectedNoDesignerCount. If INTENTIONAL script-only: add the new migration(s) to docs/operations/EF_NO_DESIGNER_MIGRATIONS_MANIFEST.md and update ExpectedNoDesignerCount and ExpectedTotalMainCount in validate-migration-hygiene.ps1 and all related docs. If ACCIDENTAL: do not commit; recreate with backend\scripts\create-migration.ps1 -MigrationName ""YourDescriptiveName"" so a Designer is generated."
        $details = "Newest: $newestId | Has Designer: $newestHasDesigner | Actual: total=$total withDesigner=$withDesigner noDesigner=$noDesigner | Expected noDesigner=$ExpectedNoDesignerCount"
    } elseif ($withDesigner -gt $ExpectedWithDesignerCount -and $noDesigner -eq $ExpectedNoDesignerCount) {
        $classification = "E. BASELINE DRIFT (new migration with Designer)"
        $action = "New migration with Designer detected. Update ExpectedTotalMainCount and ExpectedWithDesignerCount in validate-migration-hygiene.ps1 and in docs (MIGRATION_HYGIENE.md, manifest references, runbook, PR checklist). Then re-run validator."
        $details = "Newest: $newestId | Actual: total=$total withDesigner=$withDesigner noDesigner=$noDesigner | Expected: $ExpectedTotalMainCount / $ExpectedWithDesignerCount / $ExpectedNoDesignerCount"
    } else {
        $classification = "E. BASELINE / DOCUMENTATION DRIFT"
        $action = "Counts differ from authoritative baseline. Align validator expected counts and docs (manifest, MIGRATION_HYGIENE.md, runbook, PR checklist) with actual state. Do not commit until docs and validator are updated."
        $details = "Actual: total=$total withDesigner=$withDesigner noDesigner=$noDesigner | Expected: total=$ExpectedTotalMainCount withDesigner=$ExpectedWithDesignerCount noDesigner=$ExpectedNoDesignerCount"
    }
} elseif ($newest -and $newestHasDesigner -and $newestLines -gt $SuspiciousLineCount) {
    $classification = "D. SNAPSHOT DRIFT RISK"
    $action = "Newest migration is unusually large ($newestLines lines). Do not commit without review. Compare diff to intended model changes; if scope is far larger, stop and escalate to a dedicated snapshot reconciliation pass. See docs/operations/EF_SAFE_MIGRATION_WORKFLOW.md."
    $details = "Newest: $newestId | Lines: $newestLines (threshold $SuspiciousLineCount)"
} elseif ($newest -and $newestHasDesigner) {
    $classification = "A. NORMAL EF MIGRATION"
    $action = "Newest migration has Designer and checks pass. Next: run validate-migration-hygiene.ps1 before commit; if this is a new migration, update ExpectedTotalMainCount and ExpectedWithDesignerCount in validate-migration-hygiene.ps1 and in docs when you add the next one."
    $details = "Newest: $newestId | With Designer: yes | Total: $total | With Designer: $withDesigner | No-Designer: $noDesigner"
} else {
    # Counts match but newest has no Designer (edge case: newest is one of the known 47)
    $classification = "A. NORMAL EF MIGRATION (baseline state)"
    $action = "Counts match baseline. No new migration detected. For any new migration, use create-migration.ps1 and ensure validator passes."
    $details = "Newest: $newestId | With Designer: $newestHasDesigner | Total: $total"
}

# Console output
Write-Host ""
Write-Host "=== EF Migration Classification ===" -ForegroundColor Cyan
Write-Host "  $details" -ForegroundColor Gray
Write-Host ""
Write-Host "CLASSIFICATION: $classification" -ForegroundColor $(if ($classification -match "^(A\.)") { "Green" } elseif ($classification -match "^(D\.|E\.)") { "Yellow" } else { "Yellow" })
Write-Host "REQUIRED ACTION: $action" -ForegroundColor Cyan
Write-Host ""

# Optional report file
if ($ReportPath) {
    $reportDir = Split-Path $ReportPath -Parent
    $fromBackend = Join-Path $PSScriptRoot ".." ".."
    $fullPath = if ($reportDir) { Join-Path $fromBackend $ReportPath } else { Join-Path $fromBackend $ReportPath }
    $parent = Split-Path $fullPath -Parent
    if (-not (Test-Path $parent)) {
        $parentFromCwd = Join-Path (Get-Location) $ReportPath
        $parent = Split-Path $parentFromCwd -Parent
        $fullPath = Join-Path (Get-Location) $ReportPath
    }
    if ($parent -and -not (Test-Path $parent)) { New-Item -ItemType Directory -Path $parent -Force | Out-Null }
    $reportContent = @"
EF Migration Classification Report
Generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')

$details

CLASSIFICATION: $classification

REQUIRED ACTION: $action

Newest migration ID: $newestId
Has Designer: $newestHasDesigner
Line count: $newestLines
Total main: $total | With Designer: $withDesigner | No-Designer: $noDesigner
Expected: total=$ExpectedTotalMainCount withDesigner=$ExpectedWithDesignerCount noDesigner=$ExpectedNoDesignerCount
"@
    Set-Content -Path $fullPath -Value $reportContent -Encoding UTF8
    Write-Host "Report written: $fullPath" -ForegroundColor Gray
}

exit 0
