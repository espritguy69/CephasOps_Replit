# Script to fix all settings controllers to remove company context requirement

$controllers = @(
    "BuildingsController.cs",
    "BuildingTypesController.cs",
    "OrderTypesController.cs",
    "InstallationTypesController.cs",
    "SplitterTypesController.cs",
    "SplittersController.cs",
    "ServiceInstallersController.cs"
)

$controllersPath = "backend\src\CephasOps.Api\Controllers"

foreach ($controller in $controllers) {
    $filePath = Join-Path $controllersPath $controller
    if (Test-Path $filePath) {
        Write-Host "Processing $controller..." -ForegroundColor Cyan
        
        $content = Get-Content $filePath -Raw
        
        # Replace companyId checks and Unauthorized returns
        $content = $content -replace '(?s)var companyId = _currentUserService\.CompanyId;\s+if \(companyId == null\)\s+\{\s+return Unauthorized\("Company context required"\);\s+\}', '// Company feature removed - companyId can be null`n        var companyId = _currentUserService.CompanyId;'
        
        # Replace companyId.Value with companyId (nullable)
        $content = $content -replace 'companyId\.Value', 'companyId'
        
        # Replace dto.CompanyId ?? _currentUserService.CompanyId with just dto.CompanyId ?? _currentUserService.CompanyId (no check)
        $content = $content -replace '(?s)// Use companyId from DTO if provided, otherwise use user''s current company\s+var companyId = dto\.CompanyId \?\? _currentUserService\.CompanyId;\s+if \(companyId == null\)\s+\{\s+return Unauthorized\("Company context required"\);\s+\}\s+// Validate that user has access to the specified company.*?// For now, allow if user has access to multiple companies\s+\}', '// Company feature removed - companyId can be null`n        var companyId = dto.CompanyId ?? _currentUserService.CompanyId;'
        
        Set-Content -Path $filePath -Value $content -NoNewline
        Write-Host "  ✓ Fixed $controller" -ForegroundColor Green
    }
}

Write-Host "`nAll controllers fixed!" -ForegroundColor Green

