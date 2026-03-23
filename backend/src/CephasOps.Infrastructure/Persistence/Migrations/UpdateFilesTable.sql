-- Update Files Table - Add missing columns
-- Date: 2025-11-23

-- Add missing columns
ALTER TABLE "Files" ADD COLUMN IF NOT EXISTS "Module" character varying(50) NULL;
ALTER TABLE "Files" ADD COLUMN IF NOT EXISTS "EntityId" uuid NULL;
ALTER TABLE "Files" ADD COLUMN IF NOT EXISTS "EntityType" character varying(50) NULL;

-- Create missing index
CREATE INDEX IF NOT EXISTS "IX_Files_CompanyId_EntityId_EntityType" ON "Files" ("CompanyId", "EntityId", "EntityType");

