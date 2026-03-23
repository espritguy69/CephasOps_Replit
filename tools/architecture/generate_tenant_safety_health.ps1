# Generate Tenant Safety Architecture Health Dashboard and optional JSON report.
# Run from repo root: ./tools/architecture/generate_tenant_safety_health.ps1
# Output: backend/docs/operations/TENANT_SAFETY_HEALTH_DASHBOARD.md, optionally tools/architecture/tenant_safety_health.json
# Uses same scanning rules as tenant_scope_ci.ps1. See backend/docs/operations/TENANT_SAFETY_CI.md and tools/tenant_safety_ci_allowlist.json

param(
    [string]$RepoRoot = "",
    [string]$AllowlistPath = "",
    [string]$DashboardPath = "",
    [string]$JsonPath = "",
    [string]$HistoryPath = "",
    [string[]]$ChangedFiles = @(),
    [switch]$EmitJson
)

$ErrorActionPreference = "Stop"

$ScriptDir = if ($PSScriptRoot) { $PSScriptRoot } else { Split-Path -Parent $MyInvocation.MyCommand.Path }
$RepoRoot = if ($RepoRoot) { $RepoRoot } else { (Resolve-Path (Join-Path $ScriptDir "..\..")).Path }
$AllowlistPath = if ($AllowlistPath) { $AllowlistPath } else { Join-Path $RepoRoot "tools/tenant_safety_ci_allowlist.json" }
$DashboardPath = if ($DashboardPath) { $DashboardPath } else { Join-Path $RepoRoot "backend/docs/operations/TENANT_SAFETY_HEALTH_DASHBOARD.md" }
$JsonPath = if ($JsonPath) { $JsonPath } else { Join-Path $RepoRoot "tools/architecture/tenant_safety_health.json" }
$HistoryPath = if ($HistoryPath) { $HistoryPath } else { Join-Path $RepoRoot "tools/architecture/tenant_safety_history.json" }

$BackendSrc = Join-Path $RepoRoot "backend/src"
$BackendDocs = Join-Path $RepoRoot "backend/docs"

if (-not (Test-Path $BackendSrc)) { Write-Error "backend\src not found. Run from repo root."; exit 2 }
if (-not (Test-Path $AllowlistPath)) { Write-Error "Allowlist not found: $AllowlistPath"; exit 2 }

$allowlist = Get-Content -Path $AllowlistPath -Raw | ConvertFrom-Json

