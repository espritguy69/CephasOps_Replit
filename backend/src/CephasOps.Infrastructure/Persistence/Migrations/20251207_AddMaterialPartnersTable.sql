-- ============================================
-- Migration: Add MaterialPartners Table
-- Date: 2025-12-07
-- Description: Creates join table for many-to-many relationship between Materials and Partners
-- ============================================

-- Create MaterialPartners table
CREATE TABLE IF NOT EXISTS "MaterialPartners" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "MaterialId" UUID NOT NULL,
    "PartnerId" UUID NOT NULL,
    "CompanyId" UUID NOT NULL,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMP WITH TIME ZONE,
    "IsDeleted" BOOLEAN NOT NULL DEFAULT FALSE,
    "DeletedAt" TIMESTAMP WITH TIME ZONE,
    "RowVersion" BYTEA,
    CONSTRAINT "FK_MaterialPartners_Materials" FOREIGN KEY ("MaterialId") 
        REFERENCES "Materials"("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_MaterialPartners_Partners" FOREIGN KEY ("PartnerId") 
        REFERENCES "Partners"("Id") ON DELETE RESTRICT,
    CONSTRAINT "UQ_MaterialPartners_Material_Partner_Company" 
        UNIQUE ("CompanyId", "MaterialId", "PartnerId")
);

-- Create indexes for performance
CREATE INDEX IF NOT EXISTS "IX_MaterialPartners_MaterialId" 
    ON "MaterialPartners" ("MaterialId");

CREATE INDEX IF NOT EXISTS "IX_MaterialPartners_PartnerId" 
    ON "MaterialPartners" ("PartnerId");

CREATE INDEX IF NOT EXISTS "IX_MaterialPartners_CompanyId" 
    ON "MaterialPartners" ("CompanyId");

CREATE INDEX IF NOT EXISTS "IX_MaterialPartners_CompanyId_MaterialId_PartnerId" 
    ON "MaterialPartners" ("CompanyId", "MaterialId", "PartnerId");

-- Add comments
COMMENT ON TABLE "MaterialPartners" IS 'Join table for many-to-many relationship between Materials and Partners. Allows a material to be associated with multiple partners.';
COMMENT ON COLUMN "MaterialPartners"."MaterialId" IS 'FK to Materials table';
COMMENT ON COLUMN "MaterialPartners"."PartnerId" IS 'FK to Partners table';
COMMENT ON COLUMN "MaterialPartners"."CompanyId" IS 'Company ID for scoping';

