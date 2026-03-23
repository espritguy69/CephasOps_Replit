# Script to remove CompanyContext references from frontend files
$files = Get-ChildItem -Path "src" -Recurse -Include "*.jsx","*.js" -File | Where-Object {
    (Get-Content $_.FullName -Raw) -match "CompanyContext|useCompany"
}

foreach ($file in $files) {
    Write-Host "Processing: $($file.FullName)" -ForegroundColor Cyan
    $content = Get-Content $file.FullName -Raw
    
    # Remove import statement
    $content = $content -replace "import\s+\{[^}]*useCompany[^}]*\}\s+from\s+['""]\.\.\/.*CompanyContext['""];?\r?\n?", ""
    $content = $content -replace "import\s+useCompany\s+from\s+['""]\.\.\/.*CompanyContext['""];?\r?\n?", ""
    $content = $content -replace "import\s+\{[^}]*\}\s+from\s+['""]\.\.\/.*CompanyContext['""];?\r?\n?", ""
    
    # Remove useCompany hook calls
    $content = $content -replace "const\s+\{[^}]*currentCompanyId[^}]*\}\s*=\s*useCompany\(\);?\r?\n?", ""
    $content = $content -replace "const\s+\{[^}]*companies[^}]*\}\s*=\s*useCompany\(\);?\r?\n?", ""
    $content = $content -replace "const\s+\{[^}]*switchCompany[^}]*\}\s*=\s*useCompany\(\);?\r?\n?", ""
    $content = $content -replace "const\s+\{[^}]*\}\s*=\s*useCompany\(\);?\r?\n?", ""
    
    # Remove references to currentCompanyId in conditionals
    $content = $content -replace "if\s*\(\s*!currentCompanyId\s*\)\s*return;?\r?\n?\s*", ""
    $content = $content -replace "if\s*\(\s*currentCompanyId\s*\)\s*\{", "if (true) {"
    
    # Remove currentCompanyId from function parameters or usage
    $content = $content -replace "currentCompanyId\s*\|\|\s*", ""
    $content = $content -replace "\|\|\s*currentCompanyId", ""
    
    Set-Content -Path $file.FullName -Value $content -NoNewline
    Write-Host "  -> Updated" -ForegroundColor Green
}

Write-Host "`nDone! Processed $($files.Count) files." -ForegroundColor Green

