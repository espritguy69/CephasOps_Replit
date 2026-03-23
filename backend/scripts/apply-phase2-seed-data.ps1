# Apply Phase 2 Settings Seed Data Script
# This script applies seed data for Guard Condition Definitions and Side Effect Definitions

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Phase 2 Settings Seed Data Application" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Database connection details
$dbHost = "db.jgahsbfoydwdgipcjvxe.supabase.co"
$dbPort = "5432"
$dbName = "postgres"
$dbUser = "postgres"
$dbPassword = "J@saw007"

# Set password as environment variable
$env:PGPASSWORD = $dbPassword

# SQL script path
$seedScriptPath = Join-Path $PSScriptRoot "..\src\CephasOps.Infrastructure\Persistence\Migrations\SeedGuardConditionsAndSideEffects_PostgreSQL.sql"

if (-not (Test-Path $seedScriptPath)) {
    Write-Host "ERROR: Seed script not found at: $seedScriptPath" -ForegroundColor Red
    exit 1
}

Write-Host "Applying seed data..." -ForegroundColor Yellow
Write-Host "Script: $seedScriptPath" -ForegroundColor Gray
Write-Host ""

try {
    # Execute SQL script
    $result = & psql -h $dbHost -p $dbPort -U $dbUser -d $dbName -f $seedScriptPath 2>&1
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Seed data applied successfully!" -ForegroundColor Green
        Write-Host ""
        
        # Verify data was inserted
        Write-Host "Verifying seed data..." -ForegroundColor Yellow
        
        $verifyQuery = @"
SELECT 
    (SELECT COUNT(*) FROM guard_condition_definitions WHERE entity_type = 'Order') as guard_conditions_count,
    (SELECT COUNT(*) FROM side_effect_definitions WHERE entity_type = 'Order') as side_effects_count;
"@
        
        $verifyResult = & psql -h $dbHost -p $dbPort -U $dbUser -d $dbName -t -c $verifyQuery 2>&1
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✅ Verification complete" -ForegroundColor Green
            Write-Host $verifyResult -ForegroundColor Gray
        } else {
            Write-Host "⚠️  Could not verify seed data (but script executed)" -ForegroundColor Yellow
        }
    } else {
        Write-Host "❌ Error applying seed data:" -ForegroundColor Red
        Write-Host $result -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "❌ Error: $_" -ForegroundColor Red
    exit 1
} finally {
    # Clear password from environment
    Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Seed Data Application Complete" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

