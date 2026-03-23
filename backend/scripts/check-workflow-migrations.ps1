# PowerShell script to check WorkflowDefinitions table and migration status
# Usage: .\check-workflow-migrations.ps1

param(
    [string]$ConnectionString = "Host=db.jgahsbfoydwdgipcjvxe.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=J@saw007;SslMode=Require"
)

$ErrorActionPreference = "Stop"

Write-Host "===========================================" -ForegroundColor Cyan
Write-Host "WorkflowDefinitions Migration Check" -ForegroundColor Cyan
Write-Host "===========================================" -ForegroundColor Cyan
Write-Host ""

# Parse connection string
$dbHost = ""
$database = ""
$username = ""
$password = ""
$port = 5432

$parts = $ConnectionString -split ";"
foreach ($part in $parts) {
    if ($part -match "Host=(.+)") {
        $dbHost = $matches[1]
    }
    elseif ($part -match "Database=(.+)") {
        $database = $matches[1]
    }
    elseif ($part -match "Port=(\d+)") {
        $port = [int]$matches[1]
    }
    elseif ($part -match "Username=(.+)") {
        $username = $matches[1]
    }
    elseif ($part -match "Password=(.+)") {
        $password = $matches[1]
    }
}

if (-not $dbHost -or -not $database -or -not $username) {
    Write-Host "Error: Invalid connection string. Required: Host, Database, Username" -ForegroundColor Red
    exit 1
}

# Set PGPASSWORD environment variable
$env:PGPASSWORD = $password

try {
    Write-Host "Checking WorkflowDefinitions table..." -ForegroundColor Yellow
    Write-Host ""
    
    # Check if table exists
    $tableExistsQuery = @"
SELECT EXISTS (
    SELECT FROM information_schema.tables 
    WHERE table_schema = 'public' 
    AND table_name = 'WorkflowDefinitions'
) as table_exists;
"@
    
    $tableExists = & psql -h $dbHost -p $port -d $database -U $username -t -A -c $tableExistsQuery 2>&1
    
    if ($tableExists -match "t") {
        Write-Host "✓ WorkflowDefinitions table EXISTS" -ForegroundColor Green
        Write-Host ""
        
        # Check table columns
        Write-Host "Checking table columns..." -ForegroundColor Yellow
        $columnsQuery = @"
SELECT column_name, data_type, is_nullable, column_default
FROM information_schema.columns
WHERE table_name = 'WorkflowDefinitions'
ORDER BY ordinal_position;
"@
        
        $columns = & psql -h $dbHost -p $port -d $database -U $username -c $columnsQuery 2>&1
        Write-Host $columns
        Write-Host ""
        
        # Check for required columns
        $requiredColumns = @("Id", "CompanyId", "DepartmentId", "Name", "EntityType", "IsActive", "IsDeleted", "RowVersion", "CreatedAt", "UpdatedAt", "DeletedAt", "DeletedByUserId")
        $missingColumns = @()
        
        foreach ($col in $requiredColumns) {
            $colCheckQuery = "SELECT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'WorkflowDefinitions' AND column_name = '$col') as exists;"
            $colExists = & psql -h $dbHost -p $port -d $database -U $username -t -A -c $colCheckQuery 2>&1
            if ($colExists -notmatch "t") {
                $missingColumns += $col
            }
        }
        
        if ($missingColumns.Count -gt 0) {
            Write-Host "⚠ Missing columns:" -ForegroundColor Yellow
            foreach ($col in $missingColumns) {
                Write-Host "  - $col" -ForegroundColor Red
            }
            Write-Host ""
        } else {
            Write-Host "✓ All required columns exist" -ForegroundColor Green
            Write-Host ""
        }
        
        # Check row count
        Write-Host "Checking workflow definitions count..." -ForegroundColor Yellow
        $countQuery = 'SELECT COUNT(*) as total, COUNT(CASE WHEN "IsDeleted" = false THEN 1 END) as not_deleted, COUNT(CASE WHEN "IsDeleted" = true THEN 1 END) as deleted, COUNT(CASE WHEN "IsActive" = true AND "IsDeleted" = false THEN 1 END) as active_not_deleted FROM "WorkflowDefinitions";'
        $countResult = & psql -h $dbHost -p $port -d $database -U $username -c $countQuery 2>&1
        Write-Host $countResult
        Write-Host ""
        
        # Show ALL workflows (including deleted)
        Write-Host "ALL workflow definitions (including deleted):" -ForegroundColor Yellow
        $allQuery = 'SELECT "Id", "Name", "EntityType", "CompanyId", "DepartmentId", "IsActive", "IsDeleted", "CreatedAt" FROM "WorkflowDefinitions" ORDER BY "CreatedAt" DESC;'
        $allResult = & psql -h $dbHost -p $port -d $database -U $username -c $allQuery 2>&1
        Write-Host $allResult
        Write-Host ""
        
        # Show only active, not-deleted workflows (what the API should return)
        Write-Host "Active, not-deleted workflow definitions (what API should return):" -ForegroundColor Yellow
        $activeQuery = 'SELECT "Id", "Name", "EntityType", "CompanyId", "DepartmentId", "IsActive", "IsDeleted" FROM "WorkflowDefinitions" WHERE "IsDeleted" = false ORDER BY "EntityType", "Name";'
        $activeResult = & psql -h $dbHost -p $port -d $database -U $username -c $activeQuery 2>&1
        Write-Host $activeResult
        Write-Host ""
        
    } else {
        Write-Host "✗ WorkflowDefinitions table DOES NOT EXIST" -ForegroundColor Red
        Write-Host ""
        Write-Host "You need to apply the Phase 6 Workflow migration:" -ForegroundColor Yellow
        Write-Host "  .\backend\src\CephasOps.Infrastructure\Persistence\Migrations\ApplyPhase6WorkflowMigration.ps1" -ForegroundColor White
        Write-Host ""
    }
    
    # Check EF Core migration history
    Write-Host "Checking EF Core migration history..." -ForegroundColor Yellow
    Write-Host ""
    
    $migrationHistoryQuery = 'SELECT "MigrationId", "ProductVersion" FROM "__EFMigrationsHistory" WHERE "MigrationId" LIKE ''%Workflow%'' OR "MigrationId" LIKE ''%Phase6%'' ORDER BY "MigrationId" DESC;'
    
    $migrationHistory = & psql -h $dbHost -p $port -d $database -U $username -c $migrationHistoryQuery 2>&1
    
    if ($migrationHistory -match "MigrationId") {
        Write-Host "Workflow-related migrations in history:" -ForegroundColor Cyan
        Write-Host $migrationHistory
        Write-Host ""
    } else {
        Write-Host "No workflow-related migrations found in history" -ForegroundColor Yellow
        Write-Host ""
    }
    
    # List all recent migrations
    Write-Host "Recent migrations (last 10):" -ForegroundColor Yellow
    $recentMigrationsQuery = 'SELECT "MigrationId" FROM "__EFMigrationsHistory" ORDER BY "MigrationId" DESC LIMIT 10;'
    $recentMigrations = & psql -h $dbHost -p $port -d $database -U $username -c $recentMigrationsQuery 2>&1
    Write-Host $recentMigrations
    Write-Host ""
    
} catch {
    Write-Host ""
    Write-Host "Error: Failed to check migrations" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
} finally {
    # Clear password from environment
    Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue
}

Write-Host "===========================================" -ForegroundColor Cyan
Write-Host "Check complete" -ForegroundColor Cyan
Write-Host "===========================================" -ForegroundColor Cyan

