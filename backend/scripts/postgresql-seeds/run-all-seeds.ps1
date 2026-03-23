# ============================================
# Run All PostgreSQL Seed Scripts
# ============================================
# Executes all seed scripts in correct order
# ============================================

param(
    [string]$Host = "localhost",
    [int]$Port = 5432,
    [string]$Database = "cephasops",
    [string]$Username = "postgres",
    [string]$Password = "",
    [switch]$DryRun = $false
)

$ErrorActionPreference = "Stop"

# Get script directory
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$seedsDir = $scriptDir

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "PostgreSQL Seed Scripts Runner" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Get password if not provided
if ([string]::IsNullOrEmpty($Password)) {
    $securePassword = Read-Host "Enter PostgreSQL password" -AsSecureString
    $BSTR = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePassword)
    $Password = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($BSTR)
}

# Set environment variable for psql
$env:PGPASSWORD = $Password

# Scripts in execution order
$scripts = @(
    "01_system_data.sql",
    "02_reference_data.sql",
    "03_master_data.sql",
    "04_configuration_data.sql",
    "05_inventory_data.sql",
    "06_document_placeholders.sql",
    "07_gpon_order_workflow.sql"
)

Write-Host "Connection Details:" -ForegroundColor Yellow
Write-Host "  Host: $Host" -ForegroundColor Gray
Write-Host "  Port: $Port" -ForegroundColor Gray
Write-Host "  Database: $Database" -ForegroundColor Gray
Write-Host "  Username: $Username" -ForegroundColor Gray
Write-Host ""

if ($DryRun) {
    Write-Host "DRY RUN MODE - No changes will be made" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Scripts to execute:" -ForegroundColor Yellow
    foreach ($script in $scripts) {
        $scriptPath = Join-Path $seedsDir $script
        if (Test-Path $scriptPath) {
            Write-Host "  ✓ $script" -ForegroundColor Green
        } else {
            Write-Host "  ✗ $script (NOT FOUND)" -ForegroundColor Red
        }
    }
    Write-Host ""
    Write-Host "To execute, run without -DryRun flag" -ForegroundColor Yellow
    return
}

# Verify all scripts exist
Write-Host "Verifying scripts..." -ForegroundColor Cyan
$allExist = $true
foreach ($script in $scripts) {
    $scriptPath = Join-Path $seedsDir $script
    if (-not (Test-Path $scriptPath)) {
        Write-Host "  ✗ $script (NOT FOUND)" -ForegroundColor Red
        $allExist = $false
    } else {
        Write-Host "  ✓ $script" -ForegroundColor Green
    }
}

if (-not $allExist) {
    Write-Host ""
    Write-Host "ERROR: Some scripts are missing. Please check the file paths." -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Executing scripts in order..." -ForegroundColor Cyan
Write-Host ""

$successCount = 0
$failCount = 0

foreach ($script in $scripts) {
    $scriptPath = Join-Path $seedsDir $script
    Write-Host "Executing: $script" -ForegroundColor Yellow
    
    try {
        $result = & psql -h $Host -p $Port -U $Username -d $Database -f $scriptPath 2>&1
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "  ✓ Success" -ForegroundColor Green
            $successCount++
        } else {
            Write-Host "  ✗ Failed (Exit Code: $LASTEXITCODE)" -ForegroundColor Red
            Write-Host $result -ForegroundColor Red
            $failCount++
        }
    } catch {
        Write-Host "  ✗ Error: $_" -ForegroundColor Red
        $failCount++
    }
    
    Write-Host ""
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Execution Summary" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Success: $successCount" -ForegroundColor Green
Write-Host "Failed:  $failCount" -ForegroundColor $(if ($failCount -gt 0) { "Red" } else { "Gray" })
Write-Host ""

if ($failCount -eq 0) {
    Write-Host "✓ All scripts executed successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Yellow
    Write-Host "  1. Verify data with verification queries" -ForegroundColor Gray
    Write-Host "  2. Test application login" -ForegroundColor Gray
    Write-Host "  3. Test basic functionality" -ForegroundColor Gray
} else {
    Write-Host "✗ Some scripts failed. Please review errors above." -ForegroundColor Red
    exit 1
}

# Clear password from environment
$env:PGPASSWORD = $null

