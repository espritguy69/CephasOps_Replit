# Quick script to delete all companies using direct SQL

$ErrorActionPreference = "Stop"

Write-Host "Deleting all companies..." -ForegroundColor Yellow
Write-Host ""

# Read connection string from appsettings
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent (Split-Path -Parent $scriptPath)
$apiPath = Join-Path $projectRoot "src\CephasOps.Api\appsettings.json"
$appsettings = Get-Content $apiPath -Raw | ConvertFrom-Json
$connString = $appsettings.ConnectionStrings.DefaultConnection

# Parse connection string
$db = ($connString -split 'Database=')[1] -split ';' | Select-Object -First 1
$host = ($connString -split 'Host=')[1] -split ';' | Select-Object -First 1
$user = ($connString -split 'Username=')[1] -split ';' | Select-Object -First 1
$pass = ($connString -split 'Password=')[1] -split ';' | Select-Object -First 1

Write-Host "Connecting to database: $db" -ForegroundColor Cyan

# Set password environment variable
$env:PGPASSWORD = $pass

try {
    # Execute DELETE command
    $sqlCommand = 'DELETE FROM "Companies";'
    echo $sqlCommand | psql -h $host -p 5432 -U $user -d $db
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host "Successfully deleted all companies!" -ForegroundColor Green
    } else {
        Write-Host "Error executing command. Exit code: $LASTEXITCODE" -ForegroundColor Red
        Write-Host ""
        Write-Host "Please run this SQL manually:" -ForegroundColor Yellow
        Write-Host 'DELETE FROM "Companies";' -ForegroundColor Cyan
    }
} catch {
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    Write-Host "Please run this SQL manually in your database tool:" -ForegroundColor Yellow
    Write-Host 'DELETE FROM "Companies";' -ForegroundColor Cyan
} finally {
    Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue
}

