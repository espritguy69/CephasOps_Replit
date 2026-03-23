-- Migration: Add Phase 3 Entities (Inventory, RMA, Billing)
-- Description: Creates tables for Inventory, RMA, and Billing modules
-- Date: 2025-01-XX

-- ============================================
-- INVENTORY MODULE TABLES
-- ============================================

-- Materials table
CREATE TABLE IF NOT EXISTS "Materials" (
    "Id" uuid NOT NULL,
    "CompanyId" uuid NOT NULL,
    "ItemCode" character varying(100) NOT NULL,
    "Description" character varying(500) NOT NULL,
    "Category" character varying(100) NULL,
    "IsSerialised" boolean NOT NULL,
    "UnitOfMeasure" character varying(50) NOT NULL,
    "DefaultCost" numeric(18,2) NULL,
    "PartnerId" uuid NULL,
    "VerticalFlags" character varying(200) NULL,
    "IsActive" boolean NOT NULL DEFAULT true,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_Materials" PRIMARY KEY ("Id")
);

-- StockLocations table
CREATE TABLE IF NOT EXISTS "StockLocations" (
    "Id" uuid NOT NULL,
    "CompanyId" uuid NOT NULL,
    "Name" character varying(200) NOT NULL,
    "Type" character varying(50) NOT NULL,
    "LinkedServiceInstallerId" uuid NULL,
    "LinkedBuildingId" uuid NULL,
    "IsActive" boolean NOT NULL DEFAULT true,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_StockLocations" PRIMARY KEY ("Id")
);

-- StockBalances table
CREATE TABLE IF NOT EXISTS "StockBalances" (
    "Id" uuid NOT NULL,
    "CompanyId" uuid NOT NULL,
    "MaterialId" uuid NOT NULL,
    "StockLocationId" uuid NOT NULL,
    "Quantity" numeric(18,3) NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_StockBalances" PRIMARY KEY ("Id")
);

-- StockMovements table
CREATE TABLE IF NOT EXISTS "StockMovements" (
    "Id" uuid NOT NULL,
    "CompanyId" uuid NOT NULL,
    "FromLocationId" uuid NULL,
    "ToLocationId" uuid NULL,
    "MaterialId" uuid NOT NULL,
    "Quantity" numeric(18,3) NOT NULL,
    "MovementType" character varying(50) NOT NULL,
    "OrderId" uuid NULL,
    "ServiceInstallerId" uuid NULL,
    "PartnerId" uuid NULL,
    "Remarks" character varying(1000) NULL,
    "CreatedByUserId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_StockMovements" PRIMARY KEY ("Id")
);

-- SerialisedItems table
CREATE TABLE IF NOT EXISTS "SerialisedItems" (
    "Id" uuid NOT NULL,
    "CompanyId" uuid NOT NULL,
    "MaterialId" uuid NOT NULL,
    "SerialNumber" character varying(200) NOT NULL,
    "CurrentLocationId" uuid NULL,
    "Status" character varying(50) NOT NULL DEFAULT 'InWarehouse',
    "LastOrderId" uuid NULL,
    "LastServiceId" uuid NULL,
    "Notes" character varying(1000) NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_SerialisedItems" PRIMARY KEY ("Id")
);

-- ============================================
-- RMA MODULE TABLES
-- ============================================

-- RmaRequests table
CREATE TABLE IF NOT EXISTS "RmaRequests" (
    "Id" uuid NOT NULL,
    "CompanyId" uuid NOT NULL,
    "PartnerId" uuid NOT NULL,
    "RmaNumber" character varying(100) NULL,
    "RequestDate" timestamp with time zone NOT NULL,
    "Reason" character varying(1000) NOT NULL,
    "Status" character varying(50) NOT NULL DEFAULT 'Requested',
    "MraDocumentId" uuid NULL,
    "CreatedByUserId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_RmaRequests" PRIMARY KEY ("Id")
);

-- RmaRequestItems table
CREATE TABLE IF NOT EXISTS "RmaRequestItems" (
    "Id" uuid NOT NULL,
    "CompanyId" uuid NOT NULL,
    "RmaRequestId" uuid NOT NULL,
    "SerialisedItemId" uuid NOT NULL,
    "OriginalOrderId" uuid NULL,
    "Notes" character varying(1000) NULL,
    "Result" character varying(50) NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_RmaRequestItems" PRIMARY KEY ("Id")
);

-- ============================================
-- BILLING MODULE TABLES
-- ============================================

