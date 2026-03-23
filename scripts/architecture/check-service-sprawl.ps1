# check-service-sprawl.ps1
# Scans CephasOps.Application for service sprawl: services >1200 LOC or >12 constructor dependencies.
# Outputs WARNING only; does not fail the build.
# Run from repository root: pwsh -File scripts/architecture/check-service-sprawl.ps1

$ErrorActionPreference = "Stop"
$AppRoot = $PSScriptRoot
$RepoRoot = (Get-Item $AppRoot).Parent.Parent.FullName
$ApplicationPath = Join-Path $RepoRoot "backend\src\CephasOps.Application"

if (-not (Test-Path $ApplicationPath)) {
    Write-Host "Application path not found: $ApplicationPath (run from repo root or scripts/architecture)"
    exit 0
}

$MaxLoc = 1200
$MaxDependencies = 12
$warnCount = 0

# Find .cs files that define a concrete service class (class *Service or *HostedService), exclude interfaces
$csFiles = Get-ChildItem -Path $ApplicationPath -Filter "*.cs" -Recurse -File |
    Where-Object { $_.FullName -notmatch "\\obj\\|\\bin\\" -and $_.Name -notmatch "^I[A-Z]" }

$results = @()
foreach ($file in $csFiles) {
    $content = Get-Content -Path $file.FullName -Raw -ErrorAction SilentlyContinue
    if (-not $content) { continue }

    # Only consider files that declare a class ending with Service or HostedService (concrete, not interface)
    if ($content -notmatch "public\s+sealed?\s+class\s+\w*(?:Service|HostedService)\b" -and
        $content -notmatch "public\s+class\s+\w*(?:Service|HostedService)\b") {
        continue
    }

    $lineCount = (Get-Content -Path $file.FullName -ErrorAction SilentlyContinue | Measure-Object -Line).Lines
    # Constructor dependency proxy: count "private readonly" (C# convention one per injected dependency)
    $depCount = ([regex]::Matches($content, "private\s+readonly\s+")).Count

    $overLoc = $lineCount -gt $MaxLoc
    $overDeps = $depCount -gt $MaxDependencies
    if ($overLoc -or $overDeps) {
        $relPath = $file.FullName.Replace($ApplicationPath + "\", "").Replace("\", "/")
        $results += [PSCustomObject]@{
            Service = $file.BaseName
            Path    = $relPath
            LOC     = $lineCount
            Deps    = $depCount
            OverLoc = $overLoc
            OverDeps = $overDeps
        }
        $warnCount++
    }
}

if ($warnCount -eq 0) {
    Write-Host "Service sprawl check: no services exceeded $MaxLoc LOC or $MaxDependencies constructor dependencies."
    exit 0
}

Write-Host ""
Write-Host "WARNING: Service sprawl detected" -ForegroundColor Yellow
Write-Host "The following services exceed guardrails (>$MaxLoc LOC or >$MaxDependencies dependencies). See docs/architecture/architecture_guardrails.md" -ForegroundColor Yellow
Write-Host ""

foreach ($r in $results) {
    $reasons = @()
    if ($r.OverLoc) { $reasons += "LOC=$($r.LOC)" }
    if ($r.OverDeps) { $reasons += "Deps=$($r.Deps)" }
    Write-Host "  Service: $($r.Service)"
    Write-Host "  Path:    $($r.Path)"
    Write-Host "  LOC:     $($r.LOC) | Dependency count: $($r.Deps) | Triggers: $($reasons -join ", ")"
    Write-Host ""
}

Write-Host "This is a warning only; CI is not blocked. Do not add new dependencies or grow these services further." -ForegroundColor Yellow
exit 0
