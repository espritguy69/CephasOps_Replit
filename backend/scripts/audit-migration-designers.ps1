# Audit EF Core migrations: list migrations that have a main .cs but no .Designer.cs.
# Run from repo root or backend/: .\backend\scripts\audit-migration-designers.ps1
# No changes to files; read-only audit.

$MigrationsDir = $PSScriptRoot + "\..\src\CephasOps.Infrastructure\Persistence\Migrations"
if (-not (Test-Path $MigrationsDir)) {
    $MigrationsDir = "src\CephasOps.Infrastructure\Persistence\Migrations"
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

$missing = $missing | Sort-Object
Write-Host "Migrations missing .Designer.cs (count: $($missing.Count)):"
Write-Host ""
foreach ($m in $missing) {
    Write-Host "  $m"
}
Write-Host ""
Write-Host "Total migrations (main .cs): $($mainFiles.Count). With Designer: $($mainFiles.Count - $missing.Count). Missing Designer: $($missing.Count)."