function Normalize-PathRel($fullPath) {
    $p = $fullPath.Replace($RepoRoot + [IO.Path]::DirectorySeparatorChar, "").Replace("\", "/")
    return $p
}

# Build allowlist sets (same as tenant_scope_ci.ps1)
$manualScopeAllowed = @{}
foreach ($e in $allowlist.manual_scope_allowed) { $manualScopeAllowed[$e.path.Replace("\", "/")] = $e.reason }
$executorNotRequired = @{}
foreach ($e in $allowlist.executor_not_required) { $executorNotRequired[$e.path] = $e.reason }
$guardFileNames = @($allowlist.guard_files)
$architectureDocPaths = @($allowlist.architecture_doc_paths)

# ---- 1. Manual Scope Health ----
$manualScopeCurrentTenantId = 0
$manualScopeEnterBypass = 0
$manualScopeExitBypass = 0
$manualScopeInAllowlisted = @{ CurrentTenantId = 0; EnterBypass = 0; ExitBypass = 0 }

$pathSep = [IO.Path]::DirectorySeparatorChar
$pathSepPattern = [regex]::Escape($pathSep)
$csFiles = Get-ChildItem -Path $BackendSrc -Recurse -Filter "*.cs" -File |
    Where-Object { $_.FullName -notmatch "($pathSepPattern)(bin|obj)($pathSepPattern)" -and $_.Name -notmatch '\.(Designer|g)\.cs$' }
$manualScopeViolations = [System.Collections.ArrayList]::new()
foreach ($f in $csFiles) {
    $relPath = Normalize-PathRel $f.FullName
    $isAllowlisted = $manualScopeAllowed.ContainsKey($relPath)
    $lines = Get-Content -Path $f.FullName
    $lineNum = 0
    foreach ($line in $lines) {
        $lineNum++
        $trimmed = $line.Trim()
        if ($trimmed -match '^(//|///|\*|/\*|#)') { continue }
        if ($line -match 'TenantScope\.CurrentTenantId') {
            if ($isAllowlisted) { $manualScopeInAllowlisted.CurrentTenantId++ } else {
                $manualScopeCurrentTenantId++
                [void]$manualScopeViolations.Add([PSCustomObject]@{ file = $relPath; line = $lineNum; kind = "CurrentTenantId" })
            }
        }
        if ($line -match '\.EnterPlatformBypass\s*\(\s*\)' -or $line -match 'TenantSafetyGuard\.EnterPlatformBypass') {
            if ($isAllowlisted) { $manualScopeInAllowlisted.EnterBypass++ } else {
                $manualScopeEnterBypass++
                [void]$manualScopeViolations.Add([PSCustomObject]@{ file = $relPath; line = $lineNum; kind = "EnterPlatformBypass" })
            }
        }
        if ($line -match '\.ExitPlatformBypass\s*\(\s*\)' -or $line -match 'TenantSafetyGuard\.ExitPlatformBypass') {
            if ($isAllowlisted) { $manualScopeInAllowlisted.ExitBypass++ } else {
                $manualScopeExitBypass++
                [void]$manualScopeViolations.Add([PSCustomObject]@{ file = $relPath; line = $lineNum; kind = "ExitPlatformBypass" })
            }
        }
    }
}

# ---- 2. Executor Adoption Health ----
$orchestratorsTotal = 0
$orchestratorsWithExecutor = 0
$orchestratorsAllowlisted = 0
$orchestratorList = [System.Collections.ArrayList]::new()

foreach ($f in $csFiles) {
    $content = Get-Content -Path $f.FullName -Raw
    $name = $f.Name
    $relPath = Normalize-PathRel $f.FullName
    if (-not ($content -match ':\s*BackgroundService' -or $content -match ':\s*IHostedService')) { continue }
    $orchestratorsTotal++
    $hasExecutor = $content -match 'TenantScopeExecutor'
    $allowed = $false
    foreach ($key in $executorNotRequired.Keys) {
        if ($relPath -match [regex]::Escape($key) -or $name -match [regex]::Escape($key)) { $allowed = $true; break }
    }
    if ($allowed) { $orchestratorsAllowlisted++ }
    if ($hasExecutor) { $orchestratorsWithExecutor++ }
    [void]$orchestratorList.Add([PSCustomObject]@{
        File = $relPath
        UsesExecutor = $hasExecutor
        Allowlisted = $allowed
    })
}
$executorGapFiles = @($orchestratorList | Where-Object { -not $_.UsesExecutor -and -not $_.Allowlisted } | ForEach-Object { $_.File })

# ---- 3. Sensitive Safety Files ----
$sensitiveFiles = @(
    @{ Name = "TenantSafetyGuard"; Path = "backend/src/CephasOps.Infrastructure/Persistence/TenantSafetyGuard.cs" },
    @{ Name = "TenantScopeExecutor"; Path = "backend/src/CephasOps.Infrastructure/Persistence/TenantScopeExecutor.cs" },
    @{ Name = "SiWorkflowGuard"; Path = "backend/src/CephasOps.Application/Workflow/SiWorkflowGuard.cs" },
    @{ Name = "FinancialIsolationGuard"; Path = "backend/src/CephasOps.Application/Common/FinancialIsolationGuard.cs" },
    @{ Name = "EventStoreConsistencyGuard"; Path = "backend/src/CephasOps.Infrastructure/Persistence/EventStoreConsistencyGuard.cs" }
)
$changedFiles = $ChangedFiles
if ($changedFiles.Count -eq 0 -and $env:GITHUB_ACTIONS -eq "true") {
    try {
        $base = if ($env:GITHUB_BASE_REF) { "origin/$env:GITHUB_BASE_REF" } else { "origin/main" }
        $changedFiles = @(git -C $RepoRoot diff --name-only "$base...HEAD" 2>$null)
        if (-not $changedFiles) { $changedFiles = @(git -C $RepoRoot diff --name-only "HEAD~1..HEAD" 2>$null) }
    } catch { }
}
foreach ($s in $sensitiveFiles) {
    $normPath = $s.Path.Replace("\", "/")
    $normChanged = $changedFiles | ForEach-Object { $_.Replace("\", "/") }
    $s.ChangedInBranch = @($normChanged | Where-Object { $_ -eq $normPath -or $_ -like "*/$($s.Name)" }).Count -gt 0
}

# ---- 4. Documentation Health ----
$securityDocName = "SECURITY_AND_TENANT_SAFETY_ARCHITECTURE.md"
$docSecurity = $null
foreach ($p in $architectureDocPaths) {
    if ($p -match [regex]::Escape($securityDocName)) { $docSecurity = Join-Path $RepoRoot ($p.Replace("\", "/")); break }
}
if (-not $docSecurity) { $docSecurity = Join-Path $RepoRoot "backend/docs/architecture/$securityDocName" }
$docDiagramInSync = $false
if (Test-Path $docSecurity) {
    $docContent = Get-Content -Path $docSecurity -Raw
    $docDiagramInSync = $docContent -match '<!-- BEGIN GENERATED: tenant_safety_diagram -->' -and $docContent -match '```mermaid'
}
$primaryDocsExist = $true
$primaryDocsList = @()
foreach ($docRel in $architectureDocPaths) {
    $full = Join-Path $RepoRoot $docRel
    $exists = Test-Path $full
    $primaryDocsExist = $primaryDocsExist -and $exists
    $primaryDocsList += [PSCustomObject]@{ Path = $docRel; Exists = $exists }
}
$guardDocDriftConfigured = Test-Path (Join-Path $RepoRoot "tools/tenant_scope_ci.ps1")

# ---- 5. Allowlist Health ----
$allowlistManualCount = $allowlist.manual_scope_allowed.Count
$allowlistExecutorCount = $allowlist.executor_not_required.Count
$allowlistTotal = $allowlistManualCount + $allowlistExecutorCount

$violationCount = $manualScopeCurrentTenantId + $manualScopeEnterBypass + $manualScopeExitBypass
$executorGap = $orchestratorsTotal - $orchestratorsWithExecutor - $orchestratorsAllowlisted

# ---- 5b. Load history for baseline (allowlist increase) and trend ----
$historyRecords = @()
$lastAllowlistTotal = $null
if ($EmitJson -and (Test-Path $HistoryPath)) {
    try {
        $historyObj = Get-Content -Path $HistoryPath -Raw | ConvertFrom-Json
        if ($historyObj.history) {
            $historyRecords = @($historyObj.history)
            if ($historyRecords.Count -gt 0) {
                $last = $historyRecords[-1]
                if ($null -ne $last.allowlistTotal) { $lastAllowlistTotal = $last.allowlistTotal }
            }
        }
    } catch { $historyRecords = @() }
}

# ---- 6. Safety Score and breakdown ----
$allowlistIncreased = ($null -ne $lastAllowlistTotal) -and ($allowlistTotal -gt $lastAllowlistTotal)
$docIntegrityFail = -not ($primaryDocsExist -and $docDiagramInSync -and $guardDocDriftConfigured)
$sensitiveFilesChangedCount = @($sensitiveFiles | Where-Object { $_.ChangedInBranch }).Count
$deductManualScope = if ($violationCount -gt 0) { 40 } else { 0 }
$deductExecutorGap = if ($executorGap -gt 0) { 20 } else { 0 }
$deductAllowlistIncrease = if ($allowlistIncreased) { 10 } else { 0 }
$deductDocumentationIntegrity = if ($docIntegrityFail) { 10 } else { 0 }
$deductSensitiveFilesChanged = if ($sensitiveFilesChangedCount -gt 0) { 5 } else { 0 }
$totalDeductions = $deductManualScope + $deductExecutorGap + $deductAllowlistIncrease + $deductDocumentationIntegrity + $deductSensitiveFilesChanged
$safetyScore = [Math]::Max(0, 100 - $totalDeductions)

# ---- 7. Test Health ----
$analyzerTestsPath = Join-Path $RepoRoot "analyzers/CephasOps.TenantSafetyAnalyzers.Tests"
$analyzerTestsExist = Test-Path $analyzerTestsPath
$tenantSafetyCiRef = "`.github/workflows/tenant-safety.yml` runs: analyzer tests (Release), API build with CEPHAS001+CEPHAS004 as errors, `tenant_safety_audit.ps1`, `tenant_scope_ci.ps1`."

# ---- Overall summary ----
$bt = [char]0x60  # backtick for code in markdown (used in summary and dashboard)
if ($violationCount -eq 0 -and $executorGap -eq 0 -and $primaryDocsExist -and $docDiagramInSync) {
    $overallStatus = "Stable"
    $overallSummary = "All health indicators are within expected bounds: no unallowlisted manual scope usage, all runtime orchestrators use TenantScopeExecutor or are allowlisted, primary docs exist, and the architecture diagram is generated and in sync."
} elseif ($violationCount -gt 0 -or $executorGap -gt 0) {
    $overallStatus = "Drifting"
    $overallSummary = "One or more indicators need attention: unallowlisted manual scope or bypass usage, or runtime orchestrators not using TenantScopeExecutor. See the table below and the Tracked exceptions section. Run $($bt)tenant_scope_ci.ps1$($bt) for enforcement details."
} else {
    $overallStatus = "Stable"
    $overallSummary = "Scope and executor health look good. Ensure docs and diagram stay in sync (see Documentation Health)."
}

# ---- Trend from history (after appending current run when EmitJson) ----
$trend = "Stable"
if ($EmitJson) {
    $executorCoverage = "$orchestratorsWithExecutor/$orchestratorsTotal"
    $newRecord = @{
        date = (Get-Date -Format "yyyy-MM-ddTHH:mm:ssZ")
        score = $safetyScore
        manualScope = $violationCount
        executorCoverage = $executorCoverage
        allowlistTotal = $allowlistTotal
    }
    $historyRecords += $newRecord
    $maxHistory = 50
    if ($historyRecords.Count -gt $maxHistory) {
        $historyRecords = $historyRecords[($historyRecords.Count - $maxHistory)..($historyRecords.Count - 1)]
    }
    $historyObj = @{ history = @($historyRecords) }
    $historyDir = Split-Path -Parent $HistoryPath
    if (-not (Test-Path $historyDir)) { New-Item -ItemType Directory -Path $historyDir -Force | Out-Null }
    $historyObj | ConvertTo-Json -Depth 3 | Set-Content -Path $HistoryPath -Encoding UTF8
    Write-Host "History written: $HistoryPath ($($historyRecords.Count) entries)"
    # Trend from last 5 runs: compare current score to average of prior up to 4 runs
    $priorRuns = [Math]::Min(4, $historyRecords.Count - 1)
    if ($priorRuns -ge 1) {
        $priorScores = @()
        for ($i = 1; $i -le $priorRuns; $i++) {
            $priorScores += $historyRecords[$historyRecords.Count - 1 - $i].score
        }
        $priorAvg = ($priorScores | Measure-Object -Average).Average
        if ($safetyScore -gt $priorAvg) { $trend = "Improving" }
        elseif ($safetyScore -lt $priorAvg) { $trend = "Declining" }
    }
}

# ---- Score breakdown explanation (human-readable) ----
if ($totalDeductions -eq 0) {
    $scoreBreakdownExplanation = "No deductions applied; score is 100."
} else {
    $parts = @()
    if ($deductManualScope -gt 0) { $parts += "unallowlisted manual scope (-$deductManualScope)" }
    if ($deductExecutorGap -gt 0) { $parts += "executor gap (-$deductExecutorGap)" }
    if ($deductAllowlistIncrease -gt 0) { $parts += "allowlist increase (-$deductAllowlistIncrease)" }
    if ($deductDocumentationIntegrity -gt 0) { $parts += "documentation integrity (-$deductDocumentationIntegrity)" }
    if ($deductSensitiveFilesChanged -gt 0) { $parts += "sensitive files changed in branch (-$deductSensitiveFilesChanged)" }
    $scoreBreakdownExplanation = "Deductions applied: " + ($parts -join "; ") + "."
}

# ---- Build dashboard Markdown ----
$genDate = Get-Date -Format "yyyy-MM-dd HH:mm UTC"
$md = @"
# Tenant Safety Architecture Health Dashboard

*Generated: $genDate. Run $($bt)./tools/architecture/generate_tenant_safety_health.ps1$($bt) from the repo root to refresh. Do not edit this file by hand.*

---

## Overall summary

| Tenant Safety Score | Trend | Status |
|---------------------|-------|--------|
| **$safetyScore/100** | $trend | $overallStatus |

$overallSummary

**Scoring:** Start at 100; apply deductions below. **Trend:** Improving if current score &gt; average of prior up to 4 runs; Declining if &lt; average; Stable otherwise (from $($bt)tenant_safety_history.json$($bt), last 50 entries). **Drift alert:** CI fails if the score drops below the previous run. To allow an intentional drop, update $($bt)backend/docs/operations/TENANT_SAFETY_DRIFT_LOG.md$($bt) in the same PR (see that file).

### Score breakdown

| Deduction | Points | Applied |
|-----------|--------|---------|
| Unallowlisted manual scope | 40 | $deductManualScope |
| Executor gap (orchestrator without TenantScopeExecutor) | 20 | $deductExecutorGap |
| Allowlist size increased vs baseline | 10 | $deductAllowlistIncrease |
| Documentation integrity (docs/diagram/drift rule) | 10 | $deductDocumentationIntegrity |
| Sensitive safety files changed in branch | 5 | $deductSensitiveFilesChanged |
| **Total deductions** | | **$totalDeductions** |

$scoreBreakdownExplanation

---

## Health indicators

| Indicator | Value | Notes |
|-----------|-------|-------|
| **Manual scope (unallowlisted)** | CurrentTenantId: $manualScopeCurrentTenantId, EnterBypass: $manualScopeEnterBypass, ExitBypass: $manualScopeExitBypass | In $($bt)backend/src$($bt) runtime code only; exclude allowlisted. Zero is healthy. |
| **Manual scope (allowlisted)** | CurrentTenantId: $($manualScopeInAllowlisted.CurrentTenantId), EnterBypass: $($manualScopeInAllowlisted.EnterBypass), ExitBypass: $($manualScopeInAllowlisted.ExitBypass) | Tracked exceptions; see allowlist. |
| **Executor adoption** | $orchestratorsWithExecutor / $orchestratorsTotal use TenantScopeExecutor; $orchestratorsAllowlisted allowlisted as executor_not_required | Runtime orchestrators (BackgroundService/IHostedService). |
| **Sensitive safety files** | 5 tracked (see below) | TenantSafetyGuard, TenantScopeExecutor, SiWorkflowGuard, FinancialIsolationGuard, EventStoreConsistencyGuard. |
| **Documentation** | Diagram in sync: $docDiagramInSync; Primary docs exist: $primaryDocsExist; Guard/doc drift rule: $guardDocDriftConfigured | Diagram from manifest; drift check in $($bt)tenant_scope_ci.ps1$($bt). |
| **Allowlist** | manual_scope_allowed: $allowlistManualCount; executor_not_required: $allowlistExecutorCount; total entries: $allowlistTotal | Warn if counts increase without justification. |
| **Test suite** | Analyzer tests path exists: $analyzerTestsExist | $($bt).github/workflows/tenant-safety.yml$($bt) runs: analyzer tests, API build CEPHAS001+CEPHAS004, $($bt)tenant_safety_audit.ps1$($bt), $($bt)tenant_scope_ci.ps1$($bt). |

---

## Sensitive safety files (tracked)

| File | Repo path | Changed in branch/PR |
|------|-----------|----------------------|
| TenantSafetyGuard | $($bt)backend/src/CephasOps.Infrastructure/Persistence/TenantSafetyGuard.cs$($bt) | $($sensitiveFiles[0].ChangedInBranch) |
| TenantScopeExecutor | $($bt)backend/src/CephasOps.Infrastructure/Persistence/TenantScopeExecutor.cs$($bt) | $($sensitiveFiles[1].ChangedInBranch) |
| SiWorkflowGuard | $($bt)backend/src/CephasOps.Application/Workflow/SiWorkflowGuard.cs$($bt) | $($sensitiveFiles[2].ChangedInBranch) |
| FinancialIsolationGuard | $($bt)backend/src/CephasOps.Application/Common/FinancialIsolationGuard.cs$($bt) | $($sensitiveFiles[3].ChangedInBranch) |
| EventStoreConsistencyGuard | $($bt)backend/src/CephasOps.Infrastructure/Persistence/EventStoreConsistencyGuard.cs$($bt) | $($sensitiveFiles[4].ChangedInBranch) |

*"Changed in branch/PR" is set when the script runs in CI or when $($bt)-ChangedFiles$($bt) is passed and the file appears in the changed set.*

---

## Primary tenant-safety docs

| Doc | Exists |
|-----|--------|
$(($primaryDocsList | ForEach-Object { "| $($bt)$($_.Path)$($bt) | $($_.Exists) |" }) -join "`n")

---

## Tracked exceptions (allowlist summary)

- **manual_scope_allowed:** $allowlistManualCount paths (bootstrap, design-time, executor/guard implementation, middleware, and documented runtime exceptions).
- **executor_not_required:** $allowlistExecutorCount components (platform-only or delegating to a service that uses TenantScopeExecutor).

Full list and reasons: $($bt)tools/tenant_safety_ci_allowlist.json$($bt). Do not add entries to avoid fixing a real violation.

---

## Autonomous remediation (for tooling / agents)

When violations exist, use $($bt)tenant_safety_health.json$($bt) (generated with $($bt)-EmitJson$($bt)) for machine-readable locations:

- **manualScope.violationLocations:** array of $($bt){ file, line, kind }$($bt) for each unallowlisted manual scope usage (CurrentTenantId, EnterPlatformBypass, ExitPlatformBypass). Fix by refactoring to TenantScopeExecutor or adding a justified allowlist entry.
- **executorAdoption.gapFiles:** list of orchestrator file paths that do not use TenantScopeExecutor and are not allowlisted. Fix by using TenantScopeExecutor or adding to $($bt)executor_not_required$($bt) with a reason.

Run $($bt)./tools/architecture/generate_tenant_safety_health.ps1 -EmitJson$($bt) from repo root to refresh the JSON and this dashboard.

---

## What to do if this changes

1. **Manual scope (unallowlisted) > 0:** Run $($bt)./tools/tenant_scope_ci.ps1$($bt); fix violations by using TenantScopeExecutor or by adding a justified entry to the allowlist.
2. **Executor adoption gap:** Ensure every BackgroundService/IHostedService either uses TenantScopeExecutor or is in $($bt)executor_not_required$($bt) with a reason.
3. **Sensitive file changed:** If you changed a guard or TenantScopeExecutor, update architecture docs in the same PR (see $($bt)tenant_scope_ci.ps1$($bt) GUARD_DOC_DRIFT).
4. **Diagram or docs missing/out of sync:** Run $($bt)./tools/architecture/generate_tenant_safety_diagram.ps1$($bt) and commit the doc; ensure $($bt)backend/docs/architecture/*.md$($bt) exist.
5. **Allowlist counts increased:** Review that each new entry has a clear reason; prefer refactoring to allowlist growth.
6. **Test suite:** Keep $($bt)analyzers/CephasOps.TenantSafetyAnalyzers.Tests$($bt) and the tenant-safety workflow passing; do not disable CEPHAS001/CEPHAS004.

See [TENANT_SAFETY_CI.md](TENANT_SAFETY_CI.md) and [TENANT_SAFETY_DEVELOPER_GUIDE.md](../architecture/TENANT_SAFETY_DEVELOPER_GUIDE.md).

---

## Guards and coverage (documented)

| Guard | Protected paths | Report |
|-------|------------------|--------|
| **FinancialIsolationGuard** | BillingService, PayrollService, OrderPayoutSnapshotService, OrderProfitabilityService, PnlService, RateEngineService, PayoutAnomalyService | [FINANCIAL_ISOLATION_GUARD_REPORT.md](FINANCIAL_ISOLATION_GUARD_REPORT.md) |
| **EventStoreConsistencyGuard** | EventStoreRepository.AppendAsync, AppendInCurrentTransaction | [EVENTSTORE_CONSISTENCY_GUARD_REPORT.md](EVENTSTORE_CONSISTENCY_GUARD_REPORT.md) |

Both are **runtime-enforced**; CI does not auto-detect guard failures. Guard violations are visible at runtime (exceptions, and in-memory buffer in operations overview). See [PLATFORM_SAFETY_OPERATOR_RESPONSE.md](PLATFORM_SAFETY_OPERATOR_RESPONSE.md) for what to do when a safeguard fails.

---

## Approved platform bypass

- **Summary:** Platform bypass is allowed only via $($bt)TenantScopeExecutor.RunWithPlatformBypassAsync$($bt) or paths listed in the allowlist.
- **Allowlist:** $($bt)tools/tenant_safety_ci_allowlist.json$($bt) — $($bt)manual_scope_allowed$($bt) (e.g. bootstrap, design-time, executor/guard implementation, middleware) and $($bt)executor_not_required$($bt) (hosted services that do not touch tenant data or delegate to executor).
- **Unexpected bypass usage:** If you see bypass in a new path, add an allowlist entry with a reason or refactor to TenantScopeExecutor. See [PLATFORM_SAFETY_OPERATOR_RESPONSE.md](PLATFORM_SAFETY_OPERATOR_RESPONSE.md).

---

## When a safeguard fails (operator response)

See **[PLATFORM_SAFETY_OPERATOR_RESPONSE.md](PLATFORM_SAFETY_OPERATOR_RESPONSE.md)** for concise guidance on:

- Missing tenant context (TenantSafetyGuard / RequireTenantOrBypass)
- Financial isolation guard exceptions
- EventStore consistency guard exceptions
- Artifact drift (score drop, stale dashboard/JSON)
- Platform bypass misuse or unexpected bypass usage

---

## Limitations (enforced vs advisory)

| Area | Enforced | Advisory / discipline |
|------|----------|------------------------|
| Manual scope (unallowlisted) | CI fails; $($bt)tenant_scope_ci.ps1$($bt) | — |
| Executor adoption | CI fails if orchestrator not using executor and not allowlisted | — |
| Guard/doc drift | CI fails if guard file changed and no doc update | — |
| Finance / EventStore guard failures | Runtime only (guard throws) | Not detected by CI; rely on runtime logs and operations overview |
| EventStore append scope | Runtime (RequireTenantOrBypassForAppend) | Analyzer does not check EventStore call sites |
| Allowlist growth | CI warns via score/allowlist count | Prefer refactoring over new allowlist entries |

For what is surfaced vs missing in observability, see [OPERATIONAL_OBSERVABILITY_INVENTORY.md](OPERATIONAL_OBSERVABILITY_INVENTORY.md).

"@

# Ensure directory exists
$dashboardDir = Split-Path -Parent $DashboardPath
if (-not (Test-Path $dashboardDir)) { New-Item -ItemType Directory -Path $dashboardDir -Force | Out-Null }
Set-Content -Path $DashboardPath -Value $md -NoNewline
Write-Host "Dashboard written: $DashboardPath"

# ---- Optional JSON ----
if ($EmitJson) {
    $json = @{
        generatedAt = (Get-Date -Format "o")
        safetyScore = $safetyScore
        trend = $trend
        deductions = @{
            manualScope = $deductManualScope
            executorGap = $deductExecutorGap
            allowlistIncrease = $deductAllowlistIncrease
            documentationIntegrity = $deductDocumentationIntegrity
            sensitiveFilesChanged = $deductSensitiveFilesChanged
        }
        overallStatus = $overallStatus
        manualScope = @{
            unallowlisted = @{ currentTenantId = $manualScopeCurrentTenantId; enterBypass = $manualScopeEnterBypass; exitBypass = $manualScopeExitBypass }
            allowlisted = @{ currentTenantId = $manualScopeInAllowlisted.CurrentTenantId; enterBypass = $manualScopeInAllowlisted.EnterBypass; exitBypass = $manualScopeInAllowlisted.ExitBypass }
            violationLocations = @($manualScopeViolations | ForEach-Object { @{ file = $_.file; line = $_.line; kind = $_.kind } })
        }
        executorAdoption = @{
            total = $orchestratorsTotal
            withExecutor = $orchestratorsWithExecutor
            allowlisted = $orchestratorsAllowlisted
            gapFiles = @($executorGapFiles)
        }
        sensitiveFiles = @($sensitiveFiles | ForEach-Object { @{ name = $_.Name; path = $_.Path; changedInBranch = $_.ChangedInBranch } })
        documentation = @{
            diagramInSync = $docDiagramInSync
            primaryDocsExist = $primaryDocsExist
            guardDocDriftConfigured = $guardDocDriftConfigured
        }
        allowlist = @{
            manualScopeAllowed = $allowlistManualCount
            executorNotRequired = $allowlistExecutorCount
        }
        testHealth = @{
            analyzerTestsExist = $analyzerTestsExist
        }
        sensitiveFilesChanged = @($sensitiveFiles | Where-Object { $_.ChangedInBranch } | ForEach-Object { $_.Name })
        guards = @{
            financialIsolation = @{
                documentedReport = "backend/docs/operations/FINANCIAL_ISOLATION_GUARD_REPORT.md"
                coverage = "BillingService, PayrollService, OrderPayoutSnapshotService, OrderProfitabilityService, PnlService, RateEngineService, PayoutAnomalyService"
                enforcement = "runtime"
            }
            eventStoreConsistency = @{
                documentedReport = "backend/docs/operations/EVENTSTORE_CONSISTENCY_GUARD_REPORT.md"
                coverage = "EventStoreRepository.AppendAsync, AppendInCurrentTransaction"
                enforcement = "runtime"
            }
        }
        approvedPlatformBypass = @{
            summary = "manual_scope_allowed and executor_not_required in allowlist"
            allowlistPath = "tools/tenant_safety_ci_allowlist.json"
            note = "Bypass only via TenantScopeExecutor.RunWithPlatformBypassAsync or allowlisted paths"
        }
        enforcementLimitations = @(
            "Guard failures (finance, EventStore) are runtime-only; not auto-detected in CI.",
            "EventStore append/replay scope is not verified by analyzer; EventStoreConsistencyGuard is runtime-only.",
            "Approved bypass usage is allowlisted; new bypass locations require allowlist entry with reason."
        )
    }
    $jsonDir = Split-Path -Parent $JsonPath
    if (-not (Test-Path $jsonDir)) { New-Item -ItemType Directory -Path $jsonDir -Force | Out-Null }
    $json | ConvertTo-Json -Depth 5 | Set-Content -Path $JsonPath -Encoding UTF8
    Write-Host "JSON written: $JsonPath"
}

exit 0
