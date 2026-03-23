# ============================================================================
# Enqueue one stock-by-location snapshot job (yesterday, Daily) - Phase 2.2.2
# Usage: .\enqueue-stock-snapshot-job.ps1
# ============================================================================

$ErrorActionPreference = "Stop"

$dbHost = "localhost"
$dbPort = "5432"
$dbName = "cephasops"
$dbUser = "postgres"
$dbPassword = "J@saw007"

$yesterday = [DateTime]::UtcNow.Date.AddDays(-1).ToString("o")
$payload = '{"periodEndDate":"' + $yesterday + '","snapshotType":"Daily"}'

$sql = @"
INSERT INTO "BackgroundJobs" ("Id", "JobType", "PayloadJson", "State", "RetryCount", "MaxRetries", "Priority", "ScheduledAt", "CreatedAt", "UpdatedAt")
VALUES (gen_random_uuid(), 'populatestockbylocationsnapshots', '$payload'::jsonb, 'Queued', 0, 3, 0, NOW(), NOW(), NOW());
"@

$psqlPath = Get-Command psql -ErrorAction SilentlyContinue
if (-not $psqlPath) {
    Write-Host "ERROR: psql not found. Install PostgreSQL client tools or run the API so the scheduler enqueues within 6 hours." -ForegroundColor Red
    exit 1
}

Write-Host "Enqueuing stock-by-location snapshot job for period $yesterday..." -ForegroundColor Cyan
$env:PGPASSWORD = $dbPassword
$result = & psql -h $dbHost -p $dbPort -U $dbUser -d $dbName -c $sql 2>&1
if ($LASTEXITCODE -eq 0) {
    Write-Host "Done. Job queued. Run the API so BackgroundJobProcessorService can process it." -ForegroundColor Green
} else {
    Write-Host "Error: $result" -ForegroundColor Red
    exit 1
}
