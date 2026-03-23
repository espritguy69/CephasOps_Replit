# Validate EF Core migration hygiene (non-destructive).
# Run from repo root or backend/: .\backend\scripts\validate-migration-hygiene.ps1
# Use before committing new migrations; optionally in CI.
# Exit code: 0 = pass, 1 = fail (e.g. new migration without Designer).
# See docs/operations/EF_SAFE_MIGRATION_WORKFLOW.md and docs/operations/EF_MIGRATION_PR_CHECKLIST.md

param(
    [switch]$WarnOnly,  # If set, do not exit 1 on failure; only write warnings.
    [switch]$Classify   # If set, run classify-migration-state.ps1 after validation and print classification.
)

$ErrorActionPreference = "Stop"

# Expected state (authoritative: 95 with Designer, 47 without; includes 20260310031127_AddExternalIntegrationBus)
# When adding a new migration WITH Designer, increment ExpectedTotalMainCount and ExpectedWithDesignerCount by 1.
# When adding a new script-only migration (no Designer), increment ExpectedNoDesignerCount and ExpectedTotalMainCount by 1.
$ExpectedNoDesignerCount = 47
$ExpectedWithDesignerCount = 95
$ExpectedTotalMainCount = 142
$SuspiciousLineCount = 500   # Migration main file lines above this may indicate snapshot drift

# Names that suggest temporary, test, or low-quality migrations (case-insensitive match on name part after timestamp).
$BadNamePatterns = @(
    '^temp$', '^test$', '^fix$', '^fixes$', '^aaa$', '^migration1$', '^newmigration$', '^migration$',
    '^new$', '^update$', '^changes$', '^wip$', '^asdf$', '^qwe$', '^foo$', '^bar$'
)

