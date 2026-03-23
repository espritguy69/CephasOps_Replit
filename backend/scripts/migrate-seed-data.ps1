# ============================================
# Complete Seed Data Migration Script
# ============================================
# Orchestrates the entire migration process
# ============================================

param(
    [string]$DatabaseHost = "localhost",
    [int]$DatabasePort = 5432,
    [string]$DatabaseName = "cephasops",
    [string]$DatabaseUser = "postgres",
    [string]$DatabasePassword = "",
    [switch]$SkipBackup = $false,
    [switch]$SkipCodeRemoval = $false,
    [switch]$DryRun = $false
)

$ErrorActionPreference = "Stop"

$projectRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Seed Data Migration - Complete Process" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

if ($DryRun) {
    Write-Host "DRY RUN MODE - No changes will be made" -ForegroundColor Yellow
    Write-Host ""
}

# Step 1: Backup Database
if (-not $SkipBackup -and -not $DryRun) {
    Write-Host "Step 1: Backup Database" -ForegroundColor Cyan
    Write-Host "----------------------" -ForegroundColor Cyan
    
    $timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
    $backupFile = "$projectRoot\backup_before_seed_removal_$timestamp.dump"
    
    if ([string]::IsNullOrEmpty($DatabasePassword)) {
        $securePassword = Read-Host "Enter PostgreSQL password" -AsSecureString
        $BSTR = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePassword)
        $DatabasePassword = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($BSTR)
    }
    
    $env:PGPASSWORD = $DatabasePassword
    
    Write-Host "Creating backup: $backupFile" -ForegroundColor Yellow
    try {
        & pg_dump -h $DatabaseHost -p $DatabasePort -U $DatabaseUser -d $DatabaseName -F c -f $backupFile
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✓ Backup created successfully" -ForegroundColor Green
        } else {
            Write-Host "✗ Backup failed" -ForegroundColor Red
            exit 1
        }
    } catch {
        Write-Host "✗ Backup error: $_" -ForegroundColor Red
        exit 1
    }
    
    Write-Host ""
}

# Step 2: Remove C# Seeding Code
if (-not $SkipCodeRemoval) {
    Write-Host "Step 2: Remove C# Seeding Code" -ForegroundColor Cyan
    Write-Host "------------------------------" -ForegroundColor Cyan
    
    if ($DryRun) {
        Write-Host "Would remove:" -ForegroundColor Yellow
        Write-Host "  - DatabaseSeeder.cs" -ForegroundColor Gray
        Write-Host "  - DocumentPlaceholderSeeder.cs" -ForegroundColor Gray
        Write-Host "  - Seeding block from Program.cs" -ForegroundColor Gray
    } else {
        & "$PSScriptRoot\remove-seeding-code.ps1" -Backup
        if ($LASTEXITCODE -ne 0) {
            Write-Host "✗ Code removal failed" -ForegroundColor Red
            exit 1
        }
    }
    
    Write-Host ""
}

# Step 3: Run PostgreSQL Seed Scripts
Write-Host "Step 3: Import PostgreSQL Seed Scripts" -ForegroundColor Cyan
Write-Host "--------------------------------------" -ForegroundColor Cyan

if ($DryRun) {
    Write-Host "Would execute seed scripts in order:" -ForegroundColor Yellow
    Write-Host "  01_system_data.sql" -ForegroundColor Gray
    Write-Host "  02_reference_data.sql" -ForegroundColor Gray
    Write-Host "  03_master_data.sql" -ForegroundColor Gray
    Write-Host "  04_configuration_data.sql" -ForegroundColor Gray
    Write-Host "  05_inventory_data.sql" -ForegroundColor Gray
    Write-Host "  06_document_placeholders.sql" -ForegroundColor Gray
} else {
    $seedScriptPath = Join-Path $PSScriptRoot "postgresql-seeds\run-all-seeds.ps1"
    & $seedScriptPath -Host $DatabaseHost -Port $DatabasePort -Database $DatabaseName -Username $DatabaseUser -Password $DatabasePassword
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "✗ Seed script execution failed" -ForegroundColor Red
        exit 1
    }
}

Write-Host ""

# Step 4: Verify Data
Write-Host "Step 4: Verify Seed Data" -ForegroundColor Cyan
Write-Host "-----------------------" -ForegroundColor Cyan

if (-not $DryRun) {
    $verifyScriptPath = Join-Path $PSScriptRoot "verify-seed-data.ps1"
    & $verifyScriptPath -Host $DatabaseHost -Port $DatabasePort -Database $DatabaseName -Username $DatabaseUser -Password $DatabasePassword
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "✗ Verification failed" -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "Would run verification queries" -ForegroundColor Yellow
}

Write-Host ""

# Summary
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Migration Summary" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

if ($DryRun) {
    Write-Host "DRY RUN - No changes made" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "To execute migration, run without -DryRun flag:" -ForegroundColor Yellow
    Write-Host "  .\migrate-seed-data.ps1" -ForegroundColor Gray
} else {
    Write-Host "✓ Migration completed successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Yellow
    Write-Host "  1. Build and test the application" -ForegroundColor Gray
    Write-Host "  2. Test login with seeded users" -ForegroundColor Gray
    Write-Host "  3. Verify all functionality works" -ForegroundColor Gray
    Write-Host "  4. Commit changes to Git" -ForegroundColor Gray
}

Write-Host ""

# Clear password
$env:PGPASSWORD = $null

