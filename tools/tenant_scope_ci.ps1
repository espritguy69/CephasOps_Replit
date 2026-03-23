# Tenant Scope & Executor CI — runtime tenant-safety checks for CephasOps.
# Run from repo root: .\tools\tenant_scope_ci.ps1
# Exit: 0 = all checks pass, 1 = at least one violation.
# See backend/docs/operations/TENANT_SAFETY_CI.md
# Allowlist: tools/tenant_safety_ci_allowlist.json

param(
    [string]$AllowlistPath = "",
    [string[]]$ChangedFiles = @(),  # If set, guard-doc drift uses this list instead of git
    [switch]$SkipGuardDocCheck      # Set when not in CI or when changed files unknown
)

$ErrorActionPreference = "Stop"

$RepoRoot = if ($PSScriptRoot) { $PSScriptRoot } else { Get-Location }
if ((Split-Path -Leaf $RepoRoot) -eq "tools") { $RepoRoot = Split-Path -Parent $RepoRoot }

$BackendSrc = Join-Path $RepoRoot "backend\src"
if (-not (Test-Path $BackendSrc)) {
    Write-Error "backend\src not found. Run from repo root or tools."
    exit 2
}

$AllowlistPath = if ($AllowlistPath) { $AllowlistPath } else { Join-Path $RepoRoot "tools\tenant_safety_ci_allowlist.json" }
if (-not (Test-Path $AllowlistPath)) {
    Write-Error "Allowlist not found: $AllowlistPath"
    exit 2
}

$allowlist = Get-Content -Path $AllowlistPath -Raw | ConvertFrom-Json

