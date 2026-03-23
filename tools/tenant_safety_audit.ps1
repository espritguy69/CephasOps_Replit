# Tenant Safety Automated Audit
# Scans C# files for risky multi-tenant data-access patterns (IgnoreQueryFilters, navigation fixup).
# Run from repo root: .\tools\tenant_safety_audit.ps1
# Exit: 0 = no HIGH findings, 1 = at least one HIGH finding.
# See backend/docs/operations/TENANT_SAFETY_AUTOMATED_AUDIT.md

param(
    [switch]$IncludeTests,  # Also scan backend/tests (default: backend/src only)
    [switch]$Quiet          # Only summary and exit code; no per-finding detail
)

$ErrorActionPreference = "Stop"

$RepoRoot = $null
if ($PSScriptRoot) {
    $RepoRoot = $PSScriptRoot
} else {
    $RepoRoot = Get-Location
}
# If we're in tools/, go up one level
if ((Split-Path -Leaf $RepoRoot) -eq "tools") {
    $RepoRoot = Split-Path -Parent $RepoRoot
}

$BackendSrc = Join-Path $RepoRoot "backend\src"
$BackendTests = Join-Path $RepoRoot "backend\tests"
if (-not (Test-Path $BackendSrc)) {
    Write-Error "backend\src not found. Run from repo root or tools."
    exit 2
}

$script:Findings = [System.Collections.ArrayList]::new()
$Script:FileCache = @{}  # path -> array of lines

function Get-FileLines {
    param([string]$Path)
    if (-not $Script:FileCache.ContainsKey($Path)) {
        $Script:FileCache[$Path] = Get-Content -Path $Path -Raw | ForEach-Object { $_ -split "`n" }
    }
    return $Script:FileCache[$Path]
}

function Get-MethodBlock {
    param([string[]]$Lines, [int]$FromLine)
    $start = 0
    for ($i = $FromLine; $i -ge 0; $i--) {
        if ($Lines[$i] -match '^\s*(public|private|protected|internal)\s+') {
            $start = $i
            break
        }
    }
    $end = $Lines.Count - 1
    for ($i = $FromLine + 1; $i -lt $Lines.Count; $i++) {
        if ($Lines[$i] -match '^\s*(public|private|protected|internal)\s+' -and $Lines[$i] -notmatch '^\s*//') {
            $end = $i - 1
            break
        }
    }
    return @{ Start = $start; End = $end; Block = $Lines[$start..$end] -join "`n" }
}

# ----- CHECK A: IgnoreQueryFilters -----
# Entity set names that are tenant-scoped; unscoped IgnoreQueryFilters on these = HIGH (clearly unsafe).
$TenantScopedSets = @('\.Orders\.', '\.Assets\.', '\.AssetDisposals\.', '\.BillingRatecards\.', '\.ServiceInstallers\.',
    '\.OrderCategories\.', '\.BuildingTypes\.', '\.Materials\.', '\.MaterialCategories\.', '\.Partners\.', '\.Invoices\.',
    '\.Departments\.', '\.DepartmentMemberships\.', '\.Buildings\.', '\.Inventory\b')

function Test-CompanyScopingNearby {
    param([string[]]$Lines, [int]$LineIndex, [int]$Window = 18)
    $end = [Math]::Min($LineIndex + $Window, $Lines.Count - 1)
    $snippet = ($Lines[$LineIndex..$end] -join " ") -replace '\s+', ' '
    return $snippet -match 'CompanyId|TenantId|CurrentTenantId|\.CompanyId\s*==|disposal\.CompanyId|order\.CompanyId|entity\.CompanyId'
}

function Test-IsTenantScopedSet {
    param([string]$Line)
    foreach ($pattern in $TenantScopedSets) {
        if ($Line -match $pattern) { return $true }
    }
    return $false
}

# Reference-data sets: used for DTO enrichment / display after create/update; parent entity is usually company-scoped.
# When we see FindAsync/Find/Single on these, we use an expanded window (lines before + after) to detect company context.
# Primary-entity sets (Orders, Assets, AssetDisposals, BillingRatecards, Buildings, Invoices, etc.) are NOT in this list.
$ReferenceDataSetPatterns = @('\.OrderCategories\.', '\.Partners\.', '\.Materials\.', '\.MaterialCategories\.', '\.BuildingTypes\.', '\.ServiceInstallers\.')

function Test-IsReferenceDataSet {
    param([string]$Line)
    foreach ($pattern in $ReferenceDataSetPatterns) {
        if ($Line -match $pattern) { return $true }
    }
    return $false
}

