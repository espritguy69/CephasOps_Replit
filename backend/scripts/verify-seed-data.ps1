# ============================================
# Verify Seed Data
# ============================================
# Runs verification queries to check seed data loaded correctly
# ============================================

param(
    [string]$Host = "localhost",
    [int]$Port = 5432,
    [string]$Database = "cephasops",
    [string]$Username = "postgres",
    [string]$Password = ""
)

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Seed Data Verification" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Get password if not provided
if ([string]::IsNullOrEmpty($Password)) {
    $securePassword = Read-Host "Enter PostgreSQL password" -AsSecureString
    $BSTR = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePassword)
    $Password = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($BSTR)
}

$env:PGPASSWORD = $Password

# Verification query
$verificationQuery = @"
SELECT 
    (SELECT COUNT(*) FROM "Companies") as companies,
    (SELECT COUNT(*) FROM "Roles") as roles,
    (SELECT COUNT(*) FROM "Users") as users,
    (SELECT COUNT(*) FROM "UserRoles") as user_roles,
    (SELECT COUNT(*) FROM "Departments" WHERE "Code" = 'GPON') as gpon_departments,
    (SELECT COUNT(*) FROM "OrderTypes") as order_types,
    (SELECT COUNT(*) FROM "OrderCategories") as order_categories,
    (SELECT COUNT(*) FROM "BuildingTypes") as building_types,
    (SELECT COUNT(*) FROM "SplitterTypes") as splitter_types,
    (SELECT COUNT(*) FROM "Materials") as materials,
    (SELECT COUNT(*) FROM "MaterialCategories") as material_categories,
    (SELECT COUNT(*) FROM "ParserTemplates") as parser_templates,
    (SELECT COUNT(*) FROM "GuardConditionDefinitions") as guard_conditions,
    (SELECT COUNT(*) FROM "SideEffectDefinitions") as side_effects,
    (SELECT COUNT(*) FROM "GlobalSettings") as global_settings,
    (SELECT COUNT(*) FROM "MovementTypes") as movement_types,
    (SELECT COUNT(*) FROM "LocationTypes") as location_types,
    (SELECT COUNT(*) FROM "DocumentPlaceholderDefinitions") as document_placeholders;
"@

Write-Host "Running verification query..." -ForegroundColor Cyan
Write-Host ""

try {
    $result = & psql -h $Host -p $Port -U $Username -d $Database -c $verificationQuery -t -A -F "|"
    
    if ($LASTEXITCODE -eq 0) {
        $values = $result -split '\|'
        
        Write-Host "Results:" -ForegroundColor Yellow
        Write-Host "  Companies:                    $($values[0])" -ForegroundColor $(if ([int]$values[0] -ge 1) { "Green" } else { "Red" })
        Write-Host "  Roles:                        $($values[1])" -ForegroundColor $(if ([int]$values[1] -ge 4) { "Green" } else { "Red" })
        Write-Host "  Users:                        $($values[2])" -ForegroundColor $(if ([int]$values[2] -ge 2) { "Green" } else { "Red" })
        Write-Host "  UserRoles:                     $($values[3])" -ForegroundColor $(if ([int]$values[3] -ge 2) { "Green" } else { "Red" })
        Write-Host "  GPON Departments:              $($values[4])" -ForegroundColor $(if ([int]$values[4] -ge 1) { "Green" } else { "Red" })
        Write-Host "  OrderTypes:                    $($values[5])" -ForegroundColor $(if ([int]$values[5] -ge 5) { "Green" } else { "Red" })
        Write-Host "  OrderCategories:               $($values[6])" -ForegroundColor $(if ([int]$values[6] -ge 4) { "Green" } else { "Red" })
        Write-Host "  BuildingTypes:                $($values[7])" -ForegroundColor $(if ([int]$values[7] -ge 15) { "Green" } else { "Red" })
        Write-Host "  SplitterTypes:                $($values[8])" -ForegroundColor $(if ([int]$values[8] -ge 3) { "Green" } else { "Red" })
        Write-Host "  Materials:                     $($values[9])" -ForegroundColor $(if ([int]$values[9] -ge 50) { "Green" } else { "Red" })
        Write-Host "  MaterialCategories:           $($values[10])" -ForegroundColor $(if ([int]$values[10] -ge 8) { "Green" } else { "Red" })
        Write-Host "  ParserTemplates:              $($values[11])" -ForegroundColor $(if ([int]$values[11] -ge 9) { "Green" } else { "Red" })
        Write-Host "  GuardConditionDefinitions:     $($values[12])" -ForegroundColor $(if ([int]$values[12] -ge 10) { "Green" } else { "Red" })
        Write-Host "  SideEffectDefinitions:         $($values[13])" -ForegroundColor $(if ([int]$values[13] -ge 5) { "Green" } else { "Red" })
        Write-Host "  GlobalSettings:               $($values[14])" -ForegroundColor $(if ([int]$values[14] -ge 30) { "Green" } else { "Red" })
        Write-Host "  MovementTypes:                $($values[15])" -ForegroundColor $(if ([int]$values[15] -ge 11) { "Green" } else { "Red" })
        Write-Host "  LocationTypes:                $($values[16])" -ForegroundColor $(if ([int]$values[16] -ge 6) { "Green" } else { "Red" })
        Write-Host "  DocumentPlaceholderDefinitions: $($values[17])" -ForegroundColor $(if ([int]$values[17] -ge 150) { "Green" } else { "Red" })
        
        Write-Host ""
        
        # Check for critical failures
        $failures = @()
        if ([int]$values[0] -lt 1) { $failures += "Companies" }
        if ([int]$values[1] -lt 4) { $failures += "Roles" }
        if ([int]$values[2] -lt 2) { $failures += "Users" }
        if ([int]$values[4] -lt 1) { $failures += "GPON Departments" }
        
        if ($failures.Count -gt 0) {
            Write-Host "✗ CRITICAL: Missing required data:" -ForegroundColor Red
            foreach ($failure in $failures) {
                Write-Host "    - $failure" -ForegroundColor Red
            }
            Write-Host ""
            Write-Host "Please re-run seed scripts for missing data." -ForegroundColor Yellow
            exit 1
        } else {
            Write-Host "✓ All critical data verified!" -ForegroundColor Green
        }
        
    } else {
        Write-Host "✗ Query failed with exit code: $LASTEXITCODE" -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "✗ Error: $_" -ForegroundColor Red
    exit 1
} finally {
    $env:PGPASSWORD = $null
}

Write-Host ""
Write-Host "Verification complete!" -ForegroundColor Green
