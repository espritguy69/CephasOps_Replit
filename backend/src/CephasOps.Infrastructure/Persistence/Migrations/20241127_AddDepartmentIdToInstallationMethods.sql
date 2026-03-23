-- Migration: Add DepartmentId column to InstallationMethods table
-- Date: 2024-11-27
-- Description: Adds the DepartmentId column that was missing from the original table creation

-- Add DepartmentId column if it doesn't exist
DO $$ 
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'InstallationMethods' AND column_name = 'DepartmentId'
    ) THEN
        ALTER TABLE "InstallationMethods" ADD COLUMN "DepartmentId" UUID NULL;
        
        -- Add foreign key constraint
        ALTER TABLE "InstallationMethods" 
            ADD CONSTRAINT "FK_InstallationMethods_Departments_DepartmentId" 
            FOREIGN KEY ("DepartmentId") 
            REFERENCES "Departments" ("Id") 
            ON DELETE SET NULL;
            
        -- Add index for performance
        CREATE INDEX IF NOT EXISTS "IX_InstallationMethods_DepartmentId" 
            ON "InstallationMethods" ("DepartmentId");
    END IF;
END $$;

-- Add Category column if it doesn't exist
DO $$ 
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'InstallationMethods' AND column_name = 'Category'
    ) THEN
        ALTER TABLE "InstallationMethods" ADD COLUMN "Category" VARCHAR(50) NULL;
    END IF;
END $$;

-- Add UpdatedAt column if it doesn't exist
DO $$ 
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'InstallationMethods' AND column_name = 'UpdatedAt'
    ) THEN
        ALTER TABLE "InstallationMethods" ADD COLUMN "UpdatedAt" TIMESTAMP WITH TIME ZONE NULL;
    END IF;
END $$;

-- Seed default installation methods if the table is empty
INSERT INTO "InstallationMethods" ("Id", "CompanyId", "DepartmentId", "Name", "Code", "Category", "Description", "IsActive", "DisplayOrder", "CreatedAt")
SELECT 
    gen_random_uuid(), 
    NULL, 
    NULL,
    'Prelaid', 
    'PRELAID', 
    'FTTH',
    'Fibre already laid by building builder/management. We mainly tap into existing infrastructure. Minimal materials needed (patch cord, indoor drop, faceplate).',
    TRUE,
    1,
    CURRENT_TIMESTAMP
WHERE NOT EXISTS (SELECT 1 FROM "InstallationMethods" WHERE "Code" = 'PRELAID');

INSERT INTO "InstallationMethods" ("Id", "CompanyId", "DepartmentId", "Name", "Code", "Category", "Description", "IsActive", "DisplayOrder", "CreatedAt")
SELECT 
    gen_random_uuid(), 
    NULL, 
    NULL,
    'Non-prelaid (MDU / old building)', 
    'NON_PRELAID', 
    'FTTH',
    'Multi-dwelling units and old buildings. We must build the fibre infrastructure (riser, trunking, backbone). Full infra pack required.',
    TRUE,
    2,
    CURRENT_TIMESTAMP
WHERE NOT EXISTS (SELECT 1 FROM "InstallationMethods" WHERE "Code" = 'NON_PRELAID');

INSERT INTO "InstallationMethods" ("Id", "CompanyId", "DepartmentId", "Name", "Code", "Category", "Description", "IsActive", "DisplayOrder", "CreatedAt")
SELECT 
    gen_random_uuid(), 
    NULL, 
    NULL,
    'SDU / RDF Pole', 
    'SDU_RDF', 
    'FTTH',
    'Single dwelling units and pole-based installations. Pole accessories, aerial cable, termination box, and basic house kit required.',
    TRUE,
    3,
    CURRENT_TIMESTAMP
WHERE NOT EXISTS (SELECT 1 FROM "InstallationMethods" WHERE "Code" = 'SDU_RDF');