# Expanded window (lines before + after) for QueryByIdOnly: reference-data lookups often have CompanyId earlier in the same method (e.g. rate.CompanyId, RequireCompanyId).
function Test-CompanyScopingInExpandedWindow {
    param([string[]]$Lines, [int]$LineIndex, [int]$BackwardLines = 30, [int]$ForwardLines = 18)
    $start = [Math]::Max(0, $LineIndex - $BackwardLines)
    $end = [Math]::Min($LineIndex + $ForwardLines, $Lines.Count - 1)
    $snippet = ($Lines[$start..$end] -join " ") -replace '\s+', ' '
    # rate. = parent rate entity in company-scoped flow (e.g. rate.CompanyId, rate.OrderTypeId).
    return $snippet -match 'CompanyId|TenantId|CurrentTenantId|\.CompanyId\s*==|disposal\.CompanyId|order\.CompanyId|entity\.CompanyId|companyId|buildingId|BuildingId|rate\.'
}

function Invoke-CheckA {
    $csFiles = Get-ChildItem -Path $BackendSrc -Recurse -Filter "*.cs" -File |
        Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' -and $_.Name -notmatch '\.(Designer|g)\.cs$' }
    if ($IncludeTests -and (Test-Path $BackendTests)) {
        $csFiles += Get-ChildItem -Path $BackendTests -Recurse -Filter "*.cs" -File |
            Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' -and $_.Name -notmatch '\.(Designer|g)\.cs$' }
    }
    foreach ($f in $csFiles) {
        $relPath = $f.FullName.Replace($RepoRoot + [IO.Path]::DirectorySeparatorChar, "").Replace("\", "/")
        $lines = Get-FileLines -Path $f.FullName
        for ($i = 0; $i -lt $lines.Count; $i++) {
            if ($lines[$i] -match '\.IgnoreQueryFilters\s*\(\s*\)') {
                $hasScope = Test-CompanyScopingNearby -Lines $lines -LineIndex $i
                if ($hasScope) {
                    [void]$script:Findings.Add([PSCustomObject]@{
                        File       = $relPath
                        Line       = $i + 1
                        Rule       = "CHECK_A_IgnoreQueryFilters"
                        Severity   = "LOW"
                        Explanation = "IgnoreQueryFilters with explicit company/tenant scoping nearby (SAFE_LIKELY)."
                    })
                } else {
                    $isTenantScoped = Test-IsTenantScopedSet -Line $lines[$i]
                    if ($isTenantScoped) {
                        [void]$script:Findings.Add([PSCustomObject]@{
                            File       = $relPath
                            Line       = $i + 1
                            Rule       = "CHECK_A_IgnoreQueryFilters"
                            Severity   = "HIGH"
                            Explanation = "IgnoreQueryFilters on tenant-scoped set with no CompanyId/TenantId constraint in local block (FLAG_UNSAFE)."
                        })
                    } else {
                        [void]$script:Findings.Add([PSCustomObject]@{
                            File       = $relPath
                            Line       = $i + 1
                            Rule       = "CHECK_A_IgnoreQueryFilters"
                            Severity   = "MEDIUM"
                            Explanation = "IgnoreQueryFilters with no obvious company scoping nearby (NEEDS_REVIEW; may be intentional e.g. Auth, platform jobs)."
                        })
                    }
                }
            }
            # CHECK A extension: FindAsync, Find, Single on tenant-scoped set without CompanyId nearby
            if ($lines[$i] -match '\.(FindAsync|Find|Single)\s*\(' -and (Test-IsTenantScopedSet -Line $lines[$i])) {
                $hasScope = $false
                if (Test-IsReferenceDataSet -Line $lines[$i]) {
                    # Reference-data sets (OrderCategories, Partners, Materials, ServiceInstallers, etc.): use expanded window
                    # (lines before + after) so post-create/update DTO enrichment with CompanyId earlier in method is not flagged.
                    $hasScope = Test-CompanyScopingInExpandedWindow -Lines $lines -LineIndex $i
                } else {
                    # Primary-entity sets (Orders, Assets, AssetDisposals, etc.): keep strict forward-only window.
                    $hasScope = Test-CompanyScopingNearby -Lines $lines -LineIndex $i
                }
                if (-not $hasScope) {
                    [void]$script:Findings.Add([PSCustomObject]@{
                        File       = $relPath
                        Line       = $i + 1
                        Rule       = "CHECK_A_QueryByIdOnly"
                        Severity   = "MEDIUM"
                        Explanation = "FindAsync/Find/Single on tenant-scoped set with no CompanyId/TenantId in local window (query by Id only; consider explicit company scope)."
                    })
                }
            }
        }
    }
}

# ----- CHECK B: Navigation property update / fixup risk -----
function Invoke-CheckB {
    $csFiles = Get-ChildItem -Path $BackendSrc -Recurse -Filter "*.cs" -File |
        Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' -and $_.Name -notmatch '\.(Designer|g)\.cs$' }
    if ($IncludeTests -and (Test-Path $BackendTests)) {
        $csFiles += Get-ChildItem -Path $BackendTests -Recurse -Filter "*.cs" -File |
            Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' -and $_.Name -notmatch '\.(Designer|g)\.cs$' }
    }
    foreach ($f in $csFiles) {
        $relPath = $f.FullName.Replace($RepoRoot + [IO.Path]::DirectorySeparatorChar, "").Replace("\", "/")
        $lines = Get-FileLines -Path $f.FullName
        for ($i = 0; $i -lt $lines.Count; $i++) {
            $line = $lines[$i]
            # Match navigation property update: entity.NavProp.Property = (e.g. disposal.Asset.Status =)
            if ($line -match '(\w+)\.(\w+)\.(Status|UpdatedAt|IsDeleted)\s*=') {
                if ($line -match '^\s*_context\.') { continue }
                if ($line -match '^\s*//') { continue }
                $block = Get-MethodBlock -Lines $lines -FromLine $i
                $blockText = $block.Block
                $hasGuardedLookup = $blockText -match 'FirstOrDefaultAsync|FirstAsync' -and $blockText -match 'CompanyId'
                $hasNullClearing = $blockText -match '\.\w+\s*=\s*null\s*;|\.Asset\s*=\s*null|\w+\.\w+\s*=\s*null\s*;'
                if ($hasGuardedLookup -and -not $hasNullClearing) {
                    [void]$script:Findings.Add([PSCustomObject]@{
                        File       = $relPath
                        Line       = $i + 1
                        Rule       = "CHECK_B_FixupRisk"
                        Severity   = "MEDIUM"
                        Explanation = "Navigation property updated; guarded lookup present but no obvious null-clearing of navigation (fixup risk)."
                    })
                }
            }
        }
    }
}

# ----- Run checks -----
Invoke-CheckA
Invoke-CheckB

# ----- Report -----
$high = ($script:Findings | Where-Object { $_.Severity -eq "HIGH" })
$medium = ($script:Findings | Where-Object { $_.Severity -eq "MEDIUM" })
$low = ($script:Findings | Where-Object { $_.Severity -eq "LOW" })

if (-not $Quiet) {
    Write-Host "=== Tenant Safety Automated Audit ===" -ForegroundColor Cyan
    Write-Host "Scanned: backend/src" -NoNewline
    if ($IncludeTests) { Write-Host " + backend/tests" -NoNewline }
    Write-Host ""
    Write-Host ""

    foreach ($h in $high) {
        Write-Host "[HIGH] $($h.File):$($h.Line) $($h.Rule)" -ForegroundColor Red
        Write-Host "  $($h.Explanation)" -ForegroundColor Gray
    }
    foreach ($m in $medium) {
        Write-Host "[MEDIUM] $($m.File):$($m.Line) $($m.Rule)" -ForegroundColor Yellow
        Write-Host "  $($m.Explanation)" -ForegroundColor Gray
    }
    if ($low.Count -gt 0 -and ($high.Count + $medium.Count) -le 15) {
        foreach ($l in $low) {
            Write-Host "[LOW] $($l.File):$($l.Line) $($l.Rule)" -ForegroundColor DarkGray
            Write-Host "  $($l.Explanation)" -ForegroundColor Gray
        }
    } elseif ($low.Count -gt 0) {
        Write-Host "[LOW] $($low.Count) SAFE_LIKELY IgnoreQueryFilters (omitted; use -Quiet:$false and no HIGH/MEDIUM to see all)." -ForegroundColor DarkGray
    }
}

Write-Host ""
Write-Host "--- Summary ---" -ForegroundColor Cyan
Write-Host "HIGH:   $($high.Count)"
Write-Host "MEDIUM: $($medium.Count)"
Write-Host "LOW:    $($low.Count)"
$mediumA = ($script:Findings | Where-Object { $_.Rule -eq "CHECK_A_IgnoreQueryFilters" -and $_.Severity -eq "MEDIUM" }).Count
$mediumAq = ($script:Findings | Where-Object { $_.Rule -eq "CHECK_A_QueryByIdOnly" }).Count
$mediumB = ($script:Findings | Where-Object { $_.Rule -eq "CHECK_B_FixupRisk" }).Count
Write-Host "By rule: CHECK_A HIGH=$($high.Count) MEDIUM=$mediumA LOW=$($low.Count) QueryByIdOnly=$mediumAq; CHECK_B MEDIUM=$mediumB"

if ($high.Count -gt 0) {
    Write-Host ""
    Write-Host "Exit code 1: HIGH findings require remediation. See backend/docs/architecture/TENANT_QUERY_SAFETY_GUIDELINES.md and EFCORE_RELATIONSHIP_FIXUP_RISK.md." -ForegroundColor Red
    exit 1
}
exit 0
