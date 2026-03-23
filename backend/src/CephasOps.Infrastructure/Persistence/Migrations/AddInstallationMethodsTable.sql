-- Migration: Add InstallationMethods table
-- Date: 2024-01-XX
-- Description: Creates the InstallationMethods table to replace BuildingTypes for installation method classification

-- Create InstallationMethods table
CREATE TABLE IF NOT EXISTS "InstallationMethods" (
    "Id" UUID PRIMARY KEY,
    "CompanyId" UUID NULL,
    "Name" VARCHAR(100) NOT NULL,
    "Code" VARCHAR(50) NOT NULL,
    "Category" VARCHAR(50) NULL,
    "Description" VARCHAR(1000) NULL,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "DisplayOrder" INTEGER NOT NULL DEFAULT 0,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NULL
);

-- Create indexes
CREATE UNIQUE INDEX IF NOT EXISTS "IX_InstallationMethods_CompanyId_Code" 
    ON "InstallationMethods" ("CompanyId", "Code");

CREATE INDEX IF NOT EXISTS "IX_InstallationMethods_CompanyId_IsActive" 
    ON "InstallationMethods" ("CompanyId", "IsActive");

CREATE INDEX IF NOT EXISTS "IX_InstallationMethods_CompanyId_Category" 
    ON "InstallationMethods" ("CompanyId", "Category");

CREATE INDEX IF NOT EXISTS "IX_InstallationMethods_DisplayOrder" 
    ON "InstallationMethods" ("DisplayOrder");

-- Add InstallationMethodId column to Buildings table
ALTER TABLE "Buildings" ADD COLUMN IF NOT EXISTS "InstallationMethodId" UUID NULL;
ALTER TABLE "Buildings" ADD COLUMN IF NOT EXISTS "PropertyType" VARCHAR(50) NULL;
ALTER TABLE "Buildings" ADD COLUMN IF NOT EXISTS "Area" VARCHAR(200) NULL;
ALTER TABLE "Buildings" ADD COLUMN IF NOT EXISTS "RfbAssignedDate" TIMESTAMP WITH TIME ZONE NULL;
ALTER TABLE "Buildings" ADD COLUMN IF NOT EXISTS "FirstOrderDate" TIMESTAMP WITH TIME ZONE NULL;
ALTER TABLE "Buildings" ADD COLUMN IF NOT EXISTS "Notes" TEXT NULL;
ALTER TABLE "Buildings" ADD COLUMN IF NOT EXISTS "UpdatedAt" TIMESTAMP WITH TIME ZONE NULL;

-- Add CreatedAt if it doesn't exist
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'Buildings' AND column_name = 'CreatedAt'
    ) THEN
        ALTER TABLE "Buildings" ADD COLUMN "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP;
    END IF;
END $$;

-- Create foreign key constraint (idempotent)
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.table_constraints 
        WHERE constraint_name = 'FK_Buildings_InstallationMethods_InstallationMethodId'
        AND table_name = 'Buildings'
    ) THEN
        ALTER TABLE "Buildings" 
            ADD CONSTRAINT "FK_Buildings_InstallationMethods_InstallationMethodId" 
            FOREIGN KEY ("InstallationMethodId") 
            REFERENCES "InstallationMethods" ("Id") 
            ON DELETE SET NULL;
    END IF;
END $$;

-- Create index on InstallationMethodId
CREATE INDEX IF NOT EXISTS "IX_Buildings_InstallationMethodId" 
    ON "Buildings" ("InstallationMethodId");

-- Seed default installation methods
INSERT INTO "InstallationMethods" ("Id", "CompanyId", "Name", "Code", "Category", "Description", "IsActive", "DisplayOrder", "CreatedAt")
VALUES 
    (
        gen_random_uuid(), 
        NULL, 
        'Prelaid', 
        'PRELAID', 
        'FTTH',
        'Fibre already laid by building builder/management. We mainly tap into existing infrastructure. Minimal materials needed (patch cord, indoor drop, faceplate, small trunking).',
        TRUE,
        1,
        CURRENT_TIMESTAMP
    ),
    (
        gen_random_uuid(), 
        NULL, 
        'Non-prelaid (MDU / old building)', 
        'NON_PRELAID', 
        'FTTH',
        'Multi-dwelling units and old buildings. We must build the fibre infrastructure (riser, trunking, backbone). Full infrastructure pack required.',
        TRUE,
        2,
        CURRENT_TIMESTAMP
    ),
    (
        gen_random_uuid(), 
        NULL, 
        'SDU / RDF Pole', 
        'SDU_RDF', 
        'FTTH',
        'Single dwelling units and pole-based installations. Pole accessories, aerial cable, termination box, and basic house kit required.',
        TRUE,
        3,
        CURRENT_TIMESTAMP
    )
ON CONFLICT DO NOTHING;

