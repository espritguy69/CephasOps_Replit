# check-controller-boundaries.ps1
# Scans CephasOps.Api/Controllers for architecture violations: DbContext or Infrastructure in controllers.
# Outputs architecture warning; does not fail the build.
# Run from repository root: pwsh -File scripts/architecture/check-controller-boundaries.ps1

$ErrorActionPreference = "Stop"
$AppRoot = $PSScriptRoot
$RepoRoot = (Get-Item $AppRoot).Parent.Parent.FullName
$ControllersPath = Join-Path $RepoRoot "backend\src\CephasOps.Api\Controllers"

if (-not (Test-Path $ControllersPath)) {
    Write-Host "Controllers path not found: $ControllersPath (run from repo root or scripts/architecture)"
    exit 0
}

$violations = @()
$controllerFiles = Get-ChildItem -Path $ControllersPath -Filter "*.cs" -File

foreach ($file in $controllerFiles) {
    $content = Get-Content -Path $file.FullName -Raw -ErrorAction SilentlyContinue
    if (-not $content) { continue }

    $hasDbContext = $content -match "ApplicationDbContext|DbContext\s+_context|: DbContext"
    $hasInfrastructure = $content -match "CephasOps\.Infrastructure\.(Persistence|Services|Repositories)" -or
                         $content -match "using\s+CephasOps\.Infrastructure"

    if ($hasDbContext -or $hasInfrastructure) {
        $reasons = @()
        if ($hasDbContext) { $reasons += "DbContext" }
        if ($hasInfrastructure) { $reasons += "Infrastructure" }
        $violations += [PSCustomObject]@{
            Controller = $file.BaseName
            File       = $file.Name
            Reasons    = $reasons -join ", "
        }
    }
}

if ($violations.Count -eq 0) {
    Write-Host "Controller boundaries check: no controllers reference DbContext or Infrastructure."
    exit 0
}

Write-Host ""
Write-Host "WARNING: Controller architecture violation" -ForegroundColor Yellow
Write-Host "Controllers must not inject DbContext or Infrastructure. Use Application services only. See docs/architecture/architecture_guardrails.md" -ForegroundColor Yellow
Write-Host ""

foreach ($v in $violations) {
    Write-Host "  Controller: $($v.Controller) ($($v.File))"
    Write-Host "  Violation:  $($v.Reasons)"
    Write-Host ""
}

Write-Host "This is a warning only; CI is not blocked. Refactor controllers to use Application-layer services for all data access." -ForegroundColor Yellow
exit 0
