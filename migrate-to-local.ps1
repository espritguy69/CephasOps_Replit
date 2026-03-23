# Migrate Supabase to Local PostgreSQL
param(
    [string]$LocalPassword = "postgres",
    [string]$BackupPath = "C:\Projects\CephasOps\supabase_backup.sql"
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Supabase to Local PostgreSQL Migration" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Export from Supabase
Write-Host "Step 1: Exporting database from Supabase..." -ForegroundColor Yellow
$env:PGPASSWORD = "J@saw007"

$timestamp = Get-Date -Format 'yyyyMMdd_HHmmss'
$BackupPath = "C:\Projects\CephasOps\supabase_backup_$timestamp.sql"

try {
    pg_dump -h db.jgahsbfoydwdgipcjvxe.supabase.co -p 5432 -U postgres -d postgres `
        --clean --if-exists --format=plain --verbose `
        --file=$BackupPath
    
    if ($LASTEXITCODE -eq 0) {
        $fileSize = (Get-Item $BackupPath).Length / 1MB
        Write-Host "✓ Export completed successfully!" -ForegroundColor Green
        Write-Host "  Backup file: $BackupPath" -ForegroundColor Gray
        Write-Host "  File size: $([math]::Round($fileSize, 2)) MB" -ForegroundColor Gray
    } else {
        Write-Host "✗ Export failed with exit code: $LASTEXITCODE" -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "✗ Error during export: $_" -ForegroundColor Red
    exit 1
}

Write-Host ""

# Step 2: Create local database
Write-Host "Step 2: Creating local database..." -ForegroundColor Yellow
$env:PGPASSWORD = $LocalPassword

try {
    # Drop existing database if it exists
    psql -h localhost -U postgres -c "DROP DATABASE IF EXISTS cephasops;" 2>&1 | Out-Null
    
    # Create new database
    psql -h localhost -U postgres -c "CREATE DATABASE cephasops;"
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✓ Local database created successfully!" -ForegroundColor Green
    } else {
        Write-Host "✗ Failed to create local database" -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "✗ Error creating database: $_" -ForegroundColor Red
    exit 1
}

Write-Host ""

# Step 3: Restore to local database
Write-Host "Step 3: Restoring data to local database..." -ForegroundColor Yellow
Write-Host "  This may take a few minutes depending on database size..." -ForegroundColor Gray

try {
    psql -h localhost -U postgres -d cephasops -f $BackupPath 2>&1 | Tee-Object -Variable restoreOutput
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✓ Restore completed successfully!" -ForegroundColor Green
    } else {
        Write-Host "✗ Restore failed with exit code: $LASTEXITCODE" -ForegroundColor Red
        Write-Host "Last 20 lines of output:" -ForegroundColor Yellow
        $restoreOutput | Select-Object -Last 20
        exit 1
    }
} catch {
    Write-Host "✗ Error during restore: $_" -ForegroundColor Red
    exit 1
}

Write-Host ""

# Step 4: Verify migration
Write-Host "Step 4: Verifying migration..." -ForegroundColor Yellow

try {
    $tableCount = psql -h localhost -U postgres -d cephasops -t -c "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'public';" | ForEach-Object { $_.Trim() }
    $userCount = psql -h localhost -U postgres -d cephasops -t -c "SELECT COUNT(*) FROM \"Users\";" | ForEach-Object { $_.Trim() }
    $orderCount = psql -h localhost -U postgres -d cephasops -t -c "SELECT COUNT(*) FROM \"Orders\";" | ForEach-Object { $_.Trim() }
    $companyCount = psql -h localhost -U postgres -d cephasops -t -c "SELECT COUNT(*) FROM \"Companies\";" | ForEach-Object { $_.Trim() }
    
    Write-Host "✓ Verification complete!" -ForegroundColor Green
    Write-Host "  Tables: $tableCount" -ForegroundColor Gray
    Write-Host "  Users: $userCount" -ForegroundColor Gray
    Write-Host "  Orders: $orderCount" -ForegroundColor Gray
    Write-Host "  Companies: $companyCount" -ForegroundColor Gray
} catch {
    Write-Host "⚠ Warning: Could not verify migration details" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "  Migration Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "  1. Update appsettings.json with local connection string" -ForegroundColor White
Write-Host "  2. Restart the backend application" -ForegroundColor White
Write-Host "  3. Test the application" -ForegroundColor White
Write-Host ""
Write-Host "Backup file saved at: $BackupPath" -ForegroundColor Gray