-- Optional: Migrate existing BuildingType data to InstallationMethods
-- This copies any existing building types that match installation method patterns
INSERT INTO "InstallationMethods" ("Id", "CompanyId", "Name", "Code", "Category", "Description", "IsActive", "DisplayOrder", "CreatedAt")
SELECT 
    bt."Id",
    bt."CompanyId",
    bt."Name",
    bt."Code",
    CASE 
        WHEN bt."Name" ILIKE '%pole%' OR bt."Code" ILIKE '%SDU%' THEN 'FTTH'
        WHEN bt."Name" ILIKE '%prelaid%' THEN 'FTTH'
        ELSE 'FTTH'
    END as "Category",
    bt."Description",
    bt."IsActive",
    bt."DisplayOrder",
    COALESCE(bt."CreatedAt", CURRENT_TIMESTAMP)
FROM "BuildingTypes" bt
WHERE NOT EXISTS (
    SELECT 1 FROM "InstallationMethods" im 
    WHERE im."Code" = bt."Code" AND (im."CompanyId" = bt."CompanyId" OR (im."CompanyId" IS NULL AND bt."CompanyId" IS NULL))
)
ON CONFLICT DO NOTHING;

-- ============================================
-- Building Contacts table
-- ============================================
CREATE TABLE IF NOT EXISTS "BuildingContacts" (
    "Id" UUID PRIMARY KEY,
    "BuildingId" UUID NOT NULL REFERENCES "Buildings"("Id") ON DELETE CASCADE,
    "Role" VARCHAR(100) NOT NULL,
    "Name" VARCHAR(200) NOT NULL,
    "Phone" VARCHAR(50) NULL,
    "Email" VARCHAR(200) NULL,
    "Remarks" VARCHAR(1000) NULL,
    "IsPrimary" BOOLEAN NOT NULL DEFAULT FALSE,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NULL
);

CREATE INDEX IF NOT EXISTS "IX_BuildingContacts_BuildingId" ON "BuildingContacts" ("BuildingId");
CREATE INDEX IF NOT EXISTS "IX_BuildingContacts_BuildingId_Role" ON "BuildingContacts" ("BuildingId", "Role");
CREATE INDEX IF NOT EXISTS "IX_BuildingContacts_BuildingId_IsActive" ON "BuildingContacts" ("BuildingId", "IsActive");

-- ============================================
-- Building Rules table
-- ============================================
CREATE TABLE IF NOT EXISTS "BuildingRules" (
    "Id" UUID PRIMARY KEY,
    "BuildingId" UUID NOT NULL REFERENCES "Buildings"("Id") ON DELETE CASCADE,
    "AccessRules" TEXT NULL,
    "InstallationRules" TEXT NULL,
    "OtherNotes" TEXT NULL,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NULL
);

-- Note: Unique index on BuildingId is created by EF Core via BuildingRulesConfiguration
-- No explicit index creation needed here to avoid duplicates

-- ============================================
-- Building Blocks table (for MDU)
-- ============================================
CREATE TABLE IF NOT EXISTS "BuildingBlocks" (
    "Id" UUID PRIMARY KEY,
    "BuildingId" UUID NOT NULL REFERENCES "Buildings"("Id") ON DELETE CASCADE,
    "Name" VARCHAR(100) NOT NULL,
    "Code" VARCHAR(50) NULL,
    "Floors" INTEGER NOT NULL DEFAULT 1,
    "UnitsPerFloor" INTEGER NULL,
    "TotalUnits" INTEGER NULL,
    "DisplayOrder" INTEGER NOT NULL DEFAULT 0,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "Notes" VARCHAR(1000) NULL,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NULL
);

CREATE INDEX IF NOT EXISTS "IX_BuildingBlocks_BuildingId" ON "BuildingBlocks" ("BuildingId");
CREATE INDEX IF NOT EXISTS "IX_BuildingBlocks_BuildingId_Name" ON "BuildingBlocks" ("BuildingId", "Name");
CREATE INDEX IF NOT EXISTS "IX_BuildingBlocks_BuildingId_DisplayOrder" ON "BuildingBlocks" ("BuildingId", "DisplayOrder");

-- ============================================
-- Building Splitters table
-- ============================================
CREATE TABLE IF NOT EXISTS "BuildingSplitters" (
    "Id" UUID PRIMARY KEY,
    "BuildingId" UUID NOT NULL REFERENCES "Buildings"("Id") ON DELETE CASCADE,
    "BlockId" UUID NULL REFERENCES "BuildingBlocks"("Id") ON DELETE SET NULL,
    "SplitterTypeId" UUID NOT NULL,
    "Name" VARCHAR(100) NOT NULL,
    "Floor" INTEGER NULL,
    "LocationDescription" VARCHAR(500) NULL,
    "PortsTotal" INTEGER NOT NULL DEFAULT 8,
    "PortsUsed" INTEGER NOT NULL DEFAULT 0,
    "Status" VARCHAR(50) NOT NULL DEFAULT 'Active',
    "SerialNumber" VARCHAR(100) NULL,
    "InstalledAt" TIMESTAMP WITH TIME ZONE NULL,
    "LastMaintenanceAt" TIMESTAMP WITH TIME ZONE NULL,
    "Remarks" VARCHAR(1000) NULL,
    "NeedsAttention" BOOLEAN NOT NULL DEFAULT FALSE,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NULL
);

