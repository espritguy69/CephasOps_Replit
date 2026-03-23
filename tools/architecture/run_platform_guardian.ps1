# Platform Guardian: lightweight detection and reporting for platform safety posture.
# Run from repo root: ./tools/architecture/run_platform_guardian.ps1
# Requires tenant_safety_health.json (run generate_tenant_safety_health.ps1 -EmitJson first, or use -RegenerateHealth).
# Output: tools/architecture/platform_guardian_report.json, backend/docs/operations/PLATFORM_GUARDIAN_REPORT.md
# Classification: Enforced (CI/code blocks), Advisory (heuristic; review recommended), Documented-only, Unknown.

param(
    [string]$RepoRoot = "",
    [string]$HealthJsonPath = "",
    [string]$ReportJsonPath = "",
    [string]$ReportMdPath = "",
    [switch]$RegenerateHealth
)

$ErrorActionPreference = "Stop"
$ScriptDir = if ($PSScriptRoot) { $PSScriptRoot } else { Split-Path -Parent $MyInvocation.MyCommand.Path }
$RepoRoot = if ($RepoRoot) { $RepoRoot } else { (Resolve-Path (Join-Path $ScriptDir "..\..")).Path }
$HealthJsonPath = if ($HealthJsonPath) { $HealthJsonPath } else { Join-Path $RepoRoot "tools/architecture/tenant_safety_health.json" }
$ReportJsonPath = if ($ReportJsonPath) { $ReportJsonPath } else { Join-Path $RepoRoot "tools/architecture/platform_guardian_report.json" }
$ReportMdPath = if ($ReportMdPath) { $ReportMdPath } else { Join-Path $RepoRoot "backend/docs/operations/PLATFORM_GUARDIAN_REPORT.md" }

$BackendSrc = Join-Path $RepoRoot "backend/src"
$BackendDocs = Join-Path $RepoRoot "backend/docs"

if (-not (Test-Path $BackendSrc)) { Write-Error "backend\src not found. Run from repo root."; exit 2 }

