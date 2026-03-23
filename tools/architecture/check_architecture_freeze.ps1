# Architecture Freeze Check — fail if a frozen file changed without an override.
# Run from repo root. Usage: .\tools\architecture\check_architecture_freeze.ps1 -ChangedFiles @("path1", "path2")
# Or from workflow: pass changed files from git diff. Exit 0 = pass, 1 = fail (frozen file changed, no valid override).
# See backend/docs/operations/ARCHITECTURE_FREEZE.md and ARCHITECTURE_FREEZE_OVERRIDE.md

param(
    [string[]]$ChangedFiles = @()
)

$ErrorActionPreference = "Stop"

$RepoRoot = if ($PSScriptRoot) { Split-Path -Parent $PSScriptRoot } else { Get-Location }
if ((Split-Path -Leaf $RepoRoot) -eq "tools") { $RepoRoot = Split-Path -Parent $RepoRoot }

$OverrideDocPath = "backend/docs/operations/ARCHITECTURE_FREEZE_OVERRIDE.md"

# Frozen paths (repo-relative, forward slash). Changes to any of these require override doc update.
$FrozenPaths = @(
    "backend/src/CephasOps.Infrastructure/Persistence/TenantSafetyGuard.cs",
    "backend/src/CephasOps.Infrastructure/Persistence/TenantScope.cs",
    "backend/src/CephasOps.Infrastructure/Persistence/TenantScopeExecutor.cs",
    "backend/src/CephasOps.Application/Workflow/SiWorkflowGuard.cs",
    "backend/src/CephasOps.Application/Common/FinancialIsolationGuard.cs",
    "backend/src/CephasOps.Infrastructure/Persistence/EventStoreConsistencyGuard.cs",
    ".github/workflows/tenant-safety.yml",
    "tools/tenant_safety_audit.ps1",
    "tools/tenant_scope_ci.ps1",
    "tools/tenant_safety_ci_allowlist.json",
    "tools/architecture/generate_tenant_safety_health.ps1",
    "tools/architecture/regenerate_tenant_safety_artifacts.ps1",
    "tools/architecture/run_platform_guardian.ps1",
    "tools/architecture/generate_tenant_safety_diagram.ps1",
    "backend/docs/operations/TENANT_SAFETY_CI.md",
    "backend/docs/operations/TENANT_SAFETY_DRIFT_LOG.md",
    "backend/docs/operations/TENANT_SAFETY_HEALTH_DASHBOARD.md",
    "backend/docs/operations/TENANT_SAFETY_ANALYZER.md",
    "backend/docs/operations/PLATFORM_SAFETY_OPERATOR_RESPONSE.md",
    "backend/docs/operations/PLATFORM_SAFETY_HARDENING_INDEX.md",
    "backend/docs/architecture/TENANT_SAFETY_DEVELOPER_GUIDE.md",
    "backend/docs/architecture/SECURITY_AND_TENANT_SAFETY_ARCHITECTURE.md",
    "backend/docs/architecture/CEPHASOPS_PLATFORM_STANDARDS.md"
)

function Normalize-PathRel($p) {
    $p = $p.Replace("\", "/").Trim()
    if ($p -match "^\./") { $p = $p.Substring(2) }
    return $p
}

$frozenSet = @{}
foreach ($fp in $FrozenPaths) {
    $key = Normalize-PathRel $fp
    $frozenSet[$key] = $true
}

if ($ChangedFiles.Count -eq 0) {
    $base = if ($env:GITHUB_BASE_REF) { "origin/$env:GITHUB_BASE_REF" } else { "origin/main" }
    $changed = @(git -C $RepoRoot diff --name-only "$base...HEAD" 2>$null)
    if (-not $changed) { $changed = @(git -C $RepoRoot diff --name-only "HEAD~1..HEAD" 2>$null) }
    $ChangedFiles = $changed
}

$normalizedChanged = @($ChangedFiles | ForEach-Object { Normalize-PathRel $_ })
$frozenChanged = @($normalizedChanged | Where-Object { $frozenSet.ContainsKey($_) })

if ($frozenChanged.Count -eq 0) {
    exit 0
}

# At least one frozen file changed. Override doc must be changed with a valid new row.
$overrideInChanged = $normalizedChanged -contains $OverrideDocPath
if (-not $overrideInChanged) {
    Write-Host "::error::Architecture freeze: the following frozen file(s) were changed but ARCHITECTURE_FREEZE_OVERRIDE.md was not updated in this PR. Add a row to $OverrideDocPath (Date | Files/scope | Reason/PR) and commit. See backend/docs/operations/ARCHITECTURE_FREEZE.md." -ForegroundColor Red
    Write-Host "Frozen files changed: $($frozenChanged -join ', ')"
    exit 1
}

# Validate override doc: diff must contain an added line that looks like a table row (| date | scope | reason |)
$datePattern = "[0-9]{4}-[0-9]{2}-[0-9]{2}"
$base = if ($env:GITHUB_BASE_REF) { "origin/$env:GITHUB_BASE_REF" } else { "origin/main" }
$diffOutput = git -C $RepoRoot diff "$base...HEAD" -- $OverrideDocPath 2>$null
if (-not $diffOutput) { $diffOutput = git -C $RepoRoot diff "HEAD~1..HEAD" -- $OverrideDocPath 2>$null }
$addedLines = @($diffOutput | Where-Object { $_ -match '^\+' -and $_ -match '\|' } | ForEach-Object { $_.Substring(1).Trim() })
$validRow = $false
foreach ($line in $addedLines) {
    if ($line -notmatch '\|') { continue }
    $cells = $line -split '\|', -1 | ForEach-Object { $_.Trim() }
    $hasDate = $line -match $datePattern
    $scope = if ($cells.Count -ge 2) { $cells[1] } else { '' }
    $reason = if ($cells.Count -ge 3) { $cells[2] } else { '' }
    $scopeOk = $scope.Length -gt 0 -and $scope -notmatch '^\*.*\*$' -and $scope -ne '(none yet)' -and $scope.Trim() -ne ''
    $reasonOk = $reason.Length -gt 2 -and $reason -notmatch '^\*+$'
    if ($hasDate -and $scopeOk -and $reasonOk) { $validRow = $true; break }
}
if (-not $validRow) {
    Write-Host "::error::Architecture freeze: ARCHITECTURE_FREEZE_OVERRIDE.md was modified but no valid new table row found. Add a row with Date (YYYY-MM-DD), Files/scope, and Reason/PR (non-empty). See backend/docs/operations/ARCHITECTURE_FREEZE.md." -ForegroundColor Red
    Write-Host "Frozen files changed: $($frozenChanged -join ', ')"
    exit 1
}

exit 0
