# ============================================================================
# PowerShell Script: Cleanup Email Parser Data
# ============================================================================
# This script connects to PostgreSQL and removes all email-related parsing data
# ============================================================================

$ErrorActionPreference = "Stop"

# Database connection parameters
$dbHost = "localhost"
$dbPort = "5432"
$dbName = "cephasops"
$dbUser = "postgres"
$dbPassword = "J@saw007"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Email Parser Data Cleanup Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Confirm before proceeding
$confirm = Read-Host "⚠️  WARNING: This will DELETE all email messages, parse sessions, and drafts from emails. Continue? (yes/no)"
if ($confirm -ne "yes") {
    Write-Host "Cleanup cancelled." -ForegroundColor Yellow
    exit 0
}

Write-Host ""
Write-Host "Connecting to database..." -ForegroundColor Yellow

# Set PGPASSWORD environment variable for psql
$env:PGPASSWORD = $dbPassword

try {
    # Read SQL script
    $sqlScript = Get-Content -Path "$PSScriptRoot\cleanup-email-parser-data.sql" -Raw
    
    # Execute SQL script
    Write-Host "Executing cleanup script..." -ForegroundColor Yellow
    
    $result = & psql -h $dbHost -p $dbPort -U $dbUser -d $dbName -c $sqlScript 2>&1
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host "✅ Cleanup completed successfully!" -ForegroundColor Green
        Write-Host ""
        
        # Show verification counts
        Write-Host "Verification:" -ForegroundColor Cyan
        & psql -h $dbHost -p $dbPort -U $dbUser -d $dbName -c "SELECT COUNT(*) as remaining_drafts FROM `"ParsedOrderDrafts`";" 2>&1 | Out-Null
        & psql -h $dbHost -p $dbPort -U $dbUser -d $dbName -c "SELECT COUNT(*) as remaining_sessions FROM `"ParseSessions`";" 2>&1 | Out-Null
        & psql -h $dbHost -p $dbPort -U $dbUser -d $dbName -c "SELECT COUNT(*) as remaining_emails FROM `"EmailMessages`";" 2>&1 | Out-Null
        & psql -h $dbHost -p $dbPort -U $dbUser -d $dbName -c "SELECT COUNT(*) as remaining_attachments FROM `"EmailAttachments`";" 2>&1 | Out-Null
    } else {
        Write-Host ""
        Write-Host "❌ Error during cleanup:" -ForegroundColor Red
        Write-Host $result -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host ""
    Write-Host "❌ Error: $_" -ForegroundColor Red
    exit 1
} finally {
    # Clear password from environment
    Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue
}

Write-Host ""
Write-Host "Done!" -ForegroundColor Green