# Optionally regenerate health so guardian has current data
if ($RegenerateHealth) {
    Write-Host "Regenerating tenant safety health..."
    & (Join-Path $ScriptDir "generate_tenant_safety_health.ps1") -RepoRoot $RepoRoot -EmitJson
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

if (-not (Test-Path $HealthJsonPath)) {
    Write-Error "Health JSON not found: $HealthJsonPath. Run ./tools/architecture/generate_tenant_safety_health.ps1 -EmitJson first, or use -RegenerateHealth."
    exit 2
}

$health = Get-Content -Path $HealthJsonPath -Raw | ConvertFrom-Json
$scanTimestamp = Get-Date -Format "o"

function Normalize-PathRel($fullPath) {
    $p = $fullPath.Replace($RepoRoot + [IO.Path]::DirectorySeparatorChar, "").Replace("\", "/")
    return $p
}

# ---- A. Tenant safety ----
$tenantFindings = [System.Collections.ArrayList]::new()
$tenantEnforced = 0
$tenantAdvisory = 0

if ($health.manualScope.violationLocations -and $health.manualScope.violationLocations.Count -gt 0) {
    foreach ($v in $health.manualScope.violationLocations) {
        [void]$tenantFindings.Add([PSCustomObject]@{
            classification = "Enforced"
            category = "TenantSafety"
            summary = "Unallowlisted manual scope usage"
            file = $v.file
            line = $v.line
            kind = $v.kind
            remediation = "Use TenantScopeExecutor or add justified allowlist entry"
        })
    }
    $tenantEnforced += $health.manualScope.violationLocations.Count
}
if ($health.executorAdoption.gapFiles -and $health.executorAdoption.gapFiles.Count -gt 0) {
    foreach ($f in $health.executorAdoption.gapFiles) {
        [void]$tenantFindings.Add([PSCustomObject]@{
            classification = "Enforced"
            category = "TenantSafety"
            summary = "Orchestrator without TenantScopeExecutor (not allowlisted)"
            file = $f
            remediation = "Use TenantScopeExecutor or add to executor_not_required with reason"
        })
    }
    $tenantEnforced += $health.executorAdoption.gapFiles.Count
}

# Advisory: IgnoreQueryFilters usage (may be justified; list for review)
$pathSep = [IO.Path]::DirectorySeparatorChar
$pathSepPattern = [regex]::Escape($pathSep)
$csFiles = Get-ChildItem -Path $BackendSrc -Recurse -Filter "*.cs" -File |
    Where-Object { $_.FullName -notmatch "($pathSepPattern)(bin|obj)($pathSepPattern)" -and $_.Name -notmatch '\.(Designer|g)\.cs$' }
$ignoreQueryFiltersFiles = [System.Collections.ArrayList]::new()
foreach ($f in $csFiles) {
    $content = Get-Content -Path $f.FullName -Raw
    if ($content -match 'IgnoreQueryFilters') {
        [void]$ignoreQueryFiltersFiles.Add((Normalize-PathRel $f.FullName))
    }
}
foreach ($path in $ignoreQueryFiltersFiles) {
    [void]$tenantFindings.Add([PSCustomObject]@{
        classification = "Advisory"
        category = "TenantSafety"
        summary = "IgnoreQueryFilters usage (review for tenant scope)"
        file = $path
        remediation = "See IGNORE_QUERY_FILTERS_AUDIT.md; ensure usage is justified and scope-safe"
    })
    $tenantAdvisory++
}

# ---- B. Finance safety ----
$financeFindings = [System.Collections.ArrayList]::new()
$financeAdvisory = 0
$financeServicePatterns = @("BillingService", "PayrollService", "OrderPayoutSnapshotService", "OrderProfitabilityService", "PnlService", "RateEngineService", "PayoutAnomalyService")
foreach ($pat in $financeServicePatterns) {
    $match = Get-ChildItem -Path $BackendSrc -Recurse -Filter "*.cs" -File | Where-Object { $_.Name -eq "$pat.cs" }
    if ($match) {
        $relPath = Normalize-PathRel $match[0].FullName
        $content = Get-Content -Path $match[0].FullName -Raw
        $hasGuard = $content -match 'RequireTenantOrBypass|RequireCompany|RequireSameCompany'
        if (-not $hasGuard) {
            [void]$financeFindings.Add([PSCustomObject]@{
                classification = "Advisory"
                category = "FinanceSafety"
                summary = "Finance-sensitive service file without guard reference in file"
                file = $relPath
                remediation = "Guard may be in caller; confirm entry points use FinancialIsolationGuard (see FINANCIAL_ISOLATION_GUARD_REPORT.md)"
            })
            $financeAdvisory++
        }
    }
}

# ---- C. EventStore safety ----
$eventStoreFindings = [System.Collections.ArrayList]::new()
$eventStoreAdvisory = 0
# Canonical append is EventStoreRepository; other callers should run under scope. List files that reference AppendAsync/AppendInCurrentTransaction
foreach ($f in $csFiles) {
    $content = Get-Content -Path $f.FullName -Raw
    if ($content -match '\.(AppendAsync|AppendInCurrentTransaction)\s*\(' -and $f.Name -ne "EventStoreRepository.cs") {
        $relPath = Normalize-PathRel $f.FullName
        [void]$eventStoreFindings.Add([PSCustomObject]@{
            classification = "Advisory"
            category = "EventStoreSafety"
            summary = "References EventStore append (ensure called under tenant scope or bypass)"
            file = $relPath
            remediation = "EventStoreRepository enforces RequireTenantOrBypassForAppend; caller must set scope (see EVENTSTORE_CONSISTENCY_GUARD_REPORT.md)"
        })
        $eventStoreAdvisory++
    }
}

# ---- D. Workflow safety ----
$workflowFindings = [System.Collections.ArrayList]::new()
# Documented-only: SiWorkflowGuard and RequireRescheduleReason are in code; coverage is documented in SI_APP_WORKFLOW_HARDENING_REPORT
$siGuardPath = Join-Path $RepoRoot "backend/src/CephasOps.Application/Workflow/SiWorkflowGuard.cs"
if (Test-Path $siGuardPath) {
    $siContent = Get-Content -Path $siGuardPath -Raw
    $hasRescheduleReason = $siContent -match 'RequireRescheduleReason'
    if (-not $hasRescheduleReason) {
        [void]$workflowFindings.Add([PSCustomObject]@{
            classification = "Documented-only"
            category = "WorkflowSafety"
            summary = "Reschedule reason enforcement (check SI_APP_WORKFLOW_HARDENING_REPORT)"
            file = "backend/src/CephasOps.Application/Workflow/SiWorkflowGuard.cs"
            remediation = "Expected: RequireRescheduleReason in SiWorkflowGuard; if missing, add or document"
        })
    }
}

# ---- E. Bypass governance ----
$bypassFindings = [System.Collections.ArrayList]::new()
$bypassEnforced = 0
if ($health.manualScope.unallowlisted.enterBypass -gt 0 -or $health.manualScope.unallowlisted.exitBypass -gt 0) {
    [void]$bypassFindings.Add([PSCustomObject]@{
        classification = "Enforced"
        category = "BypassGovernance"
        summary = "Unallowlisted platform bypass usage (Enter/Exit)"
        count = $health.manualScope.unallowlisted.enterBypass + $health.manualScope.unallowlisted.exitBypass
        remediation = "Use TenantScopeExecutor.RunWithPlatformBypassAsync or add allowlist entry"
    })
    $bypassEnforced++
}
[void]$bypassFindings.Add([PSCustomObject]@{
    classification = "Documented-only"
    category = "BypassGovernance"
    summary = "Approved bypass: see tenant_safety_health.json approvedPlatformBypass and allowlist"
    allowlistPath = $health.approvedPlatformBypass.allowlistPath
})

# ---- F. Artifact and observability drift ----
$driftFindings = [System.Collections.ArrayList]::new()
$guardianDocPaths = @(
    "backend/docs/operations/PLATFORM_SAFETY_OPERATOR_RESPONSE.md",
    "backend/docs/operations/OPERATIONAL_OBSERVABILITY_INVENTORY.md",
    "backend/docs/operations/FINANCIAL_ISOLATION_GUARD_REPORT.md",
    "backend/docs/operations/EVENTSTORE_CONSISTENCY_GUARD_REPORT.md",
    "backend/docs/operations/SI_APP_WORKFLOW_HARDENING_REPORT.md",
    "backend/docs/operations/PLATFORM_SAFETY_HARDENING_INDEX.md",
    "backend/docs/operations/TENANT_SAFETY_HEALTH_DASHBOARD.md"
)
foreach ($docRel in $guardianDocPaths) {
    $full = Join-Path $RepoRoot $docRel
    if (-not (Test-Path $full)) {
        [void]$driftFindings.Add([PSCustomObject]@{
            classification = "Advisory"
            category = "ArtifactDrift"
            summary = "Expected operations doc missing"
            path = $docRel
            remediation = "Create or restore doc; run regenerate_tenant_safety_artifacts.ps1"
        })
    }
}
if (-not $health.documentation.primaryDocsExist -or -not $health.documentation.diagramInSync) {
    [void]$driftFindings.Add([PSCustomObject]@{
        classification = "Enforced"
        category = "ArtifactDrift"
        summary = "Primary docs or diagram out of sync (from health)"
        remediation = "Run generate_tenant_safety_diagram.ps1 and commit; update architecture docs"
    })
}

# Sensitive files requiring review (changed in branch)
$sensitiveFilesReview = @()
if ($health.sensitiveFiles) {
    foreach ($s in $health.sensitiveFiles) {
        if ($s.changedInBranch -eq $true) { $sensitiveFilesReview += $s.path }
    }
}

$driftEnforced = ($driftFindings | Where-Object { $_.classification -eq "Enforced" }).Count
$driftAdvisory = ($driftFindings | Where-Object { $_.classification -eq "Advisory" }).Count
$workflowDocOnly = ($workflowFindings | Where-Object { $_.classification -eq "Documented-only" }).Count
$bypassDocOnly = ($bypassFindings | Where-Object { $_.classification -eq "Documented-only" }).Count

# Build machine-readable report
$report = @{
    scanTimestamp = $scanTimestamp
    healthGeneratedAt = $health.generatedAt
    categoriesScanned = @("TenantSafety", "FinanceSafety", "EventStoreSafety", "WorkflowSafety", "BypassGovernance", "ArtifactDrift")
    summary = @{
        enforcedFindings = $tenantEnforced + $bypassEnforced + $driftEnforced
        advisoryFindings = $tenantAdvisory + $financeAdvisory + $eventStoreAdvisory + $driftAdvisory
        documentedOnlyFindings = $workflowDocOnly + $bypassDocOnly
    }
    categories = @{
        tenantSafety = @{ findings = @($tenantFindings); classificationNote = "Manual scope/executor: Enforced by CI. IgnoreQueryFilters: Advisory." }
        financeSafety = @{ findings = @($financeFindings); classificationNote = "Heuristic: files without guard reference. Guard may be in caller. Advisory." }
        eventStoreSafety = @{ findings = @($eventStoreFindings); classificationNote = "Call sites of append; repository itself is guarded. Advisory." }
        workflowSafety = @{ findings = @($workflowFindings); classificationNote = "Documented in SI_APP_WORKFLOW_HARDENING_REPORT; runtime enforced." }
        bypassGovernance = @{ findings = @($bypassFindings); classificationNote = "Unallowlisted bypass: Enforced. Allowlist: Documented-only." }
        artifactDrift = @{ findings = @($driftFindings); classificationNote = "Missing docs: Advisory. Doc/diagram sync: Enforced in CI." }
    }
    sensitiveFilesRequiringReview = @($sensitiveFilesReview)
    limitations = @(
        "Finance/EventStore guard coverage is heuristic (file-level); call graphs not analyzed.",
        "IgnoreQueryFilters list is advisory; some usages are justified (see allowlist and audit doc).",
        "Workflow completion preconditions are configuration-driven; guardian does not validate workflow definition content.",
        "Artifact staleness is inferred from health generatedAt; run regenerate_tenant_safety_artifacts.ps1 to refresh."
    )
}

# Write JSON
$reportDir = Split-Path -Parent $ReportJsonPath
if (-not (Test-Path $reportDir)) { New-Item -ItemType Directory -Path $reportDir -Force | Out-Null }
$report | ConvertTo-Json -Depth 6 | Set-Content -Path $ReportJsonPath -Encoding UTF8
Write-Host "Guardian JSON written: $ReportJsonPath"

# Human-readable report
$genDate = Get-Date -Format "yyyy-MM-dd HH:mm UTC"
$bt = [char]0x60
$enforcedCount = $report.summary.enforcedFindings
$advisoryCount = $report.summary.advisoryFindings
$docOnlyCount = $report.summary.documentedOnlyFindings
$healthGeneratedAt = $health.generatedAt

$md = @"
# Platform Guardian Report

*Generated: $genDate. Run $($bt)./tools/architecture/run_platform_guardian.ps1$($bt) from repo root (optionally with $($bt)-RegenerateHealth$($bt)). Do not edit this file by hand.*

---

## What the Platform Guardian does

The Platform Guardian is a **lightweight detection and reporting layer** for platform safety. It does **not** block builds or change business behavior. It:

- Scans the codebase and existing health artifacts for known high-risk patterns
- Compares findings against documented safeguards
- Surfaces gaps in **machine-readable** ($($bt)platform_guardian_report.json$($bt)) and **human-readable** (this report) form
- Classifies each finding as **Enforced** (CI/code blocks), **Advisory** (heuristic; review recommended), **Documented-only**, or **Unknown**

**Truthfulness:** Advisory and heuristic checks are labeled as such. No finding is presented as a hard guarantee unless it is enforced by CI or runtime code.

---

## Summary at a glance

| Classification | Count | Meaning |
|----------------|-------|--------|
| **Enforced** | $enforcedCount | Already blocked by CI or code; fix or allowlist. |
| **Advisory** | $advisoryCount | Heuristic or scan result; review recommended. |
| **Documented-only** | $docOnlyCount | Documented safeguards; no new finding. |

**Health snapshot:** Last tenant safety health generated at $($bt)$healthGeneratedAt$($bt). For latest health and score, run $($bt)./tools/architecture/generate_tenant_safety_health.ps1 -EmitJson$($bt).

---

## What is protected (enforced)

- **Tenant safety:** Unallowlisted manual scope and executor gaps → CI fails ($($bt)tenant_scope_ci.ps1$($bt)).
- **Bypass:** Unallowlisted Enter/Exit platform bypass → CI fails.
- **Artifact/doc drift:** Primary docs missing or diagram out of sync → CI fails; guard file change without doc update → CI fails.

---

## What is advisory (review recommended)

- **IgnoreQueryFilters** usage: Listed for review; some usages are justified. See $($bt)IGNORE_QUERY_FILTERS_AUDIT.md$($bt).
- **Finance-sensitive files** without guard reference in same file: Guard may be in caller; confirm entry points use FinancialIsolationGuard.
- **EventStore append call sites** outside EventStoreRepository: Caller must set tenant scope; repository enforces at append.
- **Missing operations docs:** Expected report or runbook missing; create or restore.

---

## Sensitive files requiring review

$(if ($sensitiveFilesReview.Count -gt 0) { "Sensitive safety files **changed in branch/PR** (review and update docs if needed):`n`n" + ($sensitiveFilesReview | ForEach-Object { "- ``$_``" }) -join "`n" } else { "*(None; no sensitive safety files changed in branch.)*" })

---

## Limitations (honest)

$(($report.limitations | ForEach-Object { "- $_" }) -join "`n")

---

## Platform Guardian inventory (where safeguards live)

| Area | Enforced by | Documented in | Guardian scan |
|------|-------------|---------------|---------------|
| **Tenant scope** | CI (tenant_scope_ci.ps1) + allowlist | TENANT_SAFETY_CI.md, allowlist | Uses health violations + executor gaps; IgnoreQueryFilters listed advisory |
| **Finance** | Runtime (FinancialIsolationGuard) | FINANCIAL_ISOLATION_GUARD_REPORT.md | Heuristic: finance service files without guard reference (advisory) |
| **EventStore** | Runtime (EventStoreConsistencyGuard) | EVENTSTORE_CONSISTENCY_GUARD_REPORT.md | Append call sites outside repository (advisory) |
| **Workflow** | Runtime (SiWorkflowGuard) | SI_APP_WORKFLOW_HARDENING_REPORT.md | Documented-only; no new scan |
| **Bypass** | CI (unallowlisted Enter/Exit) | allowlist, PLATFORM_SAFETY_OPERATOR_RESPONSE | Uses health; approved bypass from allowlist |
| **Artifact drift** | CI (docs/diagram sync) | OPERATIONAL_OBSERVABILITY_INVENTORY, reports | Missing ops docs (advisory); doc/diagram sync from health |

**Cannot be scanned reliably:** Call-graph validation, workflow definition content, runtime guard coverage (guardian is static/heuristic only).

---

## How to use this report

1. **Enforced findings:** Fix violations or add justified allowlist entry; see [TENANT_SAFETY_CI.md](TENANT_SAFETY_CI.md) and [PLATFORM_SAFETY_OPERATOR_RESPONSE.md](PLATFORM_SAFETY_OPERATOR_RESPONSE.md).
2. **Advisory findings:** Review listed files/paths; confirm guard coverage or document exception.
3. **Regeneration:** To refresh health and guardian together: $($bt)./tools/architecture/regenerate_tenant_safety_artifacts.ps1$($bt) then $($bt)./tools/architecture/run_platform_guardian.ps1$($bt). Or run guardian with $($bt)-RegenerateHealth$($bt).

Machine-readable details: $($bt)tools/architecture/platform_guardian_report.json$($bt).
"@

$mdDir = Split-Path -Parent $ReportMdPath
if (-not (Test-Path $mdDir)) { New-Item -ItemType Directory -Path $mdDir -Force | Out-Null }
Set-Content -Path $ReportMdPath -Value $md -NoNewline
Write-Host "Guardian report written: $ReportMdPath"

exit 0