# Normalize path for comparison (forward slashes, relative to repo)
function Normalize-PathRel($fullPath) {
    $p = $fullPath.Replace($RepoRoot + [IO.Path]::DirectorySeparatorChar, "").Replace("\", "/")
    return $p
}

# Build allowlist sets
$manualScopeAllowed = @{}
foreach ($e in $allowlist.manual_scope_allowed) {
    $key = $e.path.Replace("\", "/")
    $manualScopeAllowed[$key] = $e.reason
}

$executorNotRequired = @{}
foreach ($e in $allowlist.executor_not_required) {
    $executorNotRequired[$e.path] = $e.reason
}

$guardFiles = @($allowlist.guard_files)
$architectureDocPaths = @($allowlist.architecture_doc_paths)

$script:Violations = [System.Collections.ArrayList]::new()

# ---- Check 1: Manual tenant scope in runtime (backend/src only; tests excluded) ----
function Test-ManualScopeViolation {
    $csFiles = Get-ChildItem -Path $BackendSrc -Recurse -Filter "*.cs" -File |
        Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' -and $_.Name -notmatch '\.(Designer|g)\.cs$' }
    foreach ($f in $csFiles) {
        $relPath = Normalize-PathRel $f.FullName
        if ($manualScopeAllowed.ContainsKey($relPath)) { continue }
        $content = Get-Content -Path $f.FullName -Raw
        $lineNum = 0
        foreach ($line in (Get-Content -Path $f.FullName)) {
            $lineNum++
            $trimmed = $line.Trim()
            if ($trimmed -match '^(//|///|\*|/\*|#)') { continue }
            if ($line -match 'TenantScope\.CurrentTenantId') {
                [void]$script:Violations.Add([PSCustomObject]@{
                    Rule       = "MANUAL_SCOPE"
                    File       = $relPath
                    Line       = $lineNum
                    Message    = "Manual use of TenantScope.CurrentTenantId is not allowed in runtime code."
                    UseInstead = "Use TenantScopeExecutor.RunWithTenantScopeAsync(companyId, work, ct) or RunWithTenantScopeOrBypassAsync for nullable-company paths."
                })
            }
            if ($line -match '\.EnterPlatformBypass\s*\(\s*\)' -or $line -match 'TenantSafetyGuard\.EnterPlatformBypass') {
                [void]$script:Violations.Add([PSCustomObject]@{
                    Rule       = "MANUAL_BYPASS_ENTER"
                    File       = $relPath
                    Line       = $lineNum
                    Message    = "Manual EnterPlatformBypass() is not allowed in runtime code."
                    UseInstead = "Use TenantScopeExecutor.RunWithPlatformBypassAsync(work, ct) for platform-wide work."
                })
            }
            if ($line -match '\.ExitPlatformBypass\s*\(\s*\)' -or $line -match 'TenantSafetyGuard\.ExitPlatformBypass') {
                [void]$script:Violations.Add([PSCustomObject]@{
                    Rule       = "MANUAL_BYPASS_EXIT"
                    File       = $relPath
                    Line       = $lineNum
                    Message    = "Manual ExitPlatformBypass() is not allowed in runtime code."
                    UseInstead = "Use TenantScopeExecutor.RunWithPlatformBypassAsync(work, ct) so exit is always in finally."
                })
            }
        }
    }
}

# ---- Check 2: Runtime orchestration (BackgroundService/IHostedService) must use TenantScopeExecutor ----
function Test-ExecutorRequired {
    $csFiles = Get-ChildItem -Path $BackendSrc -Recurse -Filter "*.cs" -File |
        Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' -and $_.Name -notmatch '\.(Designer|g)\.cs$' }
    foreach ($f in $csFiles) {
        $content = Get-Content -Path $f.FullName -Raw
        $name = $f.Name
        $relPath = Normalize-PathRel $f.FullName
        if (-not ($content -match ':\s*BackgroundService' -or $content -match ':\s*IHostedService')) { continue }
        if ($content -match 'TenantScopeExecutor') { continue }
        $allowed = $false
        foreach ($key in $executorNotRequired.Keys) {
            if ($relPath -match [regex]::Escape($key) -or $name -match [regex]::Escape($key)) { $allowed = $true; break }
        }
        if ($allowed) { continue }
        [void]$script:Violations.Add([PSCustomObject]@{
            Rule       = "EXECUTOR_REQUIRED"
            File       = $relPath
            Line       = 1
            Message    = "Runtime orchestration (hosted service / scheduler / worker) must use TenantScopeExecutor for tenant or platform scope."
            UseInstead = "Wrap work in TenantScopeExecutor.RunWithTenantScopeAsync, RunWithPlatformBypassAsync, or RunWithTenantScopeOrBypassAsync. If this component does not touch tenant data, add it to tools/tenant_safety_ci_allowlist.json executor_not_required with a reason."
        })
    }
}

# ---- Check 3: Guard file edits require architecture doc update (when changed files known) ----
function Test-GuardDocDrift {
    if ($SkipGuardDocCheck) { return }
    $changed = $ChangedFiles
    if ($changed.Count -eq 0 -and $env:GITHUB_ACTIONS -eq "true") {
        try {
            $base = if ($env:GITHUB_BASE_REF) { "origin/$env:GITHUB_BASE_REF" } else { "origin/main" }
            $changed = @(git diff --name-only "$base...HEAD" 2>$null)
            if (-not $changed) { $changed = @(git diff --name-only "HEAD~1..HEAD" 2>$null) }
        } catch {
            return
        }
    }
    if ($changed.Count -eq 0) { return }
    $guardChanged = $false
    $docChanged = $false
    foreach ($p in $changed) {
        $norm = $p.Replace("\", "/")
        foreach ($g in $guardFiles) {
            if ($norm -match [regex]::Escape($g)) { $guardChanged = $true; break }
        }
        foreach ($d in $architectureDocPaths) {
            if ($norm -replace "^/", "" -eq $d -or $norm -match [regex]::Escape($d)) { $docChanged = $true; break }
        }
    }
    if ($guardChanged -and -not $docChanged) {
        [void]$script:Violations.Add([PSCustomObject]@{
            Rule       = "GUARD_DOC_DRIFT"
            File       = "(PR or commit)"
            Line       = 0
            Message    = "A guard file (TenantSafetyGuard, SiWorkflowGuard, FinancialIsolationGuard, EventStoreConsistencyGuard) was modified but no architecture doc was updated."
            UseInstead = "Update at least one of backend/docs/architecture/TENANT_SAFETY_DEVELOPER_GUIDE.md, TENANT_SCOPE_EXECUTOR_COMPLETION.md, or SECURITY_AND_TENANT_SAFETY_ARCHITECTURE.md, or add a comment in the PR explaining why no doc change is needed."
        })
    }
}

# ---- Run checks ----
Test-ManualScopeViolation
Test-ExecutorRequired
Test-GuardDocDrift

# ---- Report ----
if (-not $script:Violations.Count) {
    Write-Host "Tenant scope CI: all checks passed." -ForegroundColor Green
    exit 0
}

Write-Host ""
Write-Host "=== Tenant Scope CI — Violations ===" -ForegroundColor Red
foreach ($v in $script:Violations) {
    Write-Host "[$($v.Rule)] $($v.File):$($v.Line)" -ForegroundColor Red
    Write-Host "  $($v.Message)" -ForegroundColor Gray
    Write-Host "  Use instead: $($v.UseInstead)" -ForegroundColor Cyan
}
Write-Host ""
Write-Host "See backend/docs/operations/TENANT_SAFETY_CI.md and .cursor/rules (00_no_manual_scope, 02_tenant_safety, 03_backend_workers)." -ForegroundColor Yellow
Write-Host "Allowlist: tools/tenant_safety_ci_allowlist.json" -ForegroundColor Yellow
exit 1
