# Check EF migration graph integrity (non-destructive, no DB).
# Run from repo root or backend/: .\backend\scripts\check-migration-graph-integrity.ps1
# Complements validate-migration-hygiene.ps1: enumerates migrations, duplicate timestamps, latest discoverable, graph health.
# See docs/operations/EF_MIGRATION_GRAPH_INTEGRITY_AUDIT.md and EF_MIGRATION_GRAPH_INTEGRITY_DECISION.md

$ErrorActionPreference = "Stop"

$ExpectedNoDesignerCount = 47
$ExpectedWithDesignerCount = 95
$ExpectedTotalMainCount = 142

$MigrationsDir = Join-Path $PSScriptRoot ".." "src" "CephasOps.Infrastructure" "Persistence" "Migrations"
if (-not (Test-Path $MigrationsDir)) {
    $MigrationsDir = Join-Path (Get-Location) "src" "CephasOps.Infrastructure" "Persistence" "Migrations"
    if (-not (Test-Path $MigrationsDir)) {
        Write-Host "ERROR: Migrations folder not found. Run from backend/ or repo root." -ForegroundColor Red
        exit 1
    }
}

$mainFiles = Get-ChildItem -Path $MigrationsDir -Filter "*.cs" -File |
    Where-Object { $_.Name -notmatch "\.Designer\.cs$" -and $_.Name -ne "ApplicationDbContextModelSnapshot.cs" } |
    Sort-Object Name

$missing = @()
$withDesignerList = @()
foreach ($f in $mainFiles) {
    $base = $f.BaseName
    $designerPath = Join-Path $MigrationsDir "$base.Designer.cs"
    if (Test-Path $designerPath) {
        $withDesignerList += $base
    } else {
        $missing += $base
    }
}

$total = $mainFiles.Count
$withDesigner = $withDesignerList.Count
$noDesigner = $missing.Count

# Duplicate timestamp groups (timestamp = prefix before first _)
$byTimestamp = @{}
foreach ($b in $mainFiles.BaseName) {
    if ($b -match '^(\d+)_') {
        $ts = $matches[1]
        if (-not $byTimestamp[$ts]) { $byTimestamp[$ts] = @() }
        $byTimestamp[$ts] += $b
    }
}
$duplicateTimestamps = $byTimestamp.GetEnumerator() | Where-Object { $_.Value.Count -gt 1 } | Sort-Object Name

# Latest discoverable = lexicographically last migration that has Designer (EF orders by ID). Authoritative: EF_MIGRATION_FULL_AUDIT_RECOVERY_PASS.md §2.1 (20260309120000_AddJobRunEventId).
$latestDiscoverable = $null
if ($withDesignerList.Count -gt 0) {
    $latestDiscoverable = ($withDesignerList | Sort-Object)[-1]
}

# Manifest check
$manifestPath = Join-Path $PSScriptRoot ".." ".." "docs" "operations" "EF_NO_DESIGNER_MIGRATIONS_MANIFEST.md"
if (-not (Test-Path $manifestPath)) {
    $manifestPath = Join-Path (Get-Location) "docs" "operations" "EF_NO_DESIGNER_MIGRATIONS_MANIFEST.md"
}
$manifestExists = Test-Path $manifestPath

# Graph health
$countsMatch = ($total -eq $ExpectedTotalMainCount -and $withDesigner -eq $ExpectedWithDesignerCount -and $noDesigner -eq $ExpectedNoDesignerCount)
$health = "OK"
if (-not $countsMatch) { $health = "WARN (counts differ from baseline)" }
if (-not $manifestExists) { $health = "WARN (manifest missing)" }

# Output
Write-Host ""
Write-Host "=== EF Migration Graph Integrity ===" -ForegroundColor Cyan
Write-Host "  Total main: $total | With Designer: $withDesigner | No Designer: $noDesigner"
Write-Host "  Expected:   $ExpectedTotalMainCount | $ExpectedWithDesignerCount | $ExpectedNoDesignerCount"
Write-Host "  Latest discoverable: $latestDiscoverable"
Write-Host "  Manifest exists: $manifestExists"
Write-Host ""

if ($duplicateTimestamps) {
    Write-Host "Duplicate timestamp groups:" -ForegroundColor Yellow
    foreach ($d in $duplicateTimestamps) {
        Write-Host "  $($d.Key): $($d.Value -join ', ')"
    }
    Write-Host ""
}

Write-Host "Graph health: $health" -ForegroundColor $(if ($health -eq "OK") { "Green" } else { "Yellow" })
Write-Host "=== End graph check ===" -ForegroundColor Cyan
Write-Host ""

exit 0
