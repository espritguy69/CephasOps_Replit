# Script to list all companies from the database directly
# Uses PostgreSQL connection

$dbHost = "localhost"
$dbPort = "5432"
$dbName = "cephasops"
$dbUser = "postgres"
$dbPassword = "J@saw007"

Write-Host "Querying companies from database..." -ForegroundColor Cyan
Write-Host ""

# Check if psql is available
$psqlPath = Get-Command psql -ErrorAction SilentlyContinue

if (-not $psqlPath) {
    Write-Host "⚠ PostgreSQL client (psql) not found in PATH." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Please install PostgreSQL client tools or use one of these alternatives:" -ForegroundColor Yellow
    Write-Host "1. View companies in the UI: http://localhost:5173/settings/company" -ForegroundColor Cyan
    Write-Host "2. Use pgAdmin or another PostgreSQL client" -ForegroundColor Cyan
    exit 1
}

# Set PGPASSWORD environment variable for psql
$env:PGPASSWORD = $dbPassword

try {
    # Query companies table (table name is "Companies" in PostgreSQL)
    $query = 'SELECT "Id", "LegalName", "ShortName", "Email", "IsActive", "CreatedAt" FROM "Companies" ORDER BY "CreatedAt" DESC;'
    
    $result = psql -h $dbHost -p $dbPort -U $dbUser -d $dbName -t -A -F "|" -c $query 2>&1
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Error querying database:" -ForegroundColor Red
        Write-Host $result -ForegroundColor Red
        exit 1
    }
    
    $lines = $result | Where-Object { $_.Trim() -ne "" }
    
    if ($lines.Count -eq 0) {
        Write-Host "No companies found in the database." -ForegroundColor Yellow
    } else {
        Write-Host "Found $($lines.Count) company/companies:" -ForegroundColor Green
        Write-Host ""
        Write-Host ("=" * 100) -ForegroundColor Gray
        
        foreach ($line in $lines) {
            $fields = $line -split '\|'
            if ($fields.Count -ge 6) {
                $id = $fields[0].Trim()
                $legalName = $fields[1].Trim()
                $shortName = $fields[2].Trim()
                $email = $fields[3].Trim()
                $isActive = $fields[4].Trim()
                $createdAt = $fields[5].Trim()
                
                $status = if ($isActive -eq "True" -or $isActive -eq "t") { "Active" } else { "Inactive" }
                $statusColor = if ($isActive -eq "True" -or $isActive -eq "t") { "Green" } else { "Yellow" }
                
                Write-Host "Company: $legalName" -ForegroundColor Cyan
                if ($shortName) {
                    Write-Host "  Short Name: $shortName" -ForegroundColor Gray
                }
                if ($email) {
                    Write-Host "  Email: $email" -ForegroundColor Gray
                }
                Write-Host "  Status: $status" -ForegroundColor $statusColor
                Write-Host "  ID: $id" -ForegroundColor DarkGray
                Write-Host "  Created: $createdAt" -ForegroundColor DarkGray
                Write-Host ("-" * 100) -ForegroundColor Gray
            }
        }
    }
} catch {
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
} finally {
    # Clear password from environment
    Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue
}