-- Invoices table
CREATE TABLE IF NOT EXISTS "Invoices" (
    "Id" uuid NOT NULL,
    "CompanyId" uuid NOT NULL,
    "InvoiceNumber" character varying(100) NOT NULL,
    "PartnerId" uuid NOT NULL,
    "InvoiceDate" timestamp with time zone NOT NULL,
    "DueDate" timestamp with time zone NULL,
    "TotalAmount" numeric(18,2) NOT NULL,
    "TaxAmount" numeric(18,2) NOT NULL,
    "SubTotal" numeric(18,2) NOT NULL,
    "Status" character varying(50) NOT NULL DEFAULT 'Draft',
    "SubmissionId" character varying(200) NULL,
    "SubmittedAt" timestamp with time zone NULL,
    "PaidAt" timestamp with time zone NULL,
    "CreatedByUserId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_Invoices" PRIMARY KEY ("Id")
);

-- InvoiceLineItems table
CREATE TABLE IF NOT EXISTS "InvoiceLineItems" (
    "Id" uuid NOT NULL,
    "CompanyId" uuid NOT NULL,
    "InvoiceId" uuid NOT NULL,
    "Description" character varying(500) NOT NULL,
    "Quantity" numeric(18,3) NOT NULL,
    "UnitPrice" numeric(18,2) NOT NULL,
    "Total" numeric(18,2) NOT NULL,
    "OrderId" uuid NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_InvoiceLineItems" PRIMARY KEY ("Id")
);

-- ============================================
-- INDEXES
-- ============================================

-- Materials indexes
CREATE UNIQUE INDEX IF NOT EXISTS "IX_Materials_CompanyId_ItemCode" ON "Materials" ("CompanyId", "ItemCode");
CREATE INDEX IF NOT EXISTS "IX_Materials_CompanyId_Category" ON "Materials" ("CompanyId", "Category");
CREATE INDEX IF NOT EXISTS "IX_Materials_CompanyId_IsActive" ON "Materials" ("CompanyId", "IsActive");

-- StockLocations indexes
CREATE INDEX IF NOT EXISTS "IX_StockLocations_CompanyId_Name" ON "StockLocations" ("CompanyId", "Name");
CREATE INDEX IF NOT EXISTS "IX_StockLocations_CompanyId_Type" ON "StockLocations" ("CompanyId", "Type");

-- StockBalances indexes
CREATE UNIQUE INDEX IF NOT EXISTS "IX_StockBalances_CompanyId_MaterialId_StockLocationId" ON "StockBalances" ("CompanyId", "MaterialId", "StockLocationId");
CREATE INDEX IF NOT EXISTS "IX_StockBalances_CompanyId_StockLocationId" ON "StockBalances" ("CompanyId", "StockLocationId");

-- StockMovements indexes
CREATE INDEX IF NOT EXISTS "IX_StockMovements_CompanyId_MaterialId" ON "StockMovements" ("CompanyId", "MaterialId");
CREATE INDEX IF NOT EXISTS "IX_StockMovements_CompanyId_OrderId" ON "StockMovements" ("CompanyId", "OrderId");
CREATE INDEX IF NOT EXISTS "IX_StockMovements_CompanyId_CreatedAt" ON "StockMovements" ("CompanyId", "CreatedAt");
CREATE INDEX IF NOT EXISTS "IX_StockMovements_MovementType" ON "StockMovements" ("MovementType");

-- SerialisedItems indexes
CREATE UNIQUE INDEX IF NOT EXISTS "IX_SerialisedItems_CompanyId_SerialNumber" ON "SerialisedItems" ("CompanyId", "SerialNumber");
CREATE INDEX IF NOT EXISTS "IX_SerialisedItems_CompanyId_MaterialId" ON "SerialisedItems" ("CompanyId", "MaterialId");
CREATE INDEX IF NOT EXISTS "IX_SerialisedItems_CompanyId_Status" ON "SerialisedItems" ("CompanyId", "Status");
CREATE INDEX IF NOT EXISTS "IX_SerialisedItems_LastOrderId" ON "SerialisedItems" ("LastOrderId");

-- RmaRequests indexes
CREATE INDEX IF NOT EXISTS "IX_RmaRequests_CompanyId_PartnerId" ON "RmaRequests" ("CompanyId", "PartnerId");
CREATE INDEX IF NOT EXISTS "IX_RmaRequests_CompanyId_Status" ON "RmaRequests" ("CompanyId", "Status");
CREATE INDEX IF NOT EXISTS "IX_RmaRequests_CompanyId_RequestDate" ON "RmaRequests" ("CompanyId", "RequestDate");
CREATE INDEX IF NOT EXISTS "IX_RmaRequests_RmaNumber" ON "RmaRequests" ("RmaNumber");

-- RmaRequestItems indexes
CREATE INDEX IF NOT EXISTS "IX_RmaRequestItems_CompanyId_RmaRequestId" ON "RmaRequestItems" ("CompanyId", "RmaRequestId");
CREATE INDEX IF NOT EXISTS "IX_RmaRequestItems_SerialisedItemId" ON "RmaRequestItems" ("SerialisedItemId");

