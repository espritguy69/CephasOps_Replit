# Script to delete specific companies from database
$env:PGPASSWORD = "J@saw007"

$companiesToDelete = @(
    @{ Id = "e2d4389a-7e75-4c20-85c0-acc24fd8de86"; Name = "test" },
    @{ Id = "22fbdc7c-c7ce-4f86-9589-207408f58ed4"; Name = "Cephas Sdn Bhd" }
)

Write-Host "Deleting companies from database..." -ForegroundColor Cyan
Write-Host ""

foreach ($company in $companiesToDelete) {
    $id = $company.Id
    $name = $company.Name
    
    Write-Host "Deleting: $name (ID: $id)" -ForegroundColor Yellow
    
    $query = "DELETE FROM `"Companies`" WHERE `"Id`" = '$id';"
    $result = psql -h localhost -p 5432 -U postgres -d cephasops -c $query 2>&1
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  Successfully deleted: $name" -ForegroundColor Green
    } else {
        Write-Host "  Error deleting: $name" -ForegroundColor Red
        Write-Host "  Error: $result" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "Verifying deletion..." -ForegroundColor Cyan
$countQuery = 'SELECT COUNT(*) FROM "Companies";'
$count = psql -h localhost -p 5432 -U postgres -d cephasops -t -A -c $countQuery 2>&1
$countValue = $count.Trim()
Write-Host "Remaining companies in database: $countValue" -ForegroundColor Green

Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue
