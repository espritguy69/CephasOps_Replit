# Platform Safety Drift Monitor: compare current guardian/health output to previous baseline.
# Run from repo root after Platform Guardian: ./tools/architecture/run_platform_safety_drift.ps1
# Requires: platform_guardian_report.json, tenant_safety_health.json (from regenerate_tenant_safety_artifacts.ps1 or run_platform_guardian.ps1).
# Output: tools/architecture/platform_safety_drift_report.json, backend/docs/operations/PLATFORM_SAFETY_DRIFT_REPORT.md
# Baseline: tools/architecture/platform_guardian_baseline.json (created/updated by this script).
# Does NOT block runtime or CI; surfaces drift for operator review.

param(
    [string]$RepoRoot = "",
    [string]$GuardianReportPath = "",
    [string]$HealthJsonPath = "",
    [string]$BaselinePath = "",
    [string]$DriftReportJsonPath = "",
    [string]$DriftReportMdPath = ""
)

$ErrorActionPreference = "Stop"
$ScriptDir = if ($PSScriptRoot) { $PSScriptRoot } else { Split-Path -Parent $MyInvocation.MyCommand.Path }
$RepoRoot = if ($RepoRoot) { $RepoRoot } else { (Resolve-Path (Join-Path $ScriptDir "..\..")).Path }
$GuardianReportPath = if ($GuardianReportPath) { $GuardianReportPath } else { Join-Path $RepoRoot "tools/architecture/platform_guardian_report.json" }
$HealthJsonPath = if ($HealthJsonPath) { $HealthJsonPath } else { Join-Path $RepoRoot "tools/architecture/tenant_safety_health.json" }
$BaselinePath = if ($BaselinePath) { $BaselinePath } else { Join-Path $RepoRoot "tools/architecture/platform_guardian_baseline.json" }
$DriftReportJsonPath = if ($DriftReportJsonPath) { $DriftReportJsonPath } else { Join-Path $RepoRoot "tools/architecture/platform_safety_drift_report.json" }
$DriftReportMdPath = if ($DriftReportMdPath) { $DriftReportMdPath } else { Join-Path $RepoRoot "backend/docs/operations/PLATFORM_SAFETY_DRIFT_REPORT.md" }

$scanTimestamp = Get-Date -Format "o"

# ---- Artifact integrity: expect guardian and health to exist ----
$artifactIntegrity = @{ allPresent = $true; missingArtifacts = @(); message = "" }
if (-not (Test-Path $GuardianReportPath)) {
    $artifactIntegrity.allPresent = $false
    $artifactIntegrity.missingArtifacts += "platform_guardian_report.json"
}
if (-not (Test-Path $HealthJsonPath)) {
    $artifactIntegrity.allPresent = $false
    $artifactIntegrity.missingArtifacts += "tenant_safety_health.json"
}
if (-not $artifactIntegrity.allPresent) {
    $artifactIntegrity.message = "Run ./tools/architecture/regenerate_tenant_safety_artifacts.ps1 to generate missing artifacts."
    $driftReport = @{
        scanTimestamp = $scanTimestamp
        baselineTimestamp = $null
        driftDetected = $true
        summary = "Artifact integrity: one or more expected artifacts are missing. Generate artifacts first."
        changesDetected = @()
        newSensitiveFiles = @()
        bypassGrowth = @{ allowlistIncrease = $false; unallowlistedIncrease = $false; detail = "N/A - health missing" }
        advisoryDelta = 0
        enforcedDelta = 0
        documentedOnlyDelta = 0
        limitations = @("Drift comparison skipped when artifacts are missing.")
        artifactIntegrity = $artifactIntegrity
    }
    $reportDir = Split-Path -Parent $DriftReportJsonPath
    if (-not (Test-Path $reportDir)) { New-Item -ItemType Directory -Path $reportDir -Force | Out-Null }
    $driftReport | ConvertTo-Json -Depth 4 | Set-Content -Path $DriftReportJsonPath -Encoding UTF8
    $mdDir = Split-Path -Parent $DriftReportMdPath
    if (-not (Test-Path $mdDir)) { New-Item -ItemType Directory -Path $mdDir -Force | Out-Null }
    Set-Content -Path $DriftReportMdPath -Value "# Platform Safety Drift Report`n`n*Generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm') UTC. Artifacts missing; run regenerate_tenant_safety_artifacts.ps1 first.*" -NoNewline
    Write-Host "Drift monitor: missing artifacts; drift report written with integrity failure."
    exit 0
}
$artifactIntegrity.message = "Guardian and health artifacts present."

