# Comprehensive Documentation Refactoring Script
# Creates modular subfolder structure with focused, small files

$docsRoot = "C:\Projects\CephasOps\docs"
$modulesRoot = Join-Path $docsRoot "02_modules"

Write-Host "🔄 Starting comprehensive documentation refactoring..." -ForegroundColor Cyan
Write-Host ""

# ========================================
# STEP 1: Create subfolder structure in 02_modules
# ========================================
Write-Host "📁 STEP 1: Creating modular subfolder structure..." -ForegroundColor Yellow

$subfolders = @(
    "department",
    "inventory",
    "email_parser",
    "document_generation",
    "billing",
    "global_settings",
    "gpon",
    "materials",
    "orders",
    "partners",
    "rate_engine",
    "scheduler",
    "payroll",
    "pnl",
    "notifications",
    "rbac",
    "tasks",
    "splitters",
    "workflow",
    "kpi",
    "background_jobs",
    "service_installer"
)

foreach ($folder in $subfolders) {
    $path = Join-Path $modulesRoot $folder
    if (-not (Test-Path $path)) {
        New-Item -Path $path -ItemType Directory -Force | Out-Null
        Write-Host "  ✅ Created: $folder/" -ForegroundColor Green
    }
}

Write-Host ""

# ========================================
# STEP 2: Merge duplicate DEPARTMENT files
# ========================================
Write-Host "🔀 STEP 2: Merging DEPARTMENT duplicates..." -ForegroundColor Yellow

# The ORIGINAL is DEPARTMENT_MODULE.md - keep this name
# Merge content from DEPARTMENT_MODULE_README.md, DEPARTMENT_README.md, DEPARTMENT_FILTERING.md

Write-Host "  ℹ️  Original: DEPARTMENT_MODULE.md" -ForegroundColor Cyan
Write-Host "  ℹ️  Will merge from:" -ForegroundColor Cyan
Write-Host "     - DEPARTMENT_MODULE_README.md" -ForegroundColor Gray
Write-Host "     - DEPARTMENT_README.md" -ForegroundColor Gray
Write-Host "     - DEPARTMENT_FILTERING.md" -ForegroundColor Gray
Write-Host "  ⚠️  NOTE: Manual merge required (content review needed)" -ForegroundColor Yellow

Write-Host ""

# ========================================
# STEP 3: Merge EMAIL_PARSER files
# ========================================
Write-Host "🔀 STEP 3: Merging EMAIL_PARSER duplicates..." -ForegroundColor Yellow

Write-Host "  ℹ️  Original: EMAIL_PARSER_SETUP.md (keep this)" -ForegroundColor Cyan
Write-Host "  ℹ️  Will merge from:" -ForegroundColor Cyan
Write-Host "     - EMAIL_PARSER_MODULE.md (has full spec)" -ForegroundColor Gray
Write-Host "  ⚠️  NOTE: Manual merge required (content review needed)" -ForegroundColor Yellow

Write-Host ""

# ========================================
# STEP 4: Move and organize module files into subfolders
# ========================================
Write-Host "📦 STEP 4: Organizing module files into subfolders..." -ForegroundColor Yellow

# Map: filename pattern -> subfolder
$fileMapping = @(
    @{Pattern="DEPARTMENT*"; Folder="department"},
    @{Pattern="INVENTORY*"; Folder="inventory"},
    @{Pattern="RMA*"; Folder="inventory"},
    @{Pattern="EMAIL_PARSER*"; Folder="email_parser"},
    @{Pattern="DOCUMENT_GENERATION*"; Folder="document_generation"},
    @{Pattern="DOCUMENT_TEMPLATES*"; Folder="document_generation"},
    @{Pattern="BILLING*"; Folder="billing"},
    @{Pattern="GLOBAL_SETTINGS*"; Folder="global_settings"},
    @{Pattern="GPON*"; Folder="gpon"},
    @{Pattern="MATERIAL*"; Folder="materials"},
    @{Pattern="ORDERS*"; Folder="orders"},
    @{Pattern="PARTNER*"; Folder="partners"},
    @{Pattern="RATE_ENGINE*"; Folder="rate_engine"},
    @{Pattern="SCHEDULER*"; Folder="scheduler"},
    @{Pattern="PAYROLL*"; Folder="payroll"},
    @{Pattern="PNL*"; Folder="pnl"},
    @{Pattern="notification*"; Folder="notifications"},
    @{Pattern="RBAC*"; Folder="rbac"},
    @{Pattern="tasks*"; Folder="tasks"},
    @{Pattern="SPLITTER*"; Folder="splitters"},
    @{Pattern="WORKFLOW*"; Folder="workflow"},
    @{Pattern="KPI*"; Folder="kpi"},
    @{Pattern="BACKGROUND_JOBS*"; Folder="background_jobs"},
    @{Pattern="SERVICE_INSTALLER*"; Folder="service_installer"},
    @{Pattern="BUILDING*"; Folder="orders"},
    @{Pattern="CARBONE*"; Folder="document_generation"},
    @{Pattern="COMPANIES*"; Folder="global_settings"},
    @{Pattern="COMPANY*"; Folder="global_settings"},
    @{Pattern="SETTINGS*"; Folder="global_settings"},
    @{Pattern="LOGGING*"; Folder="global_settings"},
    @{Pattern="MULTI_COMPANY*"; Folder="global_settings"}
)

foreach ($mapping in $fileMapping) {
    $pattern = $mapping.Pattern
    $targetFolder = Join-Path $modulesRoot $mapping.Folder
    
    $files = Get-ChildItem -Path $modulesRoot -Filter "$pattern" -File
    foreach ($file in $files) {
        $dest = Join-Path $targetFolder $file.Name
        if ($file.FullName -ne $dest) {
            Move-Item -Path $file.FullName -Destination $dest -Force
            Write-Host "  ✅ Moved: $($file.Name) → $($mapping.Folder)/" -ForegroundColor Green
        }
    }
}

Write-Host ""
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Gray
Write-Host "✅ REFACTORING PHASE 1 COMPLETE!" -ForegroundColor Green
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Gray
Write-Host ""
Write-Host "⚠️  NEXT: Manual content merges required for:" -ForegroundColor Yellow
Write-Host "   - DEPARTMENT files (3 → 1)" -ForegroundColor Gray
Write-Host "   - EMAIL_PARSER files (2 → 1)" -ForegroundColor Gray
Write-Host ""

