# CephasOps Documentation Reorganization Script
# This script reorganizes all MD files in /docs according to the classification rules

$ErrorActionPreference = "Continue"
$docsRoot = "C:\Projects\CephasOps\docs"
$logFile = "C:\Projects\CephasOps\docs\_REORGANIZATION_LOG.txt"

# Initialize log
"=== CephasOps Documentation Reorganization Log ===" | Out-File $logFile
"Started: $(Get-Date)" | Out-File $logFile -Append
"" | Out-File $logFile -Append

Write-Host "🔄 Starting documentation reorganization..." -ForegroundColor Cyan
Write-Host ""

# Counter
$moved = 0
$deleted = 0
$merged = 0

# ========================================
# STEP 1: Move completion/summary/report files to 99_appendix
# ========================================
Write-Host "📦 STEP 1: Moving archive files to 99_appendix..." -ForegroundColor Yellow

$appendixFiles = @(
  "BACKEND_COMPLETION_REPORT.md",
  "COMPLETE_INTEGRATION_SUMMARY.md",
  "COMPLETION_CHECKLIST.md",
  "IMPLEMENTATION_COMPLETE_SUMMARY.md",
  "INTEGRATION_STATUS_SUMMARY.md",
  "INTEGRATION_STATUS.md",
  "INTEGRATION_FIXES.md",
  "NOTIFICATION_SYSTEM_100_PERCENT_COMPLETE.md",
  "NOTIFICATION_SYSTEM_AUDIT_REPORT.md",
  "NOTIFICATION_SYSTEM_COMPLETION_ROADMAP.md",
  "NOTIFICATION_SYSTEM_IMPLEMENTATION_SUMMARY.md",
  "SANITY_CHECK_COMPLETE.md",
  "SANITY_CHECK_FIXES_APPLIED.md",
  "SANITY_CHECK_REPORT.md",
  "SCHEDULER_IMPLEMENTATION_GAP_ANALYSIS.md",
  "EMAIL_PARSER_VIP_IMPLEMENTATION_SUMMARY.md",
  "VIP_EMAIL_NOTIFICATIONS_IMPLEMENTATION_SUMMARY.md",
  "EXEC_SUMMARY.md",
  "NEXT_STEPS.md"
)

foreach ($file in $appendixFiles) {
  $src = Join-Path $docsRoot $file
  $dst = Join-Path $docsRoot "99_appendix\$file"
  if (Test-Path $src) {
    Move-Item -Path $src -Destination $dst -Force
    $moved++
    Write-Host "  ✅ Moved: $file" -ForegroundColor Green
    "Moved: $file -> 99_appendix/" | Out-File $logFile -Append
  }
}

Write-Host ""

# ========================================
# STEP 2: Handle EMAIL_PARSER duplicates
# ========================================
Write-Host "🔀 STEP 2: Handling EMAIL_PARSER duplicates..." -ForegroundColor Yellow

# Keep 06_ai version (newer), delete 01_system version
$emailParserOld = Join-Path $docsRoot "01_system\EMAIL_PARSER.md"
$emailParserNew = Join-Path $docsRoot "06_ai\EMAIL_PARSER.md"
if ((Test-Path $emailParserOld) -and (Test-Path $emailParserNew)) {
  # Move newer version to 02_modules (it's a module-level doc about the parser module)
  $emailParserDest = Join-Path $docsRoot "02_modules\EMAIL_PARSER_MODULE.md"
  Move-Item -Path $emailParserNew -Destination $emailParserDest -Force
  Remove-Item -Path $emailParserOld -Force
  $merged++
  $deleted++
  Write-Host "  ✅ Merged EMAIL_PARSER: 06_ai (newer) -> 02_modules/EMAIL_PARSER_MODULE.md" -ForegroundColor Green
  Write-Host "  ❌ Deleted: 01_system/EMAIL_PARSER.md (older)" -ForegroundColor Red
  "Merged: EMAIL_PARSER files -> 02_modules/EMAIL_PARSER_MODULE.md" | Out-File $logFile -Append
  "Deleted: 01_system/EMAIL_PARSER.md (older duplicate)" | Out-File $logFile -Append
}