-- Invoices indexes
CREATE UNIQUE INDEX IF NOT EXISTS "IX_Invoices_CompanyId_InvoiceNumber" ON "Invoices" ("CompanyId", "InvoiceNumber");
CREATE INDEX IF NOT EXISTS "IX_Invoices_CompanyId_PartnerId" ON "Invoices" ("CompanyId", "PartnerId");
CREATE INDEX IF NOT EXISTS "IX_Invoices_CompanyId_Status" ON "Invoices" ("CompanyId", "Status");
CREATE INDEX IF NOT EXISTS "IX_Invoices_CompanyId_InvoiceDate" ON "Invoices" ("CompanyId", "InvoiceDate");
CREATE INDEX IF NOT EXISTS "IX_Invoices_SubmissionId" ON "Invoices" ("SubmissionId");

-- InvoiceLineItems indexes
CREATE INDEX IF NOT EXISTS "IX_InvoiceLineItems_CompanyId_InvoiceId" ON "InvoiceLineItems" ("CompanyId", "InvoiceId");
CREATE INDEX IF NOT EXISTS "IX_InvoiceLineItems_OrderId" ON "InvoiceLineItems" ("OrderId");

-- ============================================
-- FOREIGN KEY CONSTRAINTS
-- ============================================

-- StockBalances foreign keys
ALTER TABLE "StockBalances" 
    ADD CONSTRAINT "FK_StockBalances_Materials_MaterialId" 
    FOREIGN KEY ("MaterialId") REFERENCES "Materials" ("Id") ON DELETE RESTRICT;

ALTER TABLE "StockBalances" 
    ADD CONSTRAINT "FK_StockBalances_StockLocations_StockLocationId" 
    FOREIGN KEY ("StockLocationId") REFERENCES "StockLocations" ("Id") ON DELETE RESTRICT;

-- StockMovements foreign keys
ALTER TABLE "StockMovements" 
    ADD CONSTRAINT "FK_StockMovements_Materials_MaterialId" 
    FOREIGN KEY ("MaterialId") REFERENCES "Materials" ("Id") ON DELETE RESTRICT;

ALTER TABLE "StockMovements" 
    ADD CONSTRAINT "FK_StockMovements_StockLocations_FromLocationId" 
    FOREIGN KEY ("FromLocationId") REFERENCES "StockLocations" ("Id") ON DELETE RESTRICT;

ALTER TABLE "StockMovements" 
    ADD CONSTRAINT "FK_StockMovements_StockLocations_ToLocationId" 
    FOREIGN KEY ("ToLocationId") REFERENCES "StockLocations" ("Id") ON DELETE RESTRICT;

-- SerialisedItems foreign keys
ALTER TABLE "SerialisedItems" 
    ADD CONSTRAINT "FK_SerialisedItems_Materials_MaterialId" 
    FOREIGN KEY ("MaterialId") REFERENCES "Materials" ("Id") ON DELETE RESTRICT;

ALTER TABLE "SerialisedItems" 
    ADD CONSTRAINT "FK_SerialisedItems_StockLocations_CurrentLocationId" 
    FOREIGN KEY ("CurrentLocationId") REFERENCES "StockLocations" ("Id") ON DELETE RESTRICT;

-- RmaRequestItems foreign keys
ALTER TABLE "RmaRequestItems" 
    ADD CONSTRAINT "FK_RmaRequestItems_RmaRequests_RmaRequestId" 
    FOREIGN KEY ("RmaRequestId") REFERENCES "RmaRequests" ("Id") ON DELETE CASCADE;

-- InvoiceLineItems foreign keys
ALTER TABLE "InvoiceLineItems" 
    ADD CONSTRAINT "FK_InvoiceLineItems_Invoices_InvoiceId" 
    FOREIGN KEY ("InvoiceId") REFERENCES "Invoices" ("Id") ON DELETE CASCADE;

-- ============================================
-- COMMENTS
-- ============================================

COMMENT ON TABLE "Materials" IS 'Material master data - catalog of all items';
COMMENT ON TABLE "StockLocations" IS 'Stock locations - warehouses, SI locations, customer sites';
COMMENT ON TABLE "StockBalances" IS 'Current stock quantities per material and location';
COMMENT ON TABLE "StockMovements" IS 'Stock movement transaction history';
COMMENT ON TABLE "SerialisedItems" IS 'Serial number tracking for serialised materials';
COMMENT ON TABLE "RmaRequests" IS 'RMA request headers';
COMMENT ON TABLE "RmaRequestItems" IS 'RMA request line items';
COMMENT ON TABLE "Invoices" IS 'Invoice headers';
COMMENT ON TABLE "InvoiceLineItems" IS 'Invoice line items';

