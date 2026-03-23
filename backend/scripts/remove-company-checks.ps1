# Script to remove companyId checks from all controllers
# Since companyId is now always null, we remove all checks

$controllersPath = Join-Path $PSScriptRoot "..\src\CephasOps.Api\Controllers"
$files = Get-ChildItem -Path $controllersPath -Filter "*.cs" -Exclude "AuthController.cs","CompaniesController.cs","DiagnosticController.cs","AdminController.cs"

foreach ($file in $files) {
    Write-Host "Processing $($file.Name)..." -ForegroundColor Yellow
    $content = Get-Content $file.FullName -Raw
    
    # Pattern 1: Remove companyId checks with SuperAdmin bypass
    $content = $content -replace '(?s)// SuperAdmin can access all companies, regular users need company context\s+var companyId = _currentUserService\.CompanyId;\s+if \(companyId == null && !_currentUserService\.IsSuperAdmin\)\s+\{\s+return Unauthorized\("Company context required"\);\s+\}', ''
    
    # Pattern 2: Remove simple companyId checks
    $content = $content -replace '(?s)var companyId = _currentUserService\.CompanyId;\s+if \(companyId == null\)\s+\{\s+return Unauthorized\("Company context required"\);\s+\}', ''
    
    # Pattern 3: Remove companyId checks with userId
    $content = $content -replace '(?s)// SuperAdmin can access all companies, regular users need company context\s+var companyId = _currentUserService\.CompanyId;\s+var userId = _currentUserService\.UserId;\s+if \(\(companyId == null && !_currentUserService\.IsSuperAdmin\) \|\| userId == null\)\s+\{\s+return Unauthorized\("Company and user context required"\);\s+\}', 
        'var userId = _currentUserService.UserId;`r`n        if (userId == null)`r`n        {`r`n            return Unauthorized("User context required");`r`n        }'
    
    # Pattern 4: Remove currentUserCompanyId checks
    $content = $content -replace '(?s)// SuperAdmin can access all companies, regular users need company context\s+var currentUserCompanyId = _currentUserService\.CompanyId;\s+if \(currentUserCompanyId == null && !_currentUserService\.IsSuperAdmin\)\s+\{\s+return Unauthorized\("Company context required"\);\s+\}', ''
    
    # Pattern 5: Replace companyId.Value with companyId (since it's now always null, pass null)
    $content = $content -replace 'companyId\.Value', 'companyId'
    
    # Pattern 6: Replace var companyId = _currentUserService.CompanyId; with var companyId = (Guid?)null;
    $content = $content -replace 'var companyId = _currentUserService\.CompanyId;', 'var companyId = (Guid?)null; // Company feature removed'
    
    Set-Content $file.FullName -Value $content -NoNewline
    Write-Host "  Updated $($file.Name)" -ForegroundColor Green
}

Write-Host "`nAll controllers updated!" -ForegroundColor Cyan