# EMAIL_PIPELINE
$emailPipelineOld = Join-Path $docsRoot "01_system\EMAIL_PIPELINE.md"
$emailPipelineNew = Join-Path $docsRoot "06_ai\EMAIL_PIPELINE.md"
if ((Test-Path $emailPipelineOld) -and (Test-Path $emailPipelineNew)) {
  # Keep in 01_system (it's system flow), delete 06_ai version
  Remove-Item -Path $emailPipelineNew -Force
  $deleted++
  Write-Host "  ✅ Kept: 01_system/EMAIL_PIPELINE.md" -ForegroundColor Green
  Write-Host "  ❌ Deleted: 06_ai/EMAIL_PIPELINE.md (duplicate)" -ForegroundColor Red
  "Deleted: 06_ai/EMAIL_PIPELINE.md (duplicate of 01_system version)" | Out-File $logFile -Append
}

# IMPLEMENTATION_NOTES
$implNotesOld = Join-Path $docsRoot "01_system\IMPLEMENTATION_NOTES_FOR_DEVELOPERS_AND_CURSOR_AI.md"
$implNotesNew = Join-Path $docsRoot "06_ai\IMPLEMENTATION_NOTES_FOR_DEVELOPERS_AND_CURSOR_AI.md"
if ((Test-Path $implNotesOld) -and (Test-Path $implNotesNew)) {
  # Keep in 06_ai (it's AI-specific), delete 01_system version
  Remove-Item -Path $implNotesOld -Force
  $deleted++
  Write-Host "  ✅ Kept: 06_ai/IMPLEMENTATION_NOTES_FOR_DEVELOPERS_AND_CURSOR_AI.md" -ForegroundColor Green
  Write-Host "  ❌ Deleted: 01_system/IMPLEMENTATION_NOTES_FOR_DEVELOPERS_AND_CURSOR_AI.md (duplicate)" -ForegroundColor Red
  "Deleted: 01_system/IMPLEMENTATION_NOTES_FOR_DEVELOPERS_AND_CURSOR_AI.md (duplicate)" | Out-File $logFile -Append
}

Write-Host ""

# ========================================
# STEP 3: Move system-level docs from root to 01_system
# ========================================
Write-Host "📁 STEP 3: Moving system docs to 01_system..." -ForegroundColor Yellow

$systemDocs = @(
  "ARCHITECTURE_BOOK.md",
  "SERVER_STATUS.md"
)

foreach ($file in $systemDocs) {
  $src = Join-Path $docsRoot $file
  $dst = Join-Path $docsRoot "01_system\$file"
  if (Test-Path $src) {
    Move-Item -Path $src -Destination $dst -Force
    $moved++
    Write-Host "  ✅ Moved: $file -> 01_system/" -ForegroundColor Green
    "Moved: $file -> 01_system/" | Out-File $logFile -Append
  }
}

Write-Host ""

# ========================================
# STEP 4: Move developer/guide docs to 06_ai
# ========================================
Write-Host "🤖 STEP 4: Moving developer/AI docs to 06_ai..." -ForegroundColor Yellow

$aiDocs = @(
  "DEV_HANDBOOK.md",
  "DEVELOPER_GUIDE.md",
  "QUICK_START.md"
)

foreach ($file in $aiDocs) {
  $src = Join-Path $docsRoot $file
  $dst = Join-Path $docsRoot "06_ai\$file"
  if (Test-Path $src) {
    Move-Item -Path $src -Destination $dst -Force
    $moved++
    Write-Host "  ✅ Moved: $file -> 06_ai/" -ForegroundColor Green
    "Moved: $file -> 06_ai/" | Out-File $logFile -Append
  }
}