$guardian = Get-Content -Path $GuardianReportPath -Raw | ConvertFrom-Json
$health = Get-Content -Path $HealthJsonPath -Raw | ConvertFrom-Json

# ---- Build current snapshot for comparison ----
$curEnforced = if ($guardian.summary.enforcedFindings) { [int]$guardian.summary.enforcedFindings } else { 0 }
$curAdvisory = if ($guardian.summary.advisoryFindings) { [int]$guardian.summary.advisoryFindings } else { 0 }
$curDocOnly = if ($guardian.summary.documentedOnlyFindings) { [int]$guardian.summary.documentedOnlyFindings } else { 0 }
$curSensitive = @()
if ($guardian.sensitiveFilesRequiringReview) {
    foreach ($s in $guardian.sensitiveFilesRequiringReview) { $curSensitive += $s }
}
$allowlistManual = if ($health.allowlist.manualScopeAllowed) { [int]$health.allowlist.manualScopeAllowed } else { 0 }
$allowlistExecutor = if ($health.allowlist.executorNotRequired) { [int]$health.allowlist.executorNotRequired } else { 0 }
$unallowEnter = if ($health.manualScope.unallowlisted.enterBypass) { [int]$health.manualScope.unallowlisted.enterBypass } else { 0 }
$unallowExit = if ($health.manualScope.unallowlisted.exitBypass) { [int]$health.manualScope.unallowlisted.exitBypass } else { 0 }
$unallowCurrent = if ($health.manualScope.unallowlisted.currentTenantId) { [int]$health.manualScope.unallowlisted.currentTenantId } else { 0 }

$currentSnapshot = @{
    baselineTimestamp = $guardian.scanTimestamp
    summary = @{ enforcedFindings = $curEnforced; advisoryFindings = $curAdvisory; documentedOnlyFindings = $curDocOnly }
    sensitiveFilesRequiringReview = @($curSensitive)
    allowlistManualScopeAllowed = $allowlistManual
    allowlistExecutorNotRequired = $allowlistExecutor
    unallowlistedEnterBypass = $unallowEnter
    unallowlistedExitBypass = $unallowExit
    unallowlistedCurrentTenantId = $unallowCurrent
}

# ---- First run: no baseline ----
if (-not (Test-Path $BaselinePath)) {
    $currentSnapshot | ConvertTo-Json -Depth 4 | Set-Content -Path $BaselinePath -Encoding UTF8
    $driftReport = @{
        scanTimestamp = $scanTimestamp
        baselineTimestamp = $null
        driftDetected = $false
        summary = "First-run baseline established. No drift comparison; next run will compare against this baseline."
        changesDetected = @()
        newSensitiveFiles = @()
        bypassGrowth = @{ allowlistIncrease = $false; unallowlistedIncrease = $false; detail = "N/A - first run" }
        advisoryDelta = 0
        enforcedDelta = 0
        documentedOnlyDelta = 0
        limitations = @("Baseline stores guardian summary, sensitive files list, and health allowlist/unallowlisted counts.")
        artifactIntegrity = $artifactIntegrity
    }
    $reportDir = Split-Path -Parent $DriftReportJsonPath
    if (-not (Test-Path $reportDir)) { New-Item -ItemType Directory -Path $reportDir -Force | Out-Null }
    $driftReport | ConvertTo-Json -Depth 4 | Set-Content -Path $DriftReportJsonPath -Encoding UTF8
    Write-Host "Drift baseline written: $BaselinePath (first run)."
    $md = @"
# Platform Safety Drift Report

*Generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm') UTC. Do not edit by hand.*

---

## Safety posture summary

**First-run baseline established.** The current Platform Guardian and health outputs have been saved as the baseline. The next run of the drift monitor will compare against this snapshot and report any changes.

- **Enforced findings (current):** $curEnforced
- **Advisory findings (current):** $curAdvisory
- **Documented-only findings (current):** $curDocOnly
- **Sensitive files requiring review (current):** $($curSensitive.Count)

No drift comparison was performed. Run ``./tools/architecture/run_platform_safety_drift.ps1`` again after the next artifact regeneration to see deltas.

---

## Investigation recommended?

No. This was the first run; use the next run to detect drift.

---

## Advisory vs enforced drift

- **Enforced drift** (increase in enforced findings) usually indicates new unallowlisted manual scope or executor gaps; CI may already fail. See [TENANT_SAFETY_CI.md](TENANT_SAFETY_CI.md).
- **Advisory drift** (increase in advisory findings) is for visibility; it does not cause CI failure. Review new advisory findings when touching related code.

Machine-readable: ``tools/architecture/platform_safety_drift_report.json``.
"@
    $mdDir = Split-Path -Parent $DriftReportMdPath
    if (-not (Test-Path $mdDir)) { New-Item -ItemType Directory -Path $mdDir -Force | Out-Null }
    Set-Content -Path $DriftReportMdPath -Value $md -NoNewline
    Write-Host "Drift report written: $DriftReportJsonPath, $DriftReportMdPath"
    exit 0
}

