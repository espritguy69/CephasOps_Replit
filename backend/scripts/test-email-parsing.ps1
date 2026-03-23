# Quick test script to verify email parsing setup
# This script checks if all required services and configurations are in place

Write-Host "=== Email Parsing Setup Verification ===" -ForegroundColor Cyan
Write-Host ""

# Check if backend project exists
$backendProject = "C:\Projects\CephasOps\backend\src\CephasOps.Api\CephasOps.Api.csproj"
if (Test-Path $backendProject) {
    Write-Host "✓ Backend project found" -ForegroundColor Green
} else {
    Write-Host "✗ Backend project not found at: $backendProject" -ForegroundColor Red
    exit 1
}

# Check if frontend project exists
$frontendPackageJson = "C:\Projects\CephasOps\frontend\package.json"
if (Test-Path $frontendPackageJson) {
    Write-Host "✓ Frontend project found" -ForegroundColor Green
} else {
    Write-Host "✗ Frontend project not found at: $frontendPackageJson" -ForegroundColor Red
    exit 1
}

# Check if migration file exists
$migrationFile = "C:\Projects\CephasOps\backend\migrations\20241127_AddParserExcelFields.sql"
if (Test-Path $migrationFile) {
    Write-Host "✓ Migration file found" -ForegroundColor Green
} else {
    Write-Host "⚠ Migration file not found: $migrationFile" -ForegroundColor Yellow
    Write-Host "  Run: psql -f backend/migrations/20241127_AddParserExcelFields.sql" -ForegroundColor Yellow
}

# Check if verification script exists
$verifyScript = "C:\Projects\CephasOps\backend\scripts\verify-migration.ps1"
if (Test-Path $verifyScript) {
    Write-Host "✓ Migration verification script found" -ForegroundColor Green
} else {
    Write-Host "⚠ Verification script not found: $verifyScript" -ForegroundColor Yellow
}

# Check if parseTechnicalDetails utility exists
$parseUtil = "C:\Projects\CephasOps\frontend\src\utils\parseTechnicalDetails.ts"
if (Test-Path $parseUtil) {
    Write-Host "✓ Frontend parseTechnicalDetails utility found" -ForegroundColor Green
} else {
    Write-Host "✗ Frontend parseTechnicalDetails utility not found: $parseUtil" -ForegroundColor Red
    exit 1
}

# Check if ParseSessionReviewPage has technical details
$reviewPage = "C:\Projects\CephasOps\frontend\src\pages\parser\ParseSessionReviewPage.tsx"
if (Test-Path $reviewPage) {
    $content = Get-Content $reviewPage -Raw
    if ($content -match "parseTechnicalDetails|Technical Details|PasswordField") {
        Write-Host "✓ ParseSessionReviewPage has technical details implementation" -ForegroundColor Green
    } else {
        Write-Host "⚠ ParseSessionReviewPage may not have technical details display" -ForegroundColor Yellow
    }
} else {
    Write-Host "✗ ParseSessionReviewPage not found: $reviewPage" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "=== Key Implementation Files ===" -ForegroundColor Cyan
Write-Host ""

$files = @(
    @{Path="backend\src\CephasOps.Application\Parser\Services\TimeExcelParserService.cs"; Name="TimeExcelParserService"},
    @{Path="backend\src\CephasOps.Application\Parser\Services\EmailIngestionService.cs"; Name="EmailIngestionService"},
    @{Path="backend\src\CephasOps.Application\Orders\Services\OrderService.cs"; Name="OrderService"},
    @{Path="frontend\src\utils\parseTechnicalDetails.ts"; Name="parseTechnicalDetails utility"},
    @{Path="frontend\src\pages\parser\ParseSessionReviewPage.tsx"; Name="ParseSessionReviewPage"}
)

foreach ($file in $files) {
    $fullPath = "C:\Projects\CephasOps\$($file.Path)"
    if (Test-Path $fullPath) {
        $modified = (Get-Item $fullPath).LastWriteTime
        Write-Host "✓ $($file.Name)" -ForegroundColor Green -NoNewline
        Write-Host " (modified: $($modified.ToString('yyyy-MM-dd HH:mm')))" -ForegroundColor Gray
    } else {
        Write-Host "✗ $($file.Name) - NOT FOUND" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "=== Next Steps ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "1. Verify database migration is applied:" -ForegroundColor Yellow
Write-Host "   .\backend\scripts\verify-migration.ps1 -ConnectionString 'Host=...;Database=...;Username=...;Password=...'" -ForegroundColor White
Write-Host ""
Write-Host "2. Ensure email account is configured and active" -ForegroundColor Yellow
Write-Host ""
Write-Host "3. Start services:" -ForegroundColor Yellow
Write-Host "   Backend: cd backend && .\start.ps1" -ForegroundColor White
Write-Host "   Frontend: cd frontend && npm run dev" -ForegroundColor White
Write-Host ""
Write-Host "4. Send test email with Excel attachment" -ForegroundColor Yellow
Write-Host ""
Write-Host "5. Trigger email poll via API or UI" -ForegroundColor Yellow
Write-Host ""
Write-Host "6. Check Parse Sessions page for results" -ForegroundColor Yellow
Write-Host ""
Write-Host "=== Ready for Testing! ===" -ForegroundColor Green

