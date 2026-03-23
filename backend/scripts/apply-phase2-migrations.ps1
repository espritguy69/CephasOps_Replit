# PowerShell script to apply Phase 2 Settings migrations
# Run this script to create the new database tables

param(
    [string]$ConnectionString = "Host=db.jgahsbfoydwdgipcjvxe.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=J@saw007;SslMode=Require;Trust Server Certificate=true;Include Error Detail=true"
)

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "Phase 2 Settings Migrations" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# Get the script directory
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptDir
$migrationPath = Join-Path $projectRoot "src\CephasOps.Infrastructure\Persistence\Migrations"

# Check if psql is available
$psqlPath = Get-Command psql -ErrorAction SilentlyContinue
if (-not $psqlPath) {
    Write-Host "ERROR: psql command not found. Please install PostgreSQL client tools." -ForegroundColor Red
    exit 1
}

Write-Host "Step 1: Applying Phase 1 Settings migrations (SLA Profiles, Automation Rules)..." -ForegroundColor Yellow
$phase1Migration = Join-Path $migrationPath "AddPhase1SettingsEntities.sql"
if (Test-Path $phase1Migration) {
    $env:PGPASSWORD = "J@saw007"
    & psql -h db.jgahsbfoydwdgipcjvxe.supabase.co -p 5432 -U postgres -d postgres -f $phase1Migration
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✓ Phase 1 Settings migrations applied successfully" -ForegroundColor Green
    } else {
        Write-Host "✗ Phase 1 Settings migrations failed" -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "⚠ Phase 1 Settings migration file not found: $phase1Migration" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Step 2: Applying Phase 2 Settings migrations (Approval Workflows, Business Hours, Escalation Rules)..." -ForegroundColor Yellow
$phase2Migration = Join-Path $migrationPath "AddPhase2SettingsEntities.sql"
if (Test-Path $phase2Migration) {
    $env:PGPASSWORD = "J@saw007"
    & psql -h db.jgahsbfoydwdgipcjvxe.supabase.co -p 5432 -U postgres -d postgres -f $phase2Migration
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✓ Phase 2 Settings migrations applied successfully" -ForegroundColor Green
    } else {
        Write-Host "✗ Phase 2 Settings migrations failed" -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "✗ Phase 2 Settings migration file not found: $phase2Migration" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "All migrations completed successfully!" -ForegroundColor Green
Write-Host "============================================" -ForegroundColor Cyan