$MigrationsDir = Join-Path -Path $PSScriptRoot -ChildPath "..\src\CephasOps.Infrastructure\Persistence\Migrations"
if (-not (Test-Path $MigrationsDir)) {
    $MigrationsDir = Join-Path -Path (Get-Location) -ChildPath "src\CephasOps.Infrastructure\Persistence\Migrations"
    if (-not (Test-Path $MigrationsDir)) {
        Write-Error "Migrations folder not found. Run from backend/ or repo root."
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

$withDesigner = $mainFiles.Count - $missing.Count
$failed = $false
$warnings = 0

Write-Host "=== EF Migration Hygiene Validation ===" -ForegroundColor Cyan
Write-Host ""

# (a) Designer presence: fail if new migration missing Designer.
if ($missing.Count -ne $ExpectedNoDesignerCount) {
    Write-Host "FAIL: Migrations missing Designer count is $($missing.Count); expected $ExpectedNoDesignerCount." -ForegroundColor Red
    if ($missing.Count -gt $ExpectedNoDesignerCount) {
        Write-Host "  A NEW migration was added without a .Designer.cs. Every new migration must have a matching .Designer.cs." -ForegroundColor Red
    }
    Write-Host "  Create migrations with: backend\scripts\create-migration.ps1 -MigrationName ""YourDescriptiveName""" -ForegroundColor Red
    Write-Host "  See docs/operations/EF_SAFE_MIGRATION_WORKFLOW.md" -ForegroundColor Yellow
    Write-Host "  What to do: Create the migration via the official script or 'dotnet ef migrations add'; ensure the Designer is generated; do not commit without both files." -ForegroundColor Yellow
    $failed = $true
} else {
    Write-Host "PASS: Migrations missing Designer count = $ExpectedNoDesignerCount (expected)." -ForegroundColor Green
}

# (b) Count consistency
Write-Host ""
if ($mainFiles.Count -ne $ExpectedTotalMainCount) {
    Write-Host "WARN: Total main migration files = $($mainFiles.Count); expected $ExpectedTotalMainCount." -ForegroundColor Yellow
    Write-Host "  What to do: If you added a new migration with Designer, update ExpectedTotalMainCount and ExpectedWithDesignerCount in this script and in docs." -ForegroundColor Yellow
    $warnings++
}
if ($withDesigner -ne $ExpectedWithDesignerCount) {
    Write-Host "WARN: Migrations with Designer = $withDesigner; expected $ExpectedWithDesignerCount." -ForegroundColor Yellow
    Write-Host "  What to do: Update expected counts in this script and in docs/operations/EF_MIGRATION_FINAL_CLEANUP_AUDIT.md (and related) when you add a new migration with Designer." -ForegroundColor Yellow
    $warnings++
}

Write-Host "  Total main: $($mainFiles.Count) | With Designer: $withDesigner | Missing Designer: $($missing.Count)" -ForegroundColor Gray
Write-Host ""

# (c) Manifest
$manifestPath = Join-Path -Path $PSScriptRoot -ChildPath "..\..\docs\operations\EF_NO_DESIGNER_MIGRATIONS_MANIFEST.md"
if (-not (Test-Path $manifestPath)) {
    $manifestPath = Join-Path -Path (Get-Location) -ChildPath "docs\operations\EF_NO_DESIGNER_MIGRATIONS_MANIFEST.md"
}
if (Test-Path $manifestPath) {
    Write-Host "PASS: No-Designer manifest exists (script-only migrations documented)." -ForegroundColor Green
} else {
    Write-Host "WARN: No-Designer manifest not found at expected path." -ForegroundColor Yellow
    Write-Host "  What to do: Ensure docs/operations/EF_NO_DESIGNER_MIGRATIONS_MANIFEST.md exists." -ForegroundColor Yellow
    $warnings++
}
Write-Host ""

# (d) Naming quality: warn on obviously bad migration names (name part after first _)
$badNames = @()
foreach ($f in $mainFiles) {
    $base = $f.BaseName
    if ($base -match '^\d+_(.+)$') {
        $namePart = $matches[1]
        foreach ($pat in $BadNamePatterns) {
            if ($namePart -imatch $pat) {
                $badNames += [PSCustomObject]@{ File = $f.Name; NamePart = $namePart }
                break
            }
        }
    }
}
if ($badNames.Count -gt 0) {
    Write-Host "WARN: One or more migrations have discouraged names (temp/test/fix/generic):" -ForegroundColor Yellow
    foreach ($x in $badNames) {
        Write-Host "  $($x.File) (name part: $($x.NamePart))" -ForegroundColor Yellow
    }
    Write-Host "  What to do: Use descriptive names (e.g. AddOrderStatusChecklist, AddPasswordResetTokens). Do not use temp, test, fix, migration1, etc. Rename only if the migration is not yet applied and you can recreate it." -ForegroundColor Yellow
    Write-Host ""
    $warnings++
}

# (e) Suspiciously large migration files (possible snapshot drift)
$large = @()
foreach ($f in $mainFiles) {
    $lines = (Get-Content $f.FullName -ErrorAction SilentlyContinue | Measure-Object -Line).Lines
    if ($lines -gt $SuspiciousLineCount) {
        $large += [PSCustomObject]@{ Name = $f.Name; Lines = $lines }
    }
}
if ($large.Count -gt 0) {
    Write-Host "WARN: One or more migration main files are unusually large (possible snapshot drift):" -ForegroundColor Yellow
    foreach ($x in $large) {
        Write-Host "  $($x.Name): $($x.Lines) lines" -ForegroundColor Yellow
    }
    Write-Host "  If this is a NEW migration, do not commit without review. See docs/operations/EF_SAFE_MIGRATION_WORKFLOW.md (snapshot drift)." -ForegroundColor Yellow
    Write-Host "  What to do: Compare the diff to your intended model changes. If the scope is far larger, stop and escalate to a snapshot reconciliation pass." -ForegroundColor Yellow
    Write-Host ""
    $warnings++
}

# Snapshot misuse (documentation only in output)
Write-Host "Note: If you edited ApplicationDbContextModelSnapshot.cs without adding a migration, that is misuse. Add a migration or revert the snapshot change." -ForegroundColor Gray
Write-Host ""

# Final summary
Write-Host "=== Summary ===" -ForegroundColor Cyan
if ($failed) {
    Write-Host "Result: FAIL" -ForegroundColor Red
    Write-Host "Fix the issues above before committing. Use backend\scripts\create-migration.ps1 for new migrations." -ForegroundColor Red
} elseif ($warnings -gt 0) {
    Write-Host "Result: PASS with $warnings warning(s)" -ForegroundColor Yellow
    Write-Host "Review the warnings above before committing. See docs/operations/EF_MIGRATION_PR_CHECKLIST.md" -ForegroundColor Yellow
} else {
    Write-Host "Result: PASS" -ForegroundColor Green
}
# Optional: run auto-classification and print result
if ($Classify) {
    $classifierPath = Join-Path $PSScriptRoot "classify-migration-state.ps1"
    if (Test-Path $classifierPath) {
        & $classifierPath
    }
}

Write-Host "=== End validation ===" -ForegroundColor Cyan
Write-Host ""

if ($failed -and -not $WarnOnly) {
    exit 1
}
exit 0
