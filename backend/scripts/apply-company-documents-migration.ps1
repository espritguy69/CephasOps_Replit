# Apply CompanyDocuments table migration manually
# This script creates the CompanyDocuments table and marks the migration as applied

$env:PGPASSWORD = 'J@saw007'
$sql = @"
-- Create CompanyDocuments table
CREATE TABLE IF NOT EXISTS "CompanyDocuments" (
    "Id" uuid NOT NULL,
    "Category" character varying(50) NOT NULL,
    "Title" character varying(500) NOT NULL,
    "DocumentType" character varying(200) NOT NULL,
    "FileId" uuid NOT NULL,
    "EffectiveDate" timestamp with time zone NULL,
    "ExpiryDate" timestamp with time zone NULL,
    "IsCritical" boolean NOT NULL DEFAULT false,
    "Notes" character varying(2000) NULL,
    "RelatedModule" character varying(100) NULL,
    "RelatedEntityId" uuid NULL,
    "CreatedByUserId" uuid NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NULL,
    CONSTRAINT "PK_CompanyDocuments" PRIMARY KEY ("Id")
);

-- Create indexes
CREATE INDEX IF NOT EXISTS "IX_CompanyDocuments_CompanyId_Category" 
    ON "CompanyDocuments" ("CompanyId", "Category");

CREATE INDEX IF NOT EXISTS "IX_CompanyDocuments_CompanyId_ExpiryDate" 
    ON "CompanyDocuments" ("CompanyId", "ExpiryDate");

CREATE INDEX IF NOT EXISTS "IX_CompanyDocuments_CompanyId_IsCritical" 
    ON "CompanyDocuments" ("CompanyId", "IsCritical");

CREATE INDEX IF NOT EXISTS "IX_CompanyDocuments_FileId" 
    ON "CompanyDocuments" ("FileId");

-- Mark migration as applied
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion") 
VALUES ('20251123185309_AddCompanyDocuments', '10.0.0') 
ON CONFLICT ("MigrationId") DO NOTHING;
"@

Write-Host "Applying CompanyDocuments migration..." -ForegroundColor Cyan
$sql | psql -h localhost -U postgres -d cephasops

if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ Migration applied successfully!" -ForegroundColor Green
} else {
    Write-Host "✗ Migration failed!" -ForegroundColor Red
    exit 1
}

