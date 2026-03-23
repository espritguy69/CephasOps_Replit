-- Migration: Add Phase 4 Entities (Payroll, P&L, Departments)
-- Description: Creates tables for Payroll, P&L, and Departments modules
-- Date: 2025-01-XX

-- ============================================
-- PAYROLL MODULE TABLES
-- ============================================

-- PayrollPeriods table
CREATE TABLE IF NOT EXISTS "PayrollPeriods" (
    "Id" uuid NOT NULL,
    "CompanyId" uuid NOT NULL,
    "Period" character varying(20) NOT NULL,
    "PeriodStart" timestamp with time zone NOT NULL,
    "PeriodEnd" timestamp with time zone NOT NULL,
    "Status" character varying(50) NOT NULL,
    "IsLocked" boolean NOT NULL DEFAULT false,
    "CreatedByUserId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_PayrollPeriods" PRIMARY KEY ("Id")
);

-- PayrollRuns table
CREATE TABLE IF NOT EXISTS "PayrollRuns" (
    "Id" uuid NOT NULL,
    "CompanyId" uuid NOT NULL,
    "PayrollPeriodId" uuid NULL,
    "PeriodStart" timestamp with time zone NOT NULL,
    "PeriodEnd" timestamp with time zone NOT NULL,
    "Status" character varying(50) NOT NULL,
    "TotalAmount" numeric(18,2) NOT NULL DEFAULT 0,
    "Notes" character varying(1000) NULL,
    "ExportReference" character varying(200) NULL,
    "CreatedByUserId" uuid NOT NULL,
    "FinalizedAt" timestamp with time zone NULL,
    "PaidAt" timestamp with time zone NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_PayrollRuns" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_PayrollRuns_PayrollPeriods_PayrollPeriodId" FOREIGN KEY ("PayrollPeriodId") REFERENCES "PayrollPeriods" ("Id") ON DELETE RESTRICT
);

-- PayrollLines table
CREATE TABLE IF NOT EXISTS "PayrollLines" (
    "Id" uuid NOT NULL,
    "CompanyId" uuid NOT NULL,
    "PayrollRunId" uuid NOT NULL,
    "ServiceInstallerId" uuid NOT NULL,
    "TotalJobs" integer NOT NULL DEFAULT 0,
    "TotalPay" numeric(18,2) NOT NULL DEFAULT 0,
    "Adjustments" numeric(18,2) NOT NULL DEFAULT 0,
    "NetPay" numeric(18,2) NOT NULL DEFAULT 0,
    "ExportReference" character varying(200) NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_PayrollLines" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_PayrollLines_PayrollRuns_PayrollRunId" FOREIGN KEY ("PayrollRunId") REFERENCES "PayrollRuns" ("Id") ON DELETE CASCADE
);

-- JobEarningRecords table
CREATE TABLE IF NOT EXISTS "JobEarningRecords" (
    "Id" uuid NOT NULL,
    "CompanyId" uuid NOT NULL,
    "PayrollRunId" uuid NULL,
    "OrderId" uuid NOT NULL,
    "ServiceInstallerId" uuid NOT NULL,
    "JobType" character varying(50) NOT NULL,
    "KpiResult" character varying(50) NULL,
    "BaseRate" numeric(18,2) NOT NULL DEFAULT 0,
    "KpiAdjustment" numeric(18,2) NOT NULL DEFAULT 0,
    "FinalPay" numeric(18,2) NOT NULL DEFAULT 0,
    "Period" character varying(20) NOT NULL,
    "Status" character varying(50) NOT NULL,
    "ConfirmedAt" timestamp with time zone NULL,
    "PaidAt" timestamp with time zone NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_JobEarningRecords" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_JobEarningRecords_PayrollRuns_PayrollRunId" FOREIGN KEY ("PayrollRunId") REFERENCES "PayrollRuns" ("Id") ON DELETE SET NULL
);

-- SiRatePlans table
CREATE TABLE IF NOT EXISTS "SiRatePlans" (
    "Id" uuid NOT NULL,
    "CompanyId" uuid NOT NULL,
    "ServiceInstallerId" uuid NOT NULL,
    "Level" character varying(50) NOT NULL,
    "ActivationRate" numeric(18,2) NULL,
    "ModificationRate" numeric(18,2) NULL,
    "AssuranceRate" numeric(18,2) NULL,
    "FttrRate" numeric(18,2) NULL,
    "FttcRate" numeric(18,2) NULL,
    "SduRate" numeric(18,2) NULL,
    "RdfPoleRate" numeric(18,2) NULL,
    "OnTimeBonus" numeric(18,2) NULL,
    "LatePenalty" numeric(18,2) NULL,
    "ReworkRate" numeric(18,2) NULL,
    "IsActive" boolean NOT NULL DEFAULT true,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_SiRatePlans" PRIMARY KEY ("Id")
);

-- ============================================
-- P&L MODULE TABLES
-- ============================================

-- PnlPeriods table
CREATE TABLE IF NOT EXISTS "PnlPeriods" (
    "Id" uuid NOT NULL,
    "CompanyId" uuid NOT NULL,
    "Period" character varying(20) NOT NULL,
    "PeriodStart" timestamp with time zone NOT NULL,
    "PeriodEnd" timestamp with time zone NOT NULL,
    "IsLocked" boolean NOT NULL DEFAULT false,
    "CreatedByUserId" uuid NOT NULL,
    "LastRecalculatedAt" timestamp with time zone NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_PnlPeriods" PRIMARY KEY ("Id")
);

-- PnlFacts table
CREATE TABLE IF NOT EXISTS "PnlFacts" (
    "Id" uuid NOT NULL,
    "CompanyId" uuid NOT NULL,
    "PnlPeriodId" uuid NULL,
    "PartnerId" uuid NULL,
    "Vertical" character varying(50) NULL,
    "CostCentreId" uuid NULL,
    "Period" character varying(20) NOT NULL,
    "OrderType" character varying(50) NULL,
    "RevenueAmount" numeric(18,2) NOT NULL DEFAULT 0,
    "DirectMaterialCost" numeric(18,2) NOT NULL DEFAULT 0,
    "DirectLabourCost" numeric(18,2) NOT NULL DEFAULT 0,
    "IndirectCost" numeric(18,2) NOT NULL DEFAULT 0,
    "GrossProfit" numeric(18,2) NOT NULL DEFAULT 0,
    "NetProfit" numeric(18,2) NOT NULL DEFAULT 0,
    "JobsCount" integer NOT NULL DEFAULT 0,
    "OrdersCompletedCount" integer NOT NULL DEFAULT 0,
    "ReschedulesCount" integer NOT NULL DEFAULT 0,
    "AssuranceJobsCount" integer NOT NULL DEFAULT 0,
    "LastRecalculatedAt" timestamp with time zone NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_PnlFacts" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_PnlFacts_PnlPeriods_PnlPeriodId" FOREIGN KEY ("PnlPeriodId") REFERENCES "PnlPeriods" ("Id") ON DELETE RESTRICT
);

-- PnlDetailPerOrders table
CREATE TABLE IF NOT EXISTS "PnlDetailPerOrders" (
    "Id" uuid NOT NULL,
    "CompanyId" uuid NOT NULL,
    "OrderId" uuid NOT NULL,
    "PartnerId" uuid NOT NULL,
    "Period" character varying(20) NOT NULL,
    "OrderType" character varying(50) NOT NULL,
    "RevenueAmount" numeric(18,2) NOT NULL DEFAULT 0,
    "MaterialCost" numeric(18,2) NOT NULL DEFAULT 0,
    "LabourCost" numeric(18,2) NOT NULL DEFAULT 0,
    "OverheadAllocated" numeric(18,2) NOT NULL DEFAULT 0,
    "ProfitForOrder" numeric(18,2) NOT NULL DEFAULT 0,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_PnlDetailPerOrders" PRIMARY KEY ("Id")
);

-- OverheadEntries table
CREATE TABLE IF NOT EXISTS "OverheadEntries" (
    "Id" uuid NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CostCentreId" uuid NOT NULL,
    "Period" character varying(20) NOT NULL,
    "Amount" numeric(18,2) NOT NULL DEFAULT 0,
    "Description" character varying(500) NOT NULL,
    "AllocationBasis" character varying(200) NULL,
    "CreatedByUserId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_OverheadEntries" PRIMARY KEY ("Id")
);

-- ============================================
-- DEPARTMENTS MODULE TABLES
-- ============================================

-- Departments table
CREATE TABLE IF NOT EXISTS "Departments" (
    "Id" uuid NOT NULL,
    "CompanyId" uuid NOT NULL,
    "Name" character varying(200) NOT NULL,
    "Code" character varying(50) NULL,
    "Description" character varying(1000) NULL,
    "CostCentreId" uuid NULL,
    "IsActive" boolean NOT NULL DEFAULT true,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_Departments" PRIMARY KEY ("Id")
);

-- MaterialAllocations table
CREATE TABLE IF NOT EXISTS "MaterialAllocations" (
    "Id" uuid NOT NULL,
    "CompanyId" uuid NOT NULL,
    "DepartmentId" uuid NOT NULL,
    "MaterialId" uuid NOT NULL,
    "Quantity" numeric(18,3) NOT NULL DEFAULT 0,
    "Notes" character varying(1000) NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_MaterialAllocations" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_MaterialAllocations_Departments_DepartmentId" FOREIGN KEY ("DepartmentId") REFERENCES "Departments" ("Id") ON DELETE CASCADE
);

