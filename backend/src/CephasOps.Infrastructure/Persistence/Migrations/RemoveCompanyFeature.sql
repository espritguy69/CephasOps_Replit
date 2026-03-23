-- Migration: Remove Company Feature
-- This migration removes all company-related tables and makes CompanyId nullable in all other tables
-- Run this migration to completely remove multi-company support

BEGIN;

-- Step 1: Drop foreign key constraints that reference Companies table
-- Note: This will fail if there are existing records with foreign keys
-- You may need to delete or update those records first

-- Drop UserCompanies junction table (users to companies)
DROP TABLE IF EXISTS "UserCompanies" CASCADE;

-- Drop CompanyDocuments table
DROP TABLE IF EXISTS "CompanyDocuments" CASCADE;

-- Drop Verticals table (has CompanyId FK)
DROP TABLE IF EXISTS "Verticals" CASCADE;

-- Drop PartnerGroups table (has CompanyId FK)
DROP TABLE IF EXISTS "PartnerGroups" CASCADE;

-- Drop CostCentres table (has CompanyId FK)
DROP TABLE IF EXISTS "CostCentres" CASCADE;

-- Note: Partners table has CompanyId but is still needed, so we'll make it nullable instead
-- Drop Companies table (main company table)
DROP TABLE IF EXISTS "Companies" CASCADE;

-- Step 2: Make CompanyId nullable in all remaining tables
-- This allows existing data to keep CompanyId but new records don't need it
-- Using DO block to handle tables that might not exist

DO $$
BEGIN
    -- Orders and related
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'Orders' AND table_schema = 'public') THEN
        ALTER TABLE "Orders" ALTER COLUMN "CompanyId" DROP NOT NULL;
    END IF;
    
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'OrderMaterialUsage' AND table_schema = 'public') THEN
        ALTER TABLE "OrderMaterialUsage" ALTER COLUMN "CompanyId" DROP NOT NULL;
    END IF;
    
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'OrderStatusLogs' AND table_schema = 'public') THEN
        ALTER TABLE "OrderStatusLogs" ALTER COLUMN "CompanyId" DROP NOT NULL;
    END IF;

    -- Inventory
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'Materials' AND table_schema = 'public') THEN
        ALTER TABLE "Materials" ALTER COLUMN "CompanyId" DROP NOT NULL;
    END IF;
    
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'StockBalances' AND table_schema = 'public') THEN
        ALTER TABLE "StockBalances" ALTER COLUMN "CompanyId" DROP NOT NULL;
    END IF;
    
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'StockMovements' AND table_schema = 'public') THEN
        ALTER TABLE "StockMovements" ALTER COLUMN "CompanyId" DROP NOT NULL;
    END IF;
    
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'StockLocations' AND table_schema = 'public') THEN
        ALTER TABLE "StockLocations" ALTER COLUMN "CompanyId" DROP NOT NULL;
    END IF;
    
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'SerialisedItems' AND table_schema = 'public') THEN
        ALTER TABLE "SerialisedItems" ALTER COLUMN "CompanyId" DROP NOT NULL;
    END IF;

    -- RMA
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'RmaRequests' AND table_schema = 'public') THEN
        ALTER TABLE "RmaRequests" ALTER COLUMN "CompanyId" DROP NOT NULL;
    END IF;
    
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'RmaRequestItems' AND table_schema = 'public') THEN
        ALTER TABLE "RmaRequestItems" ALTER COLUMN "CompanyId" DROP NOT NULL;
    END IF;

    -- Billing
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'Invoices' AND table_schema = 'public') THEN
        ALTER TABLE "Invoices" ALTER COLUMN "CompanyId" DROP NOT NULL;
    END IF;
    
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'InvoiceLineItems' AND table_schema = 'public') THEN
        ALTER TABLE "InvoiceLineItems" ALTER COLUMN "CompanyId" DROP NOT NULL;
    END IF;

    -- Service Installers
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'ServiceInstallers' AND table_schema = 'public') THEN
        ALTER TABLE "ServiceInstallers" ALTER COLUMN "CompanyId" DROP NOT NULL;
    END IF;

    -- Buildings and Splitters
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'Buildings' AND table_schema = 'public') THEN
        ALTER TABLE "Buildings" ALTER COLUMN "CompanyId" DROP NOT NULL;
    END IF;
    
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'Splitters' AND table_schema = 'public') THEN
        ALTER TABLE "Splitters" ALTER COLUMN "CompanyId" DROP NOT NULL;
    END IF;

    -- Partners (keep but make nullable)
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'Partners' AND table_schema = 'public') THEN
        ALTER TABLE "Partners" ALTER COLUMN "CompanyId" DROP NOT NULL;
    END IF;

    -- Departments
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'Departments' AND table_schema = 'public') THEN
        ALTER TABLE "Departments" ALTER COLUMN "CompanyId" DROP NOT NULL;
    END IF;
    
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'MaterialAllocations' AND table_schema = 'public') THEN
        ALTER TABLE "MaterialAllocations" ALTER COLUMN "CompanyId" DROP NOT NULL;
    END IF;

    -- Scheduler
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'ScheduledSlots' AND table_schema = 'public') THEN
        ALTER TABLE "ScheduledSlots" ALTER COLUMN "CompanyId" DROP NOT NULL;
    END IF;
    
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'SiAvailabilities' AND table_schema = 'public') THEN
        ALTER TABLE "SiAvailabilities" ALTER COLUMN "CompanyId" DROP NOT NULL;
    END IF;

    -- Payroll
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'PayrollRuns' AND table_schema = 'public') THEN
        ALTER TABLE "PayrollRuns" ALTER COLUMN "CompanyId" DROP NOT NULL;
    END IF;

    -- P&L
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'PnlFacts' AND table_schema = 'public') THEN
        ALTER TABLE "PnlFacts" ALTER COLUMN "CompanyId" DROP NOT NULL;
    END IF;
    
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'PnlDetailPerOrders' AND table_schema = 'public') THEN
        ALTER TABLE "PnlDetailPerOrders" ALTER COLUMN "CompanyId" DROP NOT NULL;
    END IF;

    -- Settings
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'MaterialTemplates' AND table_schema = 'public') THEN
        ALTER TABLE "MaterialTemplates" ALTER COLUMN "CompanyId" DROP NOT NULL;
    END IF;
    
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'DocumentTemplates' AND table_schema = 'public') THEN
        ALTER TABLE "DocumentTemplates" ALTER COLUMN "CompanyId" DROP NOT NULL;
    END IF;
    
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'KpiProfiles' AND table_schema = 'public') THEN
        ALTER TABLE "KpiProfiles" ALTER COLUMN "CompanyId" DROP NOT NULL;
    END IF;
    
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'BillingRatecards' AND table_schema = 'public') THEN
        ALTER TABLE "BillingRatecards" ALTER COLUMN "CompanyId" DROP NOT NULL;
    END IF;
END $$;

COMMIT;

-- Note: After running this migration, you may want to:
-- 1. Update existing CompanyId values to NULL or Guid.Empty
-- 2. Remove CompanyId columns entirely (requires another migration)
-- 3. Update application code to not reference CompanyId
