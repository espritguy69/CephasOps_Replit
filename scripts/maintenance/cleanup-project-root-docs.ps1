# Move all loose MD files from project root to /docs
# Categorize them properly based on content

$projectRoot = "C:\Projects\CephasOps"
$docsRoot = "C:\Projects\CephasOps\docs"

Write-Host "🧹 Cleaning up project root MD files..." -ForegroundColor Cyan
Write-Host ""

$moved = 0
$archived = 0

# ========================================
# CATEGORY 1: Implementation/Completion Reports → 99_appendix
# ========================================
$appendixFiles = @(
  "AGENT_MODE_IMPLEMENTATION_COMPLETE.md",
  "ALL_ISSUES_FIXED.md",
  "AUDIT_REPORT.md",
  "COMPLETE_ORDER_WORKFLOW_LIFECYCLE.md",
  "COMPLETE_PLANS_RECALL.md",
  "DEPARTMENT_FILTERING_COMPLETE.md",
  "DEPARTMENT_FILTERING_FINAL_STATUS.md",
  "DEPARTMENT_FILTERING_SUMMARY.md",
  "DOCS_REORGANIZATION_COMPLETE.md",
  "E2E_TEST_RESULTS.md",
  "EMAIL_PARSER_AUDIT.md",
  "IMPLEMENTATION_COMPLETE_SUMMARY.md",
  "IMPLEMENTATION_READY_SUMMARY.md",
  "IMPLEMENTATION_STATUS.md",
  "LIVE_EMAIL_TEST_CHECKLIST.md",
  "NEXT_STEPS.md",
  "PARSER_FIXES_SUMMARY.md",
  "QUICK_TEST_CHECKLIST.md",
  "READY_FOR_LIVE_TEST.md",
  "SANITY_CHECK_COMPLETE.md",
  "SANITY_CHECK_FIXES_APPLIED.md",
  "SANITY_CHECK_REPORT.md",
  "SCHEDULER_IMPLEMENTATION_GAP_ANALYSIS.md",
  "TODO1_EMAIL_TEMPLATES_COMPLETE.md",
  "TODO2_WORKFLOW_LIFECYCLE_COMPLETE.md",
  "URGENT_RESTART_REQUIRED.md"
)

Write-Host "📦 Moving completion/audit reports to 99_appendix..." -ForegroundColor Yellow
foreach ($file in $appendixFiles) {
  $src = Join-Path $projectRoot $file
  $dst = Join-Path $docsRoot "99_appendix\$file"
  if (Test-Path $src) {
    Move-Item -Path $src -Destination $dst -Force
    $archived++
    Write-Host "  ✅ $file" -ForegroundColor Green
  }
}

# ========================================
# CATEGORY 2: Testing/Guide Docs → 06_ai
# ========================================
$aiDocs = @(
  "EMAIL_PARSER_E2E_TEST_GUIDE.md",
  "QUICK_START_EMAIL_TEST.md",
  "TESTING_GUIDE_EMAIL_TEMPLATES_AND_AGENT.md",
  "TESTING_INVOICE_SUBMISSION_API.md"
)

Write-Host ""
Write-Host "🤖 Moving testing guides to 06_ai..." -ForegroundColor Yellow
foreach ($file in $aiDocs) {
  $src = Join-Path $projectRoot $file
  $dst = Join-Path $docsRoot "06_ai\$file"
  if (Test-Path $src) {
    Move-Item -Path $src -Destination $dst -Force
    $moved++
    Write-Host "  ✅ $file" -ForegroundColor Green
  }
}

# ========================================
# CATEGORY 3: Feature Implementation Docs → 02_modules
# ========================================
$moduleDocs = @(
  @{File="BUILDING_DEDUPLICATION_IMPLEMENTATION.md"; Dest="02_modules"},
  @{File="BUILDING_MATCHING_SYSTEM_COMPLETE.md"; Dest="02_modules"},
  @{File="CARBONE_CONFIGURATION.md"; Dest="02_modules"},
  @{File="QUICK_BUILDING_MODAL_IMPLEMENTATION.md"; Dest="02_modules"},
  @{File="DEPARTMENT_README.md"; Dest="02_modules"}
)

Write-Host ""
Write-Host "📦 Moving module implementation docs to 02_modules..." -ForegroundColor Yellow
foreach ($doc in $moduleDocs) {
  $src = Join-Path $projectRoot $doc.File
  $dst = Join-Path $docsRoot "$($doc.Dest)\$($doc.File)"
  if (Test-Path $src) {
    Move-Item -Path $src -Destination $dst -Force
    $moved++
    Write-Host "  ✅ $($doc.File)" -ForegroundColor Green
  }
}

# ========================================
# CATEGORY 4: Infrastructure/Setup → 08_infrastructure
# ========================================
$infraDocs = @(
  "FAST_RESTART_GUIDE.md",
  "FAST_RESTART_IMPLEMENTATION_SUMMARY.md",
  "HOW_TO_USE_FAST_RESTART.md",
  "SERVER_STATUS.md",
  "TESTING_SETUP.md",
  "VERSIONING_QUICK_START.md"
)

Write-Host ""
Write-Host "🔧 Moving infrastructure docs to 08_infrastructure..." -ForegroundColor Yellow
foreach ($file in $infraDocs) {
  $src = Join-Path $projectRoot $file
  $dst = Join-Path $docsRoot "08_infrastructure\$file"
  if (Test-Path $src) {
    Move-Item -Path $src -Destination $dst -Force
    $moved++
    Write-Host "  ✅ $file" -ForegroundColor Green
  }
}

# ========================================
# CATEGORY 5: Developer Guides → 06_ai
# ========================================
$devDocs = @(
  "DEVELOPER_GUIDE.md",
  "QUICK_START.md",
  "QUICK_COMMANDS.md"
)

Write-Host ""
Write-Host "📚 Moving developer guides to 06_ai..." -ForegroundColor Yellow
foreach ($file in $devDocs) {
  $src = Join-Path $projectRoot $file
  $dst = Join-Path $docsRoot "06_ai\$file"
  if (Test-Path $src) {
    Move-Item -Path $src -Destination $dst -Force
    $moved++
    Write-Host "  ✅ $file" -ForegroundColor Green
  }
}

# ========================================
# SUMMARY
# ========================================
Write-Host ""
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Gray
Write-Host "✅ PROJECT ROOT CLEANUP COMPLETE!" -ForegroundColor Green
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Gray
Write-Host "📦 Files archived: $archived" -ForegroundColor Cyan
Write-Host "📁 Files moved: $moved" -ForegroundColor Cyan
Write-Host "📊 Total cleaned: $($archived + $moved)" -ForegroundColor Yellow
Write-Host ""

# Check remaining MD files at root
$remaining = Get-ChildItem -Path $projectRoot -Filter "*.md" -File | Where-Object { $_.Name -ne "README.md" }
if ($remaining.Count -gt 0) {
  Write-Host "⚠️  Remaining MD files at root:" -ForegroundColor Yellow
  $remaining | ForEach-Object { Write-Host "  - $($_.Name)" -ForegroundColor Gray }
} else {
  Write-Host "✅ Project root is clean (only README.md remains)!" -ForegroundColor Green
}

