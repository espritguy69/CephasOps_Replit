# Comprehensive script to remove all companyId checks from controllers
$controllersPath = Join-Path $PSScriptRoot "..\src\CephasOps.Api\Controllers"
$files = Get-ChildItem -Path $controllersPath -Filter "*.cs" -Exclude "AuthController.cs","CompaniesController.cs","DiagnosticController.cs","AdminController.cs"

foreach ($file in $files) {
    Write-Host "Processing $($file.Name)..." -ForegroundColor Yellow
    $content = Get-Content $file.FullName -Raw
    
    # Replace all companyId checks with simple null assignment
    # Pattern 1: Multi-line with SuperAdmin check
    $content = $content -replace '(?s)// SuperAdmin can access all companies[^\n]*\n\s*var companyId = _currentUserService\.CompanyId;\s*if \(companyId == null && !_currentUserService\.IsSuperAdmin\)\s*\{\s*return Unauthorized\("Company context required"\);\s*\}', '// Company feature removed`r`n        var companyId = (Guid?)null;'
    
    # Pattern 2: Simple companyId check
    $content = $content -replace '(?s)var companyId = _currentUserService\.CompanyId;\s*if \(companyId == null\)\s*\{\s*return Unauthorized\("Company context required"\);\s*\}', '// Company feature removed`r`n        var companyId = (Guid?)null;'
    
    # Pattern 3: With userId check - keep userId check, remove companyId
    $content = $content -replace '(?s)// SuperAdmin can access all companies[^\n]*\n\s*var companyId = _currentUserService\.CompanyId;\s*var userId = _currentUserService\.UserId;\s*if \(\(companyId == null && !_currentUserService\.IsSuperAdmin\) \|\| userId == null\)\s*\{\s*return Unauthorized\("Company and user context required"\);\s*\}', 
        '// Company feature removed`r`n        var companyId = (Guid?)null;`r`n        var userId = _currentUserService.UserId;`r`n        if (userId == null)`r`n        {`r`n            return Unauthorized("User context required");`r`n        }'
    
    # Pattern 4: currentUserCompanyId
    $content = $content -replace '(?s)var currentUserCompanyId = _currentUserService\.CompanyId;\s*if \(currentUserCompanyId == null && !_currentUserService\.IsSuperAdmin\)\s*\{\s*return Unauthorized\("Company context required"\);\s*\}', '// Company feature removed`r`n        var currentUserCompanyId = (Guid?)null;'
    
    # Pattern 5: Replace companyId.Value with companyId (since it's nullable now)
    $content = $content -replace 'companyId\.Value(?!\s*&&)', 'companyId'
    $content = $content -replace 'currentUserCompanyId\.Value(?!\s*&&)', 'currentUserCompanyId'
    
    # Pattern 6: Fix any remaining _currentUserService.CompanyId references
    $content = $content -replace 'var companyId = _currentUserService\.CompanyId;', 'var companyId = (Guid?)null; // Company feature removed'
    $content = $content -replace 'var currentUserCompanyId = _currentUserService\.CompanyId;', 'var currentUserCompanyId = (Guid?)null; // Company feature removed'
    
    Set-Content $file.FullName -Value $content -NoNewline
    Write-Host "  Updated $($file.Name)" -ForegroundColor Green
}

Write-Host "`nAll controllers updated!" -ForegroundColor Cyan

