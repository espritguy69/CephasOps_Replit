# Script to update all controllers to support SuperAdmin bypass
# This updates companyId checks to allow SuperAdmin to access all companies

$controllersPath = Join-Path $PSScriptRoot "..\src\CephasOps.Api\Controllers"
$files = Get-ChildItem -Path $controllersPath -Filter "*.cs" -Exclude "OrdersController.cs","AuthController.cs","AdminController.cs","DiagnosticController.cs","CompaniesController.cs"

foreach ($file in $files) {
    Write-Host "Processing $($file.Name)..." -ForegroundColor Yellow
    $content = Get-Content $file.FullName -Raw
    
    # Pattern 1: Simple companyId check
    $content = $content -replace '(var companyId = _currentUserService\.CompanyId;\s+if \(companyId == null\)\s+\{)', 
        "var companyId = _currentUserService.CompanyId;`r`n        if (companyId == null && !_currentUserService.IsSuperAdmin)`r`n        {"
    
    # Pattern 2: With userId check (companyId || userId)
    $content = $content -replace '(if \(\(companyId == null\) \|\| userId == null\))', 
        "if ((companyId == null && !_currentUserService.IsSuperAdmin) || userId == null)"
    
    # Pattern 3: currentUserCompanyId pattern
    $content = $content -replace '(var currentUserCompanyId = _currentUserService\.CompanyId;\s+if \(currentUserCompanyId == null\))', 
        "var currentUserCompanyId = _currentUserService.CompanyId;`r`n        if (currentUserCompanyId == null && !_currentUserService.IsSuperAdmin)"
    
    # Pattern 4: companyId.Value to companyId (for nullable support)
    $content = $content -replace 'companyId\.Value(?!\s*&&)', 'companyId'
    
    Set-Content $file.FullName -Value $content -NoNewline
    Write-Host "  Updated $($file.Name)" -ForegroundColor Green
}

Write-Host "`nAll controllers updated!" -ForegroundColor Cyan

