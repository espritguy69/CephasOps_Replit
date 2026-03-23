-- Migration: Add BillingRatecard Table
-- Description: Creates table for Partner Rates (PU Rates) / Billing Ratecards
-- Date: 2025-01-XX

-- ============================================
-- BILLING RATECARDS TABLE (Partner Rates)
-- ============================================

-- BillingRatecards table
CREATE TABLE IF NOT EXISTS "BillingRatecards" (
    "Id" uuid NOT NULL,
    "CompanyId" uuid NOT NULL,
    "PartnerId" uuid NOT NULL,
    "OrderTypeId" uuid NOT NULL,
    "BuildingType" character varying(100) NULL,
    "Description" character varying(500) NULL,
    "Amount" numeric(18,2) NOT NULL,
    "TaxRate" numeric(5,4) NOT NULL DEFAULT 0,
    "IsActive" boolean NOT NULL DEFAULT true,
    "EffectiveFrom" timestamp with time zone NULL,
    "EffectiveTo" timestamp with time zone NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NULL,
    CONSTRAINT "PK_BillingRatecards" PRIMARY KEY ("Id")
);

-- Create indexes
CREATE INDEX IF NOT EXISTS "IX_BillingRatecards_CompanyId_PartnerId_OrderTypeId" 
    ON "BillingRatecards" ("CompanyId", "PartnerId", "OrderTypeId");

CREATE INDEX IF NOT EXISTS "IX_BillingRatecards_CompanyId_IsActive" 
    ON "BillingRatecards" ("CompanyId", "IsActive");

CREATE INDEX IF NOT EXISTS "IX_BillingRatecards_CompanyId_EffectiveFrom_EffectiveTo" 
    ON "BillingRatecards" ("CompanyId", "EffectiveFrom", "EffectiveTo");

-- Add comment
COMMENT ON TABLE "BillingRatecards" IS 'Billing ratecards for partners - defines rates per partner/order type combination';

