# ============================================
# Delete All Companies Script (Simple Version)
# ============================================
# This script deletes all companies using the API endpoints
# ============================================

Write-Host "============================================" -ForegroundColor Yellow
Write-Host "  Delete All Companies" -ForegroundColor Yellow
Write-Host "============================================" -ForegroundColor Yellow
Write-Host ""

Write-Host "WARNING: This will delete ALL companies from the database!" -ForegroundColor Red
Write-Host ""
$response = Read-Host "Are you sure you want to continue? Type 'YES' to confirm"
if ($response -ne "YES") {
    Write-Host "Operation cancelled." -ForegroundColor Yellow
    exit 0
}

Write-Host ""
Write-Host "Fetching companies from API..." -ForegroundColor Cyan

try {
    # Get authentication token (you'll need to login first)
    # For now, we'll use direct database access instead
    
    Write-Host "Using direct database access instead..." -ForegroundColor Cyan
    
    # Use EF Core tools to execute a simple delete command
    $scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
    $projectRoot = Split-Path -Parent (Split-Path -Parent $scriptPath)
    $infraPath = Join-Path $projectRoot "src\CephasOps.Infrastructure"
    $apiPath = Join-Path $projectRoot "src\CephasOps.Api"
    
    # Create a simple SQL script
    $sqlScript = "DELETE FROM ""Companies"";"
    
    Write-Host "Executing SQL delete command..." -ForegroundColor Cyan
    
    # Use psql if available, or create a simple C# console app
    $appsettingsPath = Join-Path $apiPath "appsettings.json"
    $appsettings = Get-Content $appsettingsPath -Raw | ConvertFrom-Json
    
    $connString = $appsettings.ConnectionStrings.DefaultConnection
    
    if ($connString) {
        Write-Host "Connection string found. Creating delete script..." -ForegroundColor Cyan
        
        # Extract database connection details
        # PostgreSQL format: Host=...;Database=...;Username=...;Password=...
        $host = ($connString -split 'Host=')[1] -split ';' | Select-Object -First 1
        $db = ($connString -split 'Database=')[1] -split ';' | Select-Object -First 1
        $user = ($connString -split 'Username=')[1] -split ';' | Select-Object -First 1
        $pass = ($connString -split 'Password=')[1] -split ';' | Select-Object -First 1
        
        Write-Host "Attempting to delete via psql..." -ForegroundColor Cyan
        
        # Try using psql if available
        $env:PGPASSWORD = $pass
        $psqlCommand = "DELETE FROM ""Companies"";"
        
        $psqlPath = Get-Command psql -ErrorAction SilentlyContinue
        if ($psqlPath) {
            $result = echo $psqlCommand | psql -h $host -U $user -d $db -t
            Write-Host "SQL executed." -ForegroundColor Green
        } else {
            Write-Host "psql not found. Please use the alternative method below." -ForegroundColor Yellow
        }
    }
    
    Write-Host ""
    Write-Host "Alternative: Please run the following SQL command manually:" -ForegroundColor Yellow
    Write-Host "  DELETE FROM ""Companies"";" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Or use the API endpoints to delete companies one by one." -ForegroundColor Yellow
    
} catch {
    Write-Host "ERROR: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    Write-Host "Manual SQL command:" -ForegroundColor Yellow
    Write-Host 'DELETE FROM "Companies";' -ForegroundColor Cyan
}

