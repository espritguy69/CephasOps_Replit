-- ============================================
-- Migration: Add BuildingDefaultMaterials table
-- Date: 2024-11-27
-- Description: Adds table for storing default materials per building + job type
-- ============================================

-- Create BuildingDefaultMaterials table
CREATE TABLE IF NOT EXISTS "BuildingDefaultMaterials" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "BuildingId" UUID NOT NULL,
    "OrderTypeId" UUID NOT NULL,
    "MaterialId" UUID NOT NULL,
    "DefaultQuantity" DECIMAL(18,4) NOT NULL DEFAULT 1,
    "Notes" TEXT,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMP WITH TIME ZONE,
    CONSTRAINT "FK_BuildingDefaultMaterials_Buildings" FOREIGN KEY ("BuildingId") 
        REFERENCES "Buildings"("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_BuildingDefaultMaterials_OrderTypes" FOREIGN KEY ("OrderTypeId") 
        REFERENCES "OrderTypes"("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_BuildingDefaultMaterials_Materials" FOREIGN KEY ("MaterialId") 
        REFERENCES "Materials"("Id") ON DELETE RESTRICT,
    CONSTRAINT "UQ_BuildingDefaultMaterials_Building_OrderType_Material" 
        UNIQUE ("BuildingId", "OrderTypeId", "MaterialId")
);

-- Create indexes for performance
CREATE INDEX IF NOT EXISTS "IX_BuildingDefaultMaterials_BuildingId" 
    ON "BuildingDefaultMaterials" ("BuildingId");

CREATE INDEX IF NOT EXISTS "IX_BuildingDefaultMaterials_OrderTypeId" 
    ON "BuildingDefaultMaterials" ("OrderTypeId");

CREATE INDEX IF NOT EXISTS "IX_BuildingDefaultMaterials_MaterialId" 
    ON "BuildingDefaultMaterials" ("MaterialId");

CREATE INDEX IF NOT EXISTS "IX_BuildingDefaultMaterials_BuildingId_OrderTypeId" 
    ON "BuildingDefaultMaterials" ("BuildingId", "OrderTypeId") 
    WHERE "IsActive" = TRUE;

-- Add comments
COMMENT ON TABLE "BuildingDefaultMaterials" IS 'Stores default materials for each building per job type. These materials are auto-applied when orders are created.';
COMMENT ON COLUMN "BuildingDefaultMaterials"."BuildingId" IS 'FK to Buildings table';
COMMENT ON COLUMN "BuildingDefaultMaterials"."OrderTypeId" IS 'FK to OrderTypes (Job Types) table - e.g., Activation, Modification Outdoor';
COMMENT ON COLUMN "BuildingDefaultMaterials"."MaterialId" IS 'FK to Materials table - must be non-serialized materials only';
COMMENT ON COLUMN "BuildingDefaultMaterials"."DefaultQuantity" IS 'Default quantity to apply to orders';
COMMENT ON COLUMN "BuildingDefaultMaterials"."IsActive" IS 'Whether this default material is active';

