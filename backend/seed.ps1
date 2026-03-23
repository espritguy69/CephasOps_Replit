# ============================================
# CephasOps Backend - Seed Database
# ============================================
# Run this script from the backend folder
# Note: Seeding runs automatically when EF Core migrations are applied

$ErrorActionPreference = "Stop"
$scriptRoot = $PSScriptRoot

Write-Host "🌱 Database Seeding Information" -ForegroundColor Cyan
Write-Host "Location: $scriptRoot" -ForegroundColor Gray

Write-Host "`n💡 Note: Database seeding runs automatically when EF Core migrations are applied." -ForegroundColor Yellow
Write-Host "PostgreSQL is now the single source of truth for all seed data." -ForegroundColor Cyan

Write-Host "`nTo seed the database, apply migrations:" -ForegroundColor Cyan
Write-Host "  cd src\CephasOps.Infrastructure" -ForegroundColor Green
Write-Host "  dotnet ef database update" -ForegroundColor Green

Write-Host "`nOr start the backend (migrations run automatically if configured):" -ForegroundColor Yellow
Write-Host "  .\start.ps1" -ForegroundColor Green

Write-Host "`nDefault seeded data (via PostgreSQL migration):" -ForegroundColor Cyan
Write-Host "  - Default Company: Cephas" -ForegroundColor Gray
Write-Host "  - Roles (5: SuperAdmin, Director, HeadOfDepartment, Supervisor, FinanceManager)" -ForegroundColor Gray
Write-Host "  - Default Admin User: simon@cephas.com.my" -ForegroundColor Gray
Write-Host "  - Finance HOD User: finance@cephas.com.my" -ForegroundColor Gray
Write-Host "  - GPON Department" -ForegroundColor Gray
Write-Host "  - Default Order Types (5: Activation, Modification Indoor/Outdoor, Assurance, VAS)" -ForegroundColor Gray
Write-Host "  - Default Order Categories (4: FTTH, FTTO, FTTR, FTTC)" -ForegroundColor Gray
Write-Host "  - Default Building Types (19 types: Condo, Apartment, Terrace, Office, etc.)" -ForegroundColor Gray
Write-Host "  - Default Splitter Types (3: 1:8, 1:12, 1:32)" -ForegroundColor Gray
Write-Host "  - Skills (33: Fiber, Network Equipment, Installation Methods, Safety, Customer Service)" -ForegroundColor Gray
Write-Host "  - Guard Conditions & Side Effects (workflow configuration)" -ForegroundColor Gray
Write-Host "  - Movement Types & Location Types (inventory configuration)" -ForegroundColor Gray
Write-Host "  - Parser Templates (14 templates for TIME orders)" -ForegroundColor Gray
Write-Host "  - Global Settings (~30+ settings: SMS/WhatsApp, E-Invoice)" -ForegroundColor Gray
Write-Host "  - Material Categories (8 default categories, if none exist)" -ForegroundColor Gray

Write-Host "`n⚠️  Materials are NOT seeded automatically!" -ForegroundColor Yellow
Write-Host "   Import materials using: .\scripts\import-materials.ps1" -ForegroundColor Yellow
Write-Host "   See: .\scripts\MATERIALS_IMPORT_GUIDE.md for details" -ForegroundColor Gray

Write-Host "`n📝 Seeding is idempotent - safe to run multiple times." -ForegroundColor Cyan
Write-Host "   Existing data won't be overwritten (uses WHERE NOT EXISTS and ON CONFLICT DO NOTHING)." -ForegroundColor Gray

Write-Host "`n📄 Migration File:" -ForegroundColor Cyan
Write-Host "   src\CephasOps.Infrastructure\Persistence\Migrations\20250106_SeedAllReferenceData.cs" -ForegroundColor Gray
Write-Host "   (Contains embedded SQL: 20250106_SeedAllReferenceData.sql)" -ForegroundColor Gray

Write-Host "`n⚠️  DatabaseSeeder (C#) is DISABLED - All seeding now in PostgreSQL migrations." -ForegroundColor Yellow

