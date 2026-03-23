# Script to remove companies API imports and related code
$files = @(
    "src/pages/settings/SplittersPage.jsx",
    "src/pages/settings/PartnersPage.jsx",
    "src/pages/settings/BuildingsPage.jsx",
    "src/api/files.js",
    "src/api/billing.js"
)

foreach ($filePath in $files) {
    $fullPath = Join-Path $PSScriptRoot ".." $filePath
    if (Test-Path $fullPath) {
        Write-Host "Processing: $filePath" -ForegroundColor Cyan
        $content = Get-Content $fullPath -Raw
        
        # Remove import statements
        $content = $content -replace "import\s+\{[^}]*getCompanies[^}]*\}\s+from\s+['""]\.\.\/.*companies['""];?\r?\n?", ""
        $content = $content -replace "import\s+.*\s+from\s+['""]\.\.\/.*\/companies['""];?\r?\n?", ""
        $content = $content -replace "import\s+.*\s+from\s+['""]\.\.\/.*\/api\/companies['""];?\r?\n?", ""
        
        Set-Content -Path $fullPath -Value $content -NoNewline
        Write-Host "  -> Updated" -ForegroundColor Green
    }
}

Write-Host "`nDone!" -ForegroundColor Green

