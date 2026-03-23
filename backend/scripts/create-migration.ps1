# Official EF Core migration creation — single entry point.
# Creates a migration with the correct project paths, verifies both .cs and .Designer.cs exist,
# and runs the hygiene validator. Do not create migration files by hand.
#
# From repo root:
#   .\backend\scripts\create-migration.ps1 -MigrationName "AddOrderStatusChecklist"
# From backend/:
#   .\scripts\create-migration.ps1 -MigrationName "AddOrderStatusChecklist"
#
# Good names: AddOrderStatusChecklist, AddPasswordResetTokens, AddJobRunEventId
# Bad names:  temp, test, fix, migration1, newmigration, update, wip
# See docs/operations/EF_SAFE_MIGRATION_WORKFLOW.md and docs/operations/EF_MIGRATION_PR_CHECKLIST.md

param(
    [Parameter(Mandatory = $true)]
    [string]$MigrationName
)

$ErrorActionPreference = "Stop"

$scriptsDir = $PSScriptRoot
$backendDir = (Resolve-Path (Join-Path $scriptsDir "..")).Path
$infraPath = Join-Path $backendDir "src" "CephasOps.Infrastructure"
$apiPath = Join-Path $backendDir "src" "CephasOps.Api"
$migrationsDir = Join-Path $infraPath "Persistence" "Migrations"

if (-not (Test-Path $infraPath)) {
    Write-Host "Error: Infrastructure project not found at $infraPath" -ForegroundColor Red
    exit 1
}
if (-not (Test-Path $apiPath)) {
    Write-Host "Error: API project not found at $apiPath" -ForegroundColor Red
    exit 1
}

# Ensure dotnet-ef is available
$efTool = dotnet tool list -g 2>$null | Select-String "dotnet-ef"
if (-not $efTool) {
    Write-Host "Installing dotnet-ef tool..." -ForegroundColor Yellow
    dotnet tool install --global dotnet-ef
}

Write-Host "Creating EF Core migration: $MigrationName" -ForegroundColor Green
Write-Host "  Infrastructure: $infraPath" -ForegroundColor Gray
Write-Host "  Startup: $apiPath" -ForegroundColor Gray
Write-Host ""

# Run from Api directory so EF can resolve startup project and paths cleanly
$apiDir = $apiPath
Push-Location $apiDir
try {
    $addResult = dotnet ef migrations add $MigrationName `
        --project (Join-Path $backendDir "src" "CephasOps.Infrastructure" "CephasOps.Infrastructure.csproj") `
        --context ApplicationDbContext `
        --output-dir "Persistence/Migrations"

    if ($LASTEXITCODE -ne 0) {
        Write-Host "Migration creation failed (dotnet ef exited with $LASTEXITCODE)." -ForegroundColor Red
        exit 1
    }

    Write-Host "Migration generated. Verifying both .cs and .Designer.cs exist..." -ForegroundColor Yellow

    # Newest migration by filename (timestamp_name) is the one just added
    $mainFiles = Get-ChildItem -Path $migrationsDir -Filter "*.cs" -File |
        Where-Object { $_.Name -notmatch "\.Designer\.cs$" -and $_.Name -ne "ApplicationDbContextModelSnapshot.cs" }
    $newest = $mainFiles | Sort-Object Name -Descending | Select-Object -First 1

    if (-not $newest) {
        Write-Host "WARN: Could not find any migration main file in $migrationsDir" -ForegroundColor Yellow
    } else {
        $designerPath = Join-Path $migrationsDir "$($newest.BaseName).Designer.cs"
        if (-not (Test-Path $designerPath)) {
            Write-Host "FAIL: New migration has no .Designer.cs: $($newest.Name)" -ForegroundColor Red
            Write-Host "  Every new migration must have a .Designer.cs. Do not commit. Re-run with the official command or fix the migration." -ForegroundColor Red
            exit 1
        }
        Write-Host "PASS: Both $($newest.Name) and $($newest.BaseName).Designer.cs exist." -ForegroundColor Green
    }

    Write-Host ""
    Write-Host "Running migration hygiene validation..." -ForegroundColor Yellow
    $validatorPath = Join-Path $scriptsDir "validate-migration-hygiene.ps1"
    & $validatorPath
    $validatorExit = $LASTEXITCODE

    if ($validatorExit -ne 0) {
        Write-Host ""
        Write-Host "=== STOP AND REVIEW ===" -ForegroundColor Red
        Write-Host "Hygiene validation failed or reported warnings. Do not commit until:" -ForegroundColor Red
        Write-Host "  1. The new migration has both .cs and .Designer.cs" -ForegroundColor Red
        Write-Host "  2. validate-migration-hygiene.ps1 passes (or you have documented reason for the warning)" -ForegroundColor Red
        Write-Host "  3. The migration is not unexpectedly large (snapshot drift)" -ForegroundColor Red
        Write-Host "See docs/operations/EF_SAFE_MIGRATION_WORKFLOW.md and docs/operations/EF_MIGRATION_PR_CHECKLIST.md" -ForegroundColor Yellow
        # Still run classifier so developer sees classification and action
        $classifierPath = Join-Path $scriptsDir "classify-migration-state.ps1"
        if (Test-Path $classifierPath) { & $classifierPath }
        exit 1
    }

    # Run auto-classification and print category-specific guidance
    $classifierPath = Join-Path $scriptsDir "classify-migration-state.ps1"
    if (Test-Path $classifierPath) {
        & $classifierPath
    }

    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Green
    Write-Host "  1. Follow the REQUIRED ACTION above for your classification (A/D/E)." -ForegroundColor Cyan
    Write-Host "  2. If A (normal EF): update ExpectedTotalMainCount and ExpectedWithDesignerCount in validate-migration-hygiene.ps1 and docs when you add the next migration." -ForegroundColor Cyan
    Write-Host "  3. If D (snapshot drift risk): do not commit until reviewed; see docs/operations/EF_SAFE_MIGRATION_WORKFLOW.md." -ForegroundColor Cyan
    Write-Host "  4. Review the migration in backend/src/CephasOps.Infrastructure/Persistence/Migrations/; do not delete .Designer.cs or touch historical migrations." -ForegroundColor Cyan
} finally {
    Pop-Location
}