# ---- Load baseline and compare ----
$baseline = Get-Content -Path $BaselinePath -Raw | ConvertFrom-Json
$baseEnforced = if ($baseline.summary.enforcedFindings) { [int]$baseline.summary.enforcedFindings } else { 0 }
$baseAdvisory = if ($baseline.summary.advisoryFindings) { [int]$baseline.summary.advisoryFindings } else { 0 }
$baseDocOnly = if ($baseline.summary.documentedOnlyFindings) { [int]$baseline.summary.documentedOnlyFindings } else { 0 }
$baseSensitive = @()
if ($baseline.sensitiveFilesRequiringReview) {
    foreach ($s in $baseline.sensitiveFilesRequiringReview) { $baseSensitive += $s }
}
$baseAllowlistManual = if ($baseline.allowlistManualScopeAllowed) { [int]$baseline.allowlistManualScopeAllowed } else { 0 }
$baseAllowlistExecutor = if ($baseline.allowlistExecutorNotRequired) { [int]$baseline.allowlistExecutorNotRequired } else { 0 }
$baseUnallowEnter = if ($baseline.unallowlistedEnterBypass) { [int]$baseline.unallowlistedEnterBypass } else { 0 }
$baseUnallowExit = if ($baseline.unallowlistedExitBypass) { [int]$baseline.unallowlistedExitBypass } else { 0 }
$baseUnallowCurrent = if ($baseline.unallowlistedCurrentTenantId) { [int]$baseline.unallowlistedCurrentTenantId } else { 0 }

$enforcedDelta = $curEnforced - $baseEnforced
$advisoryDelta = $curAdvisory - $baseAdvisory
$documentedOnlyDelta = $curDocOnly - $baseDocOnly

$newSensitive = @()
$baseSet = @{}
foreach ($p in $baseSensitive) { $baseSet[$p] = $true }
foreach ($p in $curSensitive) {
    if (-not $baseSet.ContainsKey($p)) { $newSensitive += $p }
}

$allowlistIncrease = ($allowlistManual -gt $baseAllowlistManual) -or ($allowlistExecutor -gt $baseAllowlistExecutor)
$unallowlistedIncrease = ($unallowEnter -gt $baseUnallowEnter) -or ($unallowExit -gt $baseUnallowExit) -or ($unallowCurrent -gt $baseUnallowCurrent)
$bypassDetail = "allowlist manual: $baseAllowlistManual -> $allowlistManual; executor not required: $baseAllowlistExecutor -> $allowlistExecutor; unallowlisted enter: $baseUnallowEnter -> $unallowEnter; exit: $baseUnallowExit -> $unallowExit; currentTenantId: $baseUnallowCurrent -> $unallowCurrent"

$changesDetected = [System.Collections.ArrayList]::new()
if ($enforcedDelta -ne 0) { [void]$changesDetected.Add("Enforced findings: $baseEnforced -> $curEnforced (delta $enforcedDelta)") }
if ($advisoryDelta -ne 0) { [void]$changesDetected.Add("Advisory findings: $baseAdvisory -> $curAdvisory (delta $advisoryDelta)") }
if ($documentedOnlyDelta -ne 0) { [void]$changesDetected.Add("Documented-only findings: $baseDocOnly -> $curDocOnly (delta $documentedOnlyDelta)") }
if ($newSensitive.Count -gt 0) { [void]$changesDetected.Add("New sensitive files requiring review: $($newSensitive.Count)") }
if ($allowlistIncrease) { [void]$changesDetected.Add("Bypass allowlist increased") }
if ($unallowlistedIncrease) { [void]$changesDetected.Add("Unallowlisted bypass/scope usage increased") }

