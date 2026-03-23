-- Migration: Add P&L Types, Assets, and Accounting tables
-- Date: 2025-11-26

-- =============================================
-- P&L Types (Hierarchical expense/income categories)
-- =============================================
CREATE TABLE IF NOT EXISTS "PnlTypes" (
    "Id" uuid NOT NULL DEFAULT gen_random_uuid(),
    "CompanyId" uuid,
    "Name" varchar(100) NOT NULL,
    "Code" varchar(50) NOT NULL,
    "Description" varchar(500),
    "Category" varchar(20) NOT NULL, -- 'Income' or 'Expense'
    "ParentId" uuid,
    "SortOrder" int NOT NULL DEFAULT 0,
    "IsActive" boolean NOT NULL DEFAULT true,
    "IsTransactional" boolean NOT NULL DEFAULT true,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT NOW(),
    "UpdatedAt" timestamp with time zone NOT NULL DEFAULT NOW(),
    CONSTRAINT "PK_PnlTypes" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_PnlTypes_Parent" FOREIGN KEY ("ParentId") REFERENCES "PnlTypes"("Id") ON DELETE RESTRICT
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_PnlTypes_CompanyId_Code" ON "PnlTypes" ("CompanyId", "Code");
CREATE INDEX IF NOT EXISTS "IX_PnlTypes_CompanyId_Category" ON "PnlTypes" ("CompanyId", "Category");
CREATE INDEX IF NOT EXISTS "IX_PnlTypes_CompanyId_ParentId" ON "PnlTypes" ("CompanyId", "ParentId");
CREATE INDEX IF NOT EXISTS "IX_PnlTypes_IsActive" ON "PnlTypes" ("IsActive");

-- =============================================
-- Asset Types
-- =============================================
CREATE TABLE IF NOT EXISTS "AssetTypes" (
    "Id" uuid NOT NULL DEFAULT gen_random_uuid(),
    "CompanyId" uuid,
    "Name" varchar(100) NOT NULL,
    "Code" varchar(20) NOT NULL,
    "Description" varchar(500),
    "DefaultDepreciationMethod" varchar(30) NOT NULL DEFAULT 'StraightLine',
    "DefaultUsefulLifeMonths" int NOT NULL DEFAULT 60,
    "DefaultSalvageValuePercent" decimal(5,2) NOT NULL DEFAULT 10,
    "DepreciationPnlTypeId" uuid,
    "IsActive" boolean NOT NULL DEFAULT true,
    "SortOrder" int NOT NULL DEFAULT 0,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT NOW(),
    "UpdatedAt" timestamp with time zone NOT NULL DEFAULT NOW(),
    CONSTRAINT "PK_AssetTypes" PRIMARY KEY ("Id")
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_AssetTypes_CompanyId_Code" ON "AssetTypes" ("CompanyId", "Code");
CREATE INDEX IF NOT EXISTS "IX_AssetTypes_IsActive" ON "AssetTypes" ("IsActive");

-- =============================================
-- Assets
-- =============================================
CREATE TABLE IF NOT EXISTS "Assets" (
    "Id" uuid NOT NULL DEFAULT gen_random_uuid(),
    "CompanyId" uuid,
    "AssetTypeId" uuid NOT NULL,
    "AssetTag" varchar(50) NOT NULL,
    "Name" varchar(200) NOT NULL,
    "Description" varchar(1000),
    "SerialNumber" varchar(100),
    "ModelNumber" varchar(100),
    "Manufacturer" varchar(100),
    "Supplier" varchar(200),
    "SupplierInvoiceId" uuid,
    "PurchaseDate" timestamp with time zone NOT NULL,
    "InServiceDate" timestamp with time zone,
    "PurchaseCost" decimal(18,2) NOT NULL,
    "SalvageValue" decimal(18,2) NOT NULL DEFAULT 0,
    "DepreciationMethod" varchar(30) NOT NULL DEFAULT 'StraightLine',
    "UsefulLifeMonths" int NOT NULL DEFAULT 60,
    "CurrentBookValue" decimal(18,2) NOT NULL,
    "AccumulatedDepreciation" decimal(18,2) NOT NULL DEFAULT 0,
    "LastDepreciationDate" timestamp with time zone,
    "Status" varchar(20) NOT NULL DEFAULT 'Active',
    "Location" varchar(200),
    "DepartmentId" uuid,
    "AssignedToUserId" uuid,
    "CostCentreId" uuid,
    "WarrantyExpiryDate" timestamp with time zone,
    "InsurancePolicyNumber" varchar(100),
    "InsuranceExpiryDate" timestamp with time zone,
    "NextMaintenanceDate" timestamp with time zone,
    "Notes" varchar(2000),
    "IsFullyDepreciated" boolean NOT NULL DEFAULT false,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT NOW(),
    "UpdatedAt" timestamp with time zone NOT NULL DEFAULT NOW(),
    CONSTRAINT "PK_Assets" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Assets_AssetType" FOREIGN KEY ("AssetTypeId") REFERENCES "AssetTypes"("Id") ON DELETE RESTRICT
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_Assets_CompanyId_AssetTag" ON "Assets" ("CompanyId", "AssetTag");
CREATE INDEX IF NOT EXISTS "IX_Assets_CompanyId_AssetTypeId" ON "Assets" ("CompanyId", "AssetTypeId");
CREATE INDEX IF NOT EXISTS "IX_Assets_CompanyId_Status" ON "Assets" ("CompanyId", "Status");
CREATE INDEX IF NOT EXISTS "IX_Assets_SerialNumber" ON "Assets" ("SerialNumber");
CREATE INDEX IF NOT EXISTS "IX_Assets_DepartmentId" ON "Assets" ("DepartmentId");
CREATE INDEX IF NOT EXISTS "IX_Assets_AssignedToUserId" ON "Assets" ("AssignedToUserId");

-- =============================================
-- Asset Maintenance Records
-- =============================================
CREATE TABLE IF NOT EXISTS "AssetMaintenanceRecords" (
    "Id" uuid NOT NULL DEFAULT gen_random_uuid(),
    "CompanyId" uuid,
    "AssetId" uuid NOT NULL,
    "MaintenanceType" varchar(30) NOT NULL,
    "Description" varchar(1000) NOT NULL,
    "ScheduledDate" timestamp with time zone,
    "PerformedDate" timestamp with time zone,
    "NextScheduledDate" timestamp with time zone,
    "Cost" decimal(18,2) NOT NULL DEFAULT 0,
    "PnlTypeId" uuid,
    "PerformedBy" varchar(200),
    "SupplierInvoiceId" uuid,
    "ReferenceNumber" varchar(100),
    "Notes" varchar(2000),
    "IsCompleted" boolean NOT NULL DEFAULT false,
    "RecordedByUserId" uuid,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT NOW(),
    "UpdatedAt" timestamp with time zone NOT NULL DEFAULT NOW(),
    CONSTRAINT "PK_AssetMaintenanceRecords" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_AssetMaintenance_Asset" FOREIGN KEY ("AssetId") REFERENCES "Assets"("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_AssetMaintenance_CompanyId_AssetId" ON "AssetMaintenanceRecords" ("CompanyId", "AssetId");
CREATE INDEX IF NOT EXISTS "IX_AssetMaintenance_ScheduledDate" ON "AssetMaintenanceRecords" ("ScheduledDate");
CREATE INDEX IF NOT EXISTS "IX_AssetMaintenance_PerformedDate" ON "AssetMaintenanceRecords" ("PerformedDate");
CREATE INDEX IF NOT EXISTS "IX_AssetMaintenance_NextScheduledDate" ON "AssetMaintenanceRecords" ("NextScheduledDate");
CREATE INDEX IF NOT EXISTS "IX_AssetMaintenance_IsCompleted" ON "AssetMaintenanceRecords" ("IsCompleted");

-- =============================================
-- Asset Depreciation Entries
-- =============================================
CREATE TABLE IF NOT EXISTS "AssetDepreciationEntries" (
    "Id" uuid NOT NULL DEFAULT gen_random_uuid(),
    "CompanyId" uuid,
    "AssetId" uuid NOT NULL,
    "Period" varchar(10) NOT NULL, -- e.g., '2025-01'
    "DepreciationAmount" decimal(18,2) NOT NULL,
    "OpeningBookValue" decimal(18,2) NOT NULL,
    "ClosingBookValue" decimal(18,2) NOT NULL,
    "AccumulatedDepreciation" decimal(18,2) NOT NULL,
    "PnlTypeId" uuid,
    "IsPosted" boolean NOT NULL DEFAULT false,
    "CalculatedAt" timestamp with time zone NOT NULL DEFAULT NOW(),
    "Notes" varchar(500),
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT NOW(),
    "UpdatedAt" timestamp with time zone NOT NULL DEFAULT NOW(),
    CONSTRAINT "PK_AssetDepreciationEntries" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_AssetDepreciation_Asset" FOREIGN KEY ("AssetId") REFERENCES "Assets"("Id") ON DELETE CASCADE
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_AssetDepreciation_CompanyId_AssetId_Period" ON "AssetDepreciationEntries" ("CompanyId", "AssetId", "Period");
CREATE INDEX IF NOT EXISTS "IX_AssetDepreciation_CompanyId_Period" ON "AssetDepreciationEntries" ("CompanyId", "Period");
CREATE INDEX IF NOT EXISTS "IX_AssetDepreciation_IsPosted" ON "AssetDepreciationEntries" ("IsPosted");

-- =============================================
-- Asset Disposals
-- =============================================
CREATE TABLE IF NOT EXISTS "AssetDisposals" (
    "Id" uuid NOT NULL DEFAULT gen_random_uuid(),
    "CompanyId" uuid,
    "AssetId" uuid NOT NULL,
    "DisposalMethod" varchar(20) NOT NULL,
    "DisposalDate" timestamp with time zone NOT NULL,
    "BookValueAtDisposal" decimal(18,2) NOT NULL,
    "DisposalProceeds" decimal(18,2) NOT NULL DEFAULT 0,
    "GainLoss" decimal(18,2) NOT NULL DEFAULT 0,
    "PnlTypeId" uuid,
    "BuyerName" varchar(200),
    "ReferenceNumber" varchar(100),
    "Reason" varchar(500),
    "Notes" varchar(2000),
    "ProcessedByUserId" uuid,
    "IsApproved" boolean NOT NULL DEFAULT false,
    "ApprovedByUserId" uuid,
    "ApprovalDate" timestamp with time zone,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT NOW(),
    "UpdatedAt" timestamp with time zone NOT NULL DEFAULT NOW(),
    CONSTRAINT "PK_AssetDisposals" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_AssetDisposal_Asset" FOREIGN KEY ("AssetId") REFERENCES "Assets"("Id") ON DELETE CASCADE
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_AssetDisposals_CompanyId_AssetId" ON "AssetDisposals" ("CompanyId", "AssetId");
CREATE INDEX IF NOT EXISTS "IX_AssetDisposals_DisposalDate" ON "AssetDisposals" ("DisposalDate");
CREATE INDEX IF NOT EXISTS "IX_AssetDisposals_IsApproved" ON "AssetDisposals" ("IsApproved");

-- =============================================
-- Supplier Invoices
-- =============================================
CREATE TABLE IF NOT EXISTS "SupplierInvoices" (
    "Id" uuid NOT NULL DEFAULT gen_random_uuid(),
    "CompanyId" uuid,
    "InvoiceNumber" varchar(100) NOT NULL,
    "InternalReference" varchar(50),
    "SupplierName" varchar(200) NOT NULL,
    "SupplierTaxNumber" varchar(50),
    "SupplierAddress" varchar(500),
    "SupplierEmail" varchar(200),
    "InvoiceDate" timestamp with time zone NOT NULL,
    "ReceivedDate" timestamp with time zone NOT NULL DEFAULT NOW(),
    "DueDate" timestamp with time zone,
    "SubTotal" decimal(18,2) NOT NULL,
    "TaxAmount" decimal(18,2) NOT NULL DEFAULT 0,
    "TotalAmount" decimal(18,2) NOT NULL,
    "AmountPaid" decimal(18,2) NOT NULL DEFAULT 0,
    "OutstandingAmount" decimal(18,2) NOT NULL,
    "Currency" varchar(3) NOT NULL DEFAULT 'MYR',
    "Status" varchar(20) NOT NULL DEFAULT 'Draft',
    "CostCentreId" uuid,
    "DefaultPnlTypeId" uuid,
    "Description" varchar(500),
    "Notes" varchar(2000),
    "AttachmentPath" varchar(500),
    "CreatedByUserId" uuid NOT NULL,
    "ApprovedByUserId" uuid,
    "ApprovedAt" timestamp with time zone,
    "PaidAt" timestamp with time zone,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT NOW(),
    "UpdatedAt" timestamp with time zone NOT NULL DEFAULT NOW(),
    CONSTRAINT "PK_SupplierInvoices" PRIMARY KEY ("Id")
);

CREATE INDEX IF NOT EXISTS "IX_SupplierInvoices_CompanyId_InvoiceNumber" ON "SupplierInvoices" ("CompanyId", "InvoiceNumber");
CREATE INDEX IF NOT EXISTS "IX_SupplierInvoices_CompanyId_SupplierName" ON "SupplierInvoices" ("CompanyId", "SupplierName");
CREATE INDEX IF NOT EXISTS "IX_SupplierInvoices_CompanyId_Status" ON "SupplierInvoices" ("CompanyId", "Status");
CREATE INDEX IF NOT EXISTS "IX_SupplierInvoices_InvoiceDate" ON "SupplierInvoices" ("InvoiceDate");
CREATE INDEX IF NOT EXISTS "IX_SupplierInvoices_DueDate" ON "SupplierInvoices" ("DueDate");

-- =============================================
-- Supplier Invoice Line Items
-- =============================================
CREATE TABLE IF NOT EXISTS "SupplierInvoiceLineItems" (
    "Id" uuid NOT NULL DEFAULT gen_random_uuid(),
    "CompanyId" uuid,
    "SupplierInvoiceId" uuid NOT NULL,
    "LineNumber" int NOT NULL,
    "Description" varchar(500) NOT NULL,
    "Quantity" decimal(18,4) NOT NULL DEFAULT 1,
    "UnitOfMeasure" varchar(20),
    "UnitPrice" decimal(18,4) NOT NULL,
    "LineTotal" decimal(18,2) NOT NULL,
    "TaxRate" decimal(5,2) NOT NULL DEFAULT 0,
    "TaxAmount" decimal(18,2) NOT NULL DEFAULT 0,
    "TotalWithTax" decimal(18,2) NOT NULL,
    "PnlTypeId" uuid,
    "CostCentreId" uuid,
    "AssetId" uuid,
    "Notes" varchar(500),
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT NOW(),
    "UpdatedAt" timestamp with time zone NOT NULL DEFAULT NOW(),
    CONSTRAINT "PK_SupplierInvoiceLineItems" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_SupplierInvoiceLineItem_Invoice" FOREIGN KEY ("SupplierInvoiceId") REFERENCES "SupplierInvoices"("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_SupplierInvoiceLineItems_InvoiceId_LineNumber" ON "SupplierInvoiceLineItems" ("SupplierInvoiceId", "LineNumber");
CREATE INDEX IF NOT EXISTS "IX_SupplierInvoiceLineItems_PnlTypeId" ON "SupplierInvoiceLineItems" ("PnlTypeId");
CREATE INDEX IF NOT EXISTS "IX_SupplierInvoiceLineItems_AssetId" ON "SupplierInvoiceLineItems" ("AssetId");

-- =============================================
-- Payments (Income/Expense)
-- =============================================
CREATE TABLE IF NOT EXISTS "Payments" (
    "Id" uuid NOT NULL DEFAULT gen_random_uuid(),
    "CompanyId" uuid,
    "PaymentNumber" varchar(50) NOT NULL,
    "PaymentType" varchar(10) NOT NULL, -- 'Income' or 'Expense'
    "PaymentMethod" varchar(20) NOT NULL,
    "PaymentDate" timestamp with time zone NOT NULL,
    "Amount" decimal(18,2) NOT NULL,
    "Currency" varchar(3) NOT NULL DEFAULT 'MYR',
    "PayerPayeeName" varchar(200) NOT NULL,
    "BankAccount" varchar(50),
    "BankReference" varchar(100),
    "ChequeNumber" varchar(50),
    "InvoiceId" uuid, -- Customer invoice (for income)
    "SupplierInvoiceId" uuid, -- Supplier invoice (for expense)
    "PnlTypeId" uuid, -- For direct income/expense not linked to invoices
    "CostCentreId" uuid,
    "Description" varchar(500),
    "Notes" varchar(2000),
    "AttachmentPath" varchar(500),
    "IsReconciled" boolean NOT NULL DEFAULT false,
    "ReconciledAt" timestamp with time zone,
    "CreatedByUserId" uuid NOT NULL,
    "IsVoided" boolean NOT NULL DEFAULT false,
    "VoidReason" varchar(500),
    "VoidedAt" timestamp with time zone,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT NOW(),
    "UpdatedAt" timestamp with time zone NOT NULL DEFAULT NOW(),
    CONSTRAINT "PK_Payments" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Payments_Invoice" FOREIGN KEY ("InvoiceId") REFERENCES "Invoices"("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_Payments_SupplierInvoice" FOREIGN KEY ("SupplierInvoiceId") REFERENCES "SupplierInvoices"("Id") ON DELETE RESTRICT
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_Payments_CompanyId_PaymentNumber" ON "Payments" ("CompanyId", "PaymentNumber");
CREATE INDEX IF NOT EXISTS "IX_Payments_CompanyId_PaymentType" ON "Payments" ("CompanyId", "PaymentType");
CREATE INDEX IF NOT EXISTS "IX_Payments_CompanyId_PaymentDate" ON "Payments" ("CompanyId", "PaymentDate");
CREATE INDEX IF NOT EXISTS "IX_Payments_InvoiceId" ON "Payments" ("InvoiceId");
CREATE INDEX IF NOT EXISTS "IX_Payments_SupplierInvoiceId" ON "Payments" ("SupplierInvoiceId");
CREATE INDEX IF NOT EXISTS "IX_Payments_IsReconciled" ON "Payments" ("IsReconciled");
CREATE INDEX IF NOT EXISTS "IX_Payments_IsVoided" ON "Payments" ("IsVoided");

-- =============================================
-- Seed default P&L Types (can be customized per company)
-- =============================================
-- Income categories
INSERT INTO "PnlTypes" ("Id", "CompanyId", "Name", "Code", "Category", "ParentId", "SortOrder", "IsActive", "IsTransactional")
VALUES
    (gen_random_uuid(), NULL, 'Revenue', 'INC-REV', 'Income', NULL, 1, true, false),
    (gen_random_uuid(), NULL, 'Other Income', 'INC-OTH', 'Income', NULL, 2, true, false);

-- Expense categories
INSERT INTO "PnlTypes" ("Id", "CompanyId", "Name", "Code", "Category", "ParentId", "SortOrder", "IsActive", "IsTransactional")
VALUES
    (gen_random_uuid(), NULL, 'Operating Expenses', 'EXP-OPS', 'Expense', NULL, 1, true, false),
    (gen_random_uuid(), NULL, 'Administrative Expenses', 'EXP-ADM', 'Expense', NULL, 2, true, false),
    (gen_random_uuid(), NULL, 'Depreciation', 'EXP-DEP', 'Expense', NULL, 3, true, false);

-- =============================================
-- Seed default Asset Types
-- =============================================
INSERT INTO "AssetTypes" ("Id", "CompanyId", "Name", "Code", "Description", "DefaultDepreciationMethod", "DefaultUsefulLifeMonths", "DefaultSalvageValuePercent", "IsActive", "SortOrder")
VALUES
    (gen_random_uuid(), NULL, 'Vehicles', 'VEH', 'Motor vehicles and transportation', 'StraightLine', 60, 10, true, 1),
    (gen_random_uuid(), NULL, 'Computers & IT Equipment', 'IT', 'Computers, servers, and IT equipment', 'StraightLine', 36, 5, true, 2),
    (gen_random_uuid(), NULL, 'Office Equipment', 'OFF', 'Office equipment and machinery', 'StraightLine', 60, 10, true, 3),
    (gen_random_uuid(), NULL, 'Furniture & Fixtures', 'FURN', 'Furniture and fixtures', 'StraightLine', 84, 10, true, 4),
    (gen_random_uuid(), NULL, 'Tools & Equipment', 'TOOL', 'Tools and specialized equipment', 'StraightLine', 60, 10, true, 5),
    (gen_random_uuid(), NULL, 'Land & Buildings', 'LAND', 'Land and buildings', 'None', 0, 0, true, 6);

