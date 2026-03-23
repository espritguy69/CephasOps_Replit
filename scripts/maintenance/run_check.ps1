# PowerShell script to check current Service Installers state
# Connection details from appsettings.Development.json

$dbHost = "db.jgahsbfoydwdgipcjvxe.supabase.co"
$dbPort = "5432"
$dbName = "postgres"
$dbUser = "postgres"
$env:PGPASSWORD = "J@saw007"  # Set password as environment variable

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "SERVICE INSTALLERS DATABASE CHECK" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check if psql is available
try {
    $psqlVersion = psql --version
    Write-Host "✓ PostgreSQL client found: $psqlVersion" -ForegroundColor Green
} catch {
    Write-Host "✗ psql not found. Please install PostgreSQL client tools." -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Connecting to database: ${dbName}@${dbHost}:${dbPort}" -ForegroundColor Yellow
Write-Host "User: $dbUser" -ForegroundColor Yellow
Write-Host ""

# Run the SQL file
psql -h $dbHost -p $dbPort -U $dbUser -d $dbName -f check_current_state.sql

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Check complete!" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

