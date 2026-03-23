# Syncfusion Installation Verification Script
# Verifies all Syncfusion dependencies are installed correctly

Write-Host "🔍 SYNCFUSION INSTALLATION VERIFICATION" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$allGood = $true

# Check Frontend Packages
Write-Host "📦 Checking Frontend Packages..." -ForegroundColor Yellow
Push-Location frontend

try {
    $packages = npm list --depth=0 2>&1 | Select-String "syncfusion"
    
    $expectedPackages = @(
        "@syncfusion/ej2-react-calendars",
        "@syncfusion/ej2-react-charts",
        "@syncfusion/ej2-react-diagrams",
        "@syncfusion/ej2-react-dropdowns",
        "@syncfusion/ej2-react-grids",
        "@syncfusion/ej2-react-inputs",
        "@syncfusion/ej2-react-kanban",
        "@syncfusion/ej2-react-pdfviewer",
        "@syncfusion/ej2-react-richtexteditor",
        "@syncfusion/ej2-react-schedule",
        "@syncfusion/ej2-react-spreadsheet",
        "@syncfusion/ej2-react-treegrid"
    )
    
    $installedCount = 0
    foreach ($pkg in $expectedPackages) {
        if ($packages -match $pkg) {
            Write-Host "  ✅ $pkg" -ForegroundColor Green
            $installedCount++
        } else {
            Write-Host "  ❌ $pkg - MISSING" -ForegroundColor Red
            $allGood = $false
        }
    }
    
    Write-Host ""
    Write-Host "  Frontend: $installedCount / $($expectedPackages.Count) packages installed" -ForegroundColor $(if ($installedCount -eq $expectedPackages.Count) { "Green" } else { "Red" })
} catch {
    Write-Host "  ❌ Error checking frontend packages: $_" -ForegroundColor Red
    $allGood = $false
}

Pop-Location
Write-Host ""

# Check Backend Packages
Write-Host "📦 Checking Backend Packages..." -ForegroundColor Yellow
Push-Location backend/src/CephasOps.Infrastructure

try {
    $packages = dotnet list package 2>&1 | Select-String "Syncfusion"
    
    $expectedPackages = @(
        "Syncfusion.XlsIO.Net.Core",
        "Syncfusion.Pdf.Net.Core",
        "Syncfusion.DocIO.Net.Core",
        "Syncfusion.DocIORenderer.Net.Core"
    )
    
    $installedCount = 0
    foreach ($pkg in $expectedPackages) {
        if ($packages -match $pkg) {
            Write-Host "  ✅ $pkg" -ForegroundColor Green
            $installedCount++
        } else {
            Write-Host "  ❌ $pkg - MISSING" -ForegroundColor Red
            $allGood = $false
        }
    }
    
    Write-Host ""
    Write-Host "  Infrastructure: $installedCount / $($expectedPackages.Count) packages installed" -ForegroundColor $(if ($installedCount -eq $expectedPackages.Count) { "Green" } else { "Red" })
} catch {
    Write-Host "  ❌ Error checking backend packages: $_" -ForegroundColor Red
    $allGood = $false
}

Pop-Location
Write-Host ""

# Check Backend API Licensing Package
Write-Host "📦 Checking Backend API Licensing..." -ForegroundColor Yellow
Push-Location backend/src/CephasOps.Api

try {
    $packages = dotnet list package 2>&1 | Select-String "Syncfusion.Licensing"
    
    if ($packages) {
        Write-Host "  ✅ Syncfusion.Licensing" -ForegroundColor Green
    } else {
        Write-Host "  ❌ Syncfusion.Licensing - MISSING" -ForegroundColor Red
        $allGood = $false
    }
} catch {
    Write-Host "  ❌ Error checking API licensing package: $_" -ForegroundColor Red
    $allGood = $false
}

Pop-Location
Write-Host ""

# Check Frontend License Configuration
Write-Host "🔑 Checking Frontend License Configuration..." -ForegroundColor Yellow

if (Test-Path "frontend/.env.example") {
    $envExample = Get-Content "frontend/.env.example" -Raw
    if ($envExample -match "VITE_SYNCFUSION_LICENSE_KEY") {
        Write-Host "  ✅ .env.example has license key template" -ForegroundColor Green
    } else {
        Write-Host "  ❌ .env.example missing license key" -ForegroundColor Red
        $allGood = $false
    }
} else {
    Write-Host "  ⚠️  .env.example not found (will create on sync)" -ForegroundColor Yellow
}

if (Test-Path "frontend/src/utils/syncfusion.ts") {
    Write-Host "  ✅ License registration utility exists" -ForegroundColor Green
} else {
    Write-Host "  ❌ License registration utility missing" -ForegroundColor Red
    $allGood = $false
}

Write-Host ""

# Check Backend License Configuration
Write-Host "🔑 Checking Backend License Configuration..." -ForegroundColor Yellow
Push-Location backend/src/CephasOps.Api

try {
    $secrets = dotnet user-secrets list 2>&1 | Select-String "SYNCFUSION_LICENSE_KEY"
    
    if ($secrets) {
        Write-Host "  ✅ Backend license key configured in user-secrets" -ForegroundColor Green
    } else {
        Write-Host "  ⚠️  Backend license key not set in user-secrets" -ForegroundColor Yellow
        Write-Host "     Run: dotnet user-secrets set `"SYNCFUSION_LICENSE_KEY`" `"<key>`"" -ForegroundColor Gray
    }
} catch {
    Write-Host "  ⚠️  Could not check user-secrets: $_" -ForegroundColor Yellow
}

Pop-Location
Write-Host ""

# Check Critical Files
Write-Host "📄 Checking Critical Files..." -ForegroundColor Yellow

$criticalFiles = @(
    "backend/src/CephasOps.Domain/Common/BaseEntity.cs",
    "frontend/src/utils/syncfusion.ts",
    "frontend/src/main.tsx",
    "backend/src/CephasOps.Api/Program.cs"
)

foreach ($file in $criticalFiles) {
    if (Test-Path $file) {
        Write-Host "  ✅ $file" -ForegroundColor Green
    } else {
        Write-Host "  ❌ $file - MISSING" -ForegroundColor Red
        $allGood = $false
    }
}

Write-Host ""

# Final Summary
Write-Host "========================================" -ForegroundColor Cyan
if ($allGood) {
    Write-Host "✅ ALL CHECKS PASSED!" -ForegroundColor Green
    Write-Host ""
    Write-Host "🚀 Syncfusion is ready to use!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Cyan
    Write-Host "  1. Start backend: cd backend/src/CephasOps.Api && dotnet watch run" -ForegroundColor Gray
    Write-Host "  2. Start frontend: cd frontend && npm run dev" -ForegroundColor Gray
    Write-Host "  3. Check browser console for license confirmation" -ForegroundColor Gray
} else {
    Write-Host "⚠️  SOME CHECKS FAILED" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Review the errors above and:" -ForegroundColor Yellow
    Write-Host "  1. Run 'npm install' in frontend directory" -ForegroundColor Gray
    Write-Host "  2. Run 'dotnet restore' in backend directories" -ForegroundColor Gray
    Write-Host "  3. Configure license keys as needed" -ForegroundColor Gray
}
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