$driftDetected = $changesDetected.Count -gt 0
$summaryText = if ($driftDetected) {
    "Drift detected: $($changesDetected.Count) change(s). Review recommended."
} else {
    "No drift detected compared to baseline."
}

# ---- Update baseline for next run ----
$currentSnapshot | ConvertTo-Json -Depth 4 | Set-Content -Path $BaselinePath -Encoding UTF8

# ---- Write drift report JSON ----
$driftReport = @{
    scanTimestamp = $scanTimestamp
    baselineTimestamp = $baseline.baselineTimestamp
    driftDetected = $driftDetected
    summary = $summaryText
    changesDetected = @($changesDetected)
    newSensitiveFiles = @($newSensitive)
    bypassGrowth = @{
        allowlistIncrease = $allowlistIncrease
        unallowlistedIncrease = $unallowlistedIncrease
        detail = $bypassDetail
    }
    advisoryDelta = $advisoryDelta
    enforcedDelta = $enforcedDelta
    documentedOnlyDelta = $documentedOnlyDelta
    limitations = @(
        "Baseline is single previous run; no long-term history.",
        "Advisory drift is informational only; do not fail CI on advisory increases.",
        "Bypass footprint uses health allowlist/unallowlisted counts; new bypass files are not enumerated in this report."
    )
    artifactIntegrity = $artifactIntegrity
}

$reportDir = Split-Path -Parent $DriftReportJsonPath
if (-not (Test-Path $reportDir)) { New-Item -ItemType Directory -Path $reportDir -Force | Out-Null }
$driftReport | ConvertTo-Json -Depth 4 | Set-Content -Path $DriftReportJsonPath -Encoding UTF8
Write-Host "Drift report JSON written: $DriftReportJsonPath"

# ---- Human-readable MD ----
$investigate = if ($driftDetected) { "Yes. Review the detected changes below and [PLATFORM_GUARDIAN_REPORT.md](PLATFORM_GUARDIAN_REPORT.md), [TENANT_SAFETY_CI.md](TENANT_SAFETY_CI.md) as needed." } else { "No." }
$changesSection = if ($changesDetected.Count -gt 0) {
    "`n" + (($changesDetected | ForEach-Object { "- $_" }) -join "`n") + "`n"
} else {
    " None.`n"
}
$newFilesSection = if ($newSensitive.Count -gt 0) {
    "`n" + (($newSensitive | ForEach-Object { "- ``$_``" }) -join "`n") + "`n"
} else {
    " None.`n"
}

$md = @"
# Platform Safety Drift Report

*Generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm') UTC. Do not edit by hand. Run ``./tools/architecture/run_platform_safety_drift.ps1`` after artifact regeneration.*

---

## Safety posture summary

| Metric | Baseline | Current | Delta |
|--------|----------|---------|-------|
| Enforced findings | $baseEnforced | $curEnforced | $enforcedDelta |
| Advisory findings | $baseAdvisory | $curAdvisory | $advisoryDelta |
| Documented-only findings | $baseDocOnly | $curDocOnly | $documentedOnlyDelta |

**Drift detected:** $driftDetected  
**Summary:** $summaryText

---

## Detected changes

$changesSection

---

## New sensitive files requiring review

$newFilesSection

---

## Bypass footprint

- Allowlist increased: $allowlistIncrease
- Unallowlisted bypass/scope increased: $unallowlistedIncrease
- Detail: $bypassDetail

---

## Investigation recommended?

$investigate

---

## Advisory vs enforced drift

- **Enforced drift** (increase in enforced findings) usually indicates new unallowlisted manual scope or executor gaps; CI may already fail. Fix or add justified allowlist entry; see [TENANT_SAFETY_CI.md](TENANT_SAFETY_CI.md).
- **Advisory drift** (increase in advisory findings) is for visibility only; it does **not** cause CI failure. Review new advisory findings when touching related code; do not convert advisory drift into CI failures.

Machine-readable: ``tools/architecture/platform_safety_drift_report.json``. Baseline: ``tools/architecture/platform_guardian_baseline.json``.
"@

$mdDir = Split-Path -Parent $DriftReportMdPath
if (-not (Test-Path $mdDir)) { New-Item -ItemType Directory -Path $mdDir -Force | Out-Null }
Set-Content -Path $DriftReportMdPath -Value $md -NoNewline
Write-Host "Drift report MD written: $DriftReportMdPath"
exit 0