Write-Host ""

# ========================================
# STEP 5: Move department docs to 02_modules
# ========================================
Write-Host "🏢 STEP 5: Moving department docs to 02_modules..." -ForegroundColor Yellow

$moduleDocs = @(
  "DEPARTMENT_README.md",
  "DEPARTMENT_FILTERING_IMPLEMENTATION.md"
)

foreach ($file in $moduleDocs) {
  $src = Join-Path $docsRoot $file
  # Rename to match module naming convention
  $newName = $file -replace "DEPARTMENT_README", "DEPARTMENT_MODULE_README" -replace "DEPARTMENT_FILTERING_IMPLEMENTATION", "DEPARTMENT_FILTERING"
  $dst = Join-Path $docsRoot "02_modules\$newName"
  if (Test-Path $src) {
    Move-Item -Path $src -Destination $dst -Force
    $moved++
    Write-Host "  ✅ Moved: $file -> 02_modules/$newName" -ForegroundColor Green
    "Moved: $file -> 02_modules/$newName" | Out-File $logFile -Append
  }
}

Write-Host ""

# ========================================
# STEP 6: Move frontend docs to 07_frontend
# ========================================
Write-Host "🎨 STEP 6: Moving frontend docs to 07_frontend..." -ForegroundColor Yellow

$frontendDocs = @(
  "FRONTEND_CONFIGURATION_VERIFICATION.md",
  "FRONTEND_IMPROVEMENTS.md",
  "FRONTEND_WORKFLOW_CHECKLIST.md"
)

foreach ($file in $frontendDocs) {
  $src = Join-Path $docsRoot $file
  $dst = Join-Path $docsRoot "07_frontend\$file"
  if (Test-Path $src) {
    Move-Item -Path $src -Destination $dst -Force
    $moved++
    Write-Host "  ✅ Moved: $file -> 07_frontend/" -ForegroundColor Green
    "Moved: $file -> 07_frontend/" | Out-File $logFile -Append
  }
}

Write-Host ""

# ========================================
# STEP 7: Move infrastructure/testing docs
# ========================================
Write-Host "🔧 STEP 7: Moving infrastructure/testing docs..." -ForegroundColor Yellow

# Testing to 08_infrastructure
$testingDocs = @(
  @{File="TESTING_SETUP.md"; Dest="08_infrastructure"},
  @{File="VERSIONING_QUICK_START.md"; Dest="08_infrastructure"}
)

foreach ($doc in $testingDocs) {
  $src = Join-Path $docsRoot $doc.File
  $dst = Join-Path $docsRoot "$($doc.Dest)\$($doc.File)"
  if (Test-Path $src) {
    Move-Item -Path $src -Destination $dst -Force
    $moved++
    Write-Host "  ✅ Moved: $($doc.File) -> $($doc.Dest)/" -ForegroundColor Green
    "Moved: $($doc.File) -> $($doc.Dest)/" | Out-File $logFile -Append
  }
}

Write-Host ""

# ========================================
# SUMMARY
# ========================================
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Gray
Write-Host "✅ REORGANIZATION COMPLETE!" -ForegroundColor Green
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Gray
Write-Host "📊 Files moved: $moved" -ForegroundColor Cyan
Write-Host "🗑️  Files deleted: $deleted" -ForegroundColor Red
Write-Host "🔀 Files merged: $merged" -ForegroundColor Yellow
Write-Host ""
Write-Host "📋 Log file created: docs\_REORGANIZATION_LOG.txt" -ForegroundColor Cyan
Write-Host ""

# Final log
"" | Out-File $logFile -Append
"=== SUMMARY ===" | Out-File $logFile -Append
"Files moved: $moved" | Out-File $logFile -Append
"Files deleted: $deleted" | Out-File $logFile -Append
"Files merged: $merged" | Out-File $logFile -Append
"Completed: $(Get-Date)" | Out-File $logFile -Append