CREATE INDEX IF NOT EXISTS "IX_BuildingSplitters_BuildingId" ON "BuildingSplitters" ("BuildingId");
CREATE INDEX IF NOT EXISTS "IX_BuildingSplitters_BlockId" ON "BuildingSplitters" ("BlockId");
CREATE INDEX IF NOT EXISTS "IX_BuildingSplitters_SplitterTypeId" ON "BuildingSplitters" ("SplitterTypeId");
CREATE INDEX IF NOT EXISTS "IX_BuildingSplitters_Status" ON "BuildingSplitters" ("Status");
CREATE INDEX IF NOT EXISTS "IX_BuildingSplitters_BuildingId_Status" ON "BuildingSplitters" ("BuildingId", "Status");
CREATE INDEX IF NOT EXISTS "IX_BuildingSplitters_BuildingId_NeedsAttention" ON "BuildingSplitters" ("BuildingId", "NeedsAttention");

-- ============================================
-- Streets table (for Landed/SDU)
-- ============================================
CREATE TABLE IF NOT EXISTS "Streets" (
    "Id" UUID PRIMARY KEY,
    "BuildingId" UUID NOT NULL REFERENCES "Buildings"("Id") ON DELETE CASCADE,
    "Name" VARCHAR(200) NOT NULL,
    "Code" VARCHAR(50) NULL,
    "Section" VARCHAR(100) NULL,
    "DisplayOrder" INTEGER NOT NULL DEFAULT 0,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NULL
);

CREATE INDEX IF NOT EXISTS "IX_Streets_BuildingId" ON "Streets" ("BuildingId");
CREATE INDEX IF NOT EXISTS "IX_Streets_BuildingId_Name" ON "Streets" ("BuildingId", "Name");
CREATE INDEX IF NOT EXISTS "IX_Streets_BuildingId_DisplayOrder" ON "Streets" ("BuildingId", "DisplayOrder");

-- ============================================
-- Hub Boxes table (for Landed/SDU)
-- ============================================
CREATE TABLE IF NOT EXISTS "HubBoxes" (
    "Id" UUID PRIMARY KEY,
    "BuildingId" UUID NOT NULL REFERENCES "Buildings"("Id") ON DELETE CASCADE,
    "StreetId" UUID NULL REFERENCES "Streets"("Id") ON DELETE SET NULL,
    "Name" VARCHAR(100) NOT NULL,
    "Code" VARCHAR(50) NULL,
    "LocationDescription" VARCHAR(500) NULL,
    "Latitude" DECIMAL(10, 7) NULL,
    "Longitude" DECIMAL(10, 7) NULL,
    "PortsTotal" INTEGER NOT NULL DEFAULT 8,
    "PortsUsed" INTEGER NOT NULL DEFAULT 0,
    "Status" VARCHAR(50) NOT NULL DEFAULT 'Active',
    "InstalledAt" TIMESTAMP WITH TIME ZONE NULL,
    "Remarks" VARCHAR(1000) NULL,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NULL
);

CREATE INDEX IF NOT EXISTS "IX_HubBoxes_BuildingId" ON "HubBoxes" ("BuildingId");
CREATE INDEX IF NOT EXISTS "IX_HubBoxes_StreetId" ON "HubBoxes" ("StreetId");
CREATE INDEX IF NOT EXISTS "IX_HubBoxes_Status" ON "HubBoxes" ("Status");
CREATE INDEX IF NOT EXISTS "IX_HubBoxes_BuildingId_Status" ON "HubBoxes" ("BuildingId", "Status");

-- ============================================
-- Poles table (for Landed/SDU)
-- ============================================
CREATE TABLE IF NOT EXISTS "Poles" (
    "Id" UUID PRIMARY KEY,
    "BuildingId" UUID NOT NULL REFERENCES "Buildings"("Id") ON DELETE CASCADE,
    "StreetId" UUID NULL REFERENCES "Streets"("Id") ON DELETE SET NULL,
    "PoleNumber" VARCHAR(50) NOT NULL,
    "PoleType" VARCHAR(50) NULL,
    "LocationDescription" VARCHAR(500) NULL,
    "Latitude" DECIMAL(10, 7) NULL,
    "Longitude" DECIMAL(10, 7) NULL,
    "HeightMeters" DECIMAL(5, 2) NULL,
    "HasExistingFibre" BOOLEAN NOT NULL DEFAULT FALSE,
    "DropsCount" INTEGER NOT NULL DEFAULT 0,
    "Status" VARCHAR(50) NOT NULL DEFAULT 'Good',
    "Remarks" VARCHAR(1000) NULL,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "LastInspectedAt" TIMESTAMP WITH TIME ZONE NULL,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NULL
);

CREATE INDEX IF NOT EXISTS "IX_Poles_BuildingId" ON "Poles" ("BuildingId");
CREATE INDEX IF NOT EXISTS "IX_Poles_StreetId" ON "Poles" ("StreetId");
CREATE INDEX IF NOT EXISTS "IX_Poles_Status" ON "Poles" ("Status");
CREATE INDEX IF NOT EXISTS "IX_Poles_BuildingId_PoleNumber" ON "Poles" ("BuildingId", "PoleNumber");

