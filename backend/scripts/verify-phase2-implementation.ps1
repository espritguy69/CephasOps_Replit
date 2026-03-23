# Verify Phase 2 Settings Implementation
# This script verifies that all Phase 2 Settings tables and seed data exist

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Phase 2 Settings Implementation Verification" -ForegroundColor Cyan
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

$allTablesExist = $true
$seedDataExists = $true

try {
    Write-Host "Checking Phase 2 Settings tables..." -ForegroundColor Yellow
    
    $tables = @(
        "sla_profiles",
        "automation_rules",
        "approval_workflows",
        "approval_steps",
        "business_hours",
        "public_holidays",
        "escalation_rules",
        "guard_condition_definitions",
        "side_effect_definitions"
    )
    
    foreach ($table in $tables) {
        $checkQuery = "SELECT EXISTS (SELECT FROM information_schema.tables WHERE table_schema = 'public' AND table_name = '$table');"
        $result = & psql -h $dbHost -p $dbPort -U $dbUser -d $dbName -t -c $checkQuery 2>&1
        
        if ($result -match "t|true") {
            Write-Host "  ✅ $table exists" -ForegroundColor Green
        } else {
            Write-Host "  ❌ $table NOT FOUND" -ForegroundColor Red
            $allTablesExist = $false
        }
    }
    
    Write-Host ""
    Write-Host "Checking seed data..." -ForegroundColor Yellow
    
    # Check guard conditions
    $guardConditionsQuery = "SELECT COUNT(*) FROM guard_condition_definitions WHERE entity_type = 'Order';"
    $guardResult = & psql -h $dbHost -p $dbPort -U $dbUser -d $dbName -t -c $guardConditionsQuery 2>&1
    $guardCount = 0
    if ($guardResult) {
        $guardCountStr = ($guardResult -replace '\s', '').Trim()
        if ([int]::TryParse($guardCountStr, [ref]$guardCount)) {
            # Successfully parsed
        }
    }
    
    if ($guardCount -ge 10) {
        Write-Host "  ✅ Guard Condition Definitions: $guardCount found (expected: 10+)" -ForegroundColor Green
    } else {
        Write-Host "  ⚠️  Guard Condition Definitions: $guardCount found (expected: 10+)" -ForegroundColor Yellow
        if ($guardCount -eq 0) { $seedDataExists = $false }
    }
    
    # Check side effects
    $sideEffectsQuery = "SELECT COUNT(*) FROM side_effect_definitions WHERE entity_type = 'Order';"
    $sideEffectResult = & psql -h $dbHost -p $dbPort -U $dbUser -d $dbName -t -c $sideEffectsQuery 2>&1
    $sideEffectCount = 0
    if ($sideEffectResult) {
        $sideEffectCountStr = ($sideEffectResult -replace '\s', '').Trim()
        if ([int]::TryParse($sideEffectCountStr, [ref]$sideEffectCount)) {
            # Successfully parsed
        }
    }
    
    if ($sideEffectCount -ge 5) {
        Write-Host "  ✅ Side Effect Definitions: $sideEffectCount found (expected: 5+)" -ForegroundColor Green
    } else {
        Write-Host "  ⚠️  Side Effect Definitions: $sideEffectCount found (expected: 5+)" -ForegroundColor Yellow
        if ($sideEffectCount -eq 0) { $seedDataExists = $false }
    }
    
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Cyan
    
    if ($allTablesExist -and $seedDataExists) {
        Write-Host "✅ All Phase 2 Settings implementation verified!" -ForegroundColor Green
        exit 0
    } elseif ($allTablesExist) {
        Write-Host "⚠️  Tables exist but seed data may be incomplete" -ForegroundColor Yellow
        Write-Host "   Run apply-phase2-seed-data.ps1 to populate seed data" -ForegroundColor Yellow
        exit 1
    } else {
        Write-Host "❌ Some tables are missing. Run migrations first." -ForegroundColor Red
        exit 1
    }
    
} catch {
    Write-Host "❌ Error: $_" -ForegroundColor Red
    exit 1
} finally {
    # Clear password from environment
    Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue
}

