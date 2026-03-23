-- Migration: Add CompanyDocuments Table
-- Description: Creates table for Company Documents (legal, banking, tax, contracts, etc.)
-- Date: 2025-01-XX

-- ============================================
-- COMPANY DOCUMENTS TABLE
-- ============================================

-- CompanyDocuments table
CREATE TABLE IF NOT EXISTS "CompanyDocuments" (
    "Id" uuid NOT NULL,
    "CompanyId" uuid NOT NULL,
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

-- Add comment
COMMENT ON TABLE "CompanyDocuments" IS 'Company-specific documents: legal, banking, tax, contracts, tenancy, insurance, licences';