-- ============================================
-- INDEXES
-- ============================================

-- Payroll indexes
CREATE INDEX IF NOT EXISTS "IX_PayrollPeriods_CompanyId_Period" ON "PayrollPeriods" ("CompanyId", "Period");
CREATE INDEX IF NOT EXISTS "IX_PayrollPeriods_CompanyId_Status" ON "PayrollPeriods" ("CompanyId", "Status");
CREATE INDEX IF NOT EXISTS "IX_PayrollRuns_CompanyId_PayrollPeriodId" ON "PayrollRuns" ("CompanyId", "PayrollPeriodId");
CREATE INDEX IF NOT EXISTS "IX_PayrollRuns_CompanyId_Status" ON "PayrollRuns" ("CompanyId", "Status");
CREATE INDEX IF NOT EXISTS "IX_PayrollRuns_CompanyId_PeriodStart_PeriodEnd" ON "PayrollRuns" ("CompanyId", "PeriodStart", "PeriodEnd");
CREATE INDEX IF NOT EXISTS "IX_PayrollLines_CompanyId_PayrollRunId" ON "PayrollLines" ("CompanyId", "PayrollRunId");
CREATE INDEX IF NOT EXISTS "IX_PayrollLines_ServiceInstallerId" ON "PayrollLines" ("ServiceInstallerId");
CREATE INDEX IF NOT EXISTS "IX_JobEarningRecords_CompanyId_OrderId" ON "JobEarningRecords" ("CompanyId", "OrderId");
CREATE INDEX IF NOT EXISTS "IX_JobEarningRecords_CompanyId_ServiceInstallerId" ON "JobEarningRecords" ("CompanyId", "ServiceInstallerId");
CREATE INDEX IF NOT EXISTS "IX_JobEarningRecords_CompanyId_Period" ON "JobEarningRecords" ("CompanyId", "Period");
CREATE INDEX IF NOT EXISTS "IX_JobEarningRecords_PayrollRunId" ON "JobEarningRecords" ("PayrollRunId");
CREATE INDEX IF NOT EXISTS "IX_SiRatePlans_CompanyId_ServiceInstallerId_IsActive" ON "SiRatePlans" ("CompanyId", "ServiceInstallerId", "IsActive");

-- P&L indexes
CREATE INDEX IF NOT EXISTS "IX_PnlPeriods_CompanyId_Period" ON "PnlPeriods" ("CompanyId", "Period");
CREATE INDEX IF NOT EXISTS "IX_PnlFacts_CompanyId_Period" ON "PnlFacts" ("CompanyId", "Period");
CREATE INDEX IF NOT EXISTS "IX_PnlFacts_CompanyId_PartnerId" ON "PnlFacts" ("CompanyId", "PartnerId");
CREATE INDEX IF NOT EXISTS "IX_PnlFacts_CompanyId_CostCentreId" ON "PnlFacts" ("CompanyId", "CostCentreId");
CREATE INDEX IF NOT EXISTS "IX_PnlDetailPerOrders_CompanyId_OrderId" ON "PnlDetailPerOrders" ("CompanyId", "OrderId");
CREATE INDEX IF NOT EXISTS "IX_PnlDetailPerOrders_CompanyId_Period" ON "PnlDetailPerOrders" ("CompanyId", "Period");
CREATE INDEX IF NOT EXISTS "IX_PnlDetailPerOrders_PartnerId" ON "PnlDetailPerOrders" ("PartnerId");
CREATE INDEX IF NOT EXISTS "IX_OverheadEntries_CompanyId_CostCentreId" ON "OverheadEntries" ("CompanyId", "CostCentreId");
CREATE INDEX IF NOT EXISTS "IX_OverheadEntries_CompanyId_Period" ON "OverheadEntries" ("CompanyId", "Period");

-- Departments indexes
CREATE INDEX IF NOT EXISTS "IX_Departments_CompanyId_Name" ON "Departments" ("CompanyId", "Name");
CREATE INDEX IF NOT EXISTS "IX_Departments_CompanyId_Code" ON "Departments" ("CompanyId", "Code");
CREATE INDEX IF NOT EXISTS "IX_Departments_CompanyId_IsActive" ON "Departments" ("CompanyId", "IsActive");
CREATE INDEX IF NOT EXISTS "IX_MaterialAllocations_CompanyId_DepartmentId" ON "MaterialAllocations" ("CompanyId", "DepartmentId");
CREATE INDEX IF NOT EXISTS "IX_MaterialAllocations_MaterialId" ON "MaterialAllocations" ("MaterialId");

