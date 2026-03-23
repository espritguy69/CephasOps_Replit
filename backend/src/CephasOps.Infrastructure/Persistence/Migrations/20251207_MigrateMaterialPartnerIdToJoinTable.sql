-- ============================================
-- Migration: Migrate Existing Material PartnerId to MaterialPartners Table
-- Date: 2025-12-07
-- Description: Moves existing PartnerId data from Materials table to MaterialPartners join table
--              for backward compatibility during transition to many-to-many relationship
-- ============================================

-- Migrate existing PartnerId data to MaterialPartners table
-- Only create records for materials that have a PartnerId set and don't already have a MaterialPartner record
INSERT INTO "MaterialPartners" ("Id", "MaterialId", "PartnerId", "CompanyId", "CreatedAt", "UpdatedAt", "IsDeleted")
SELECT 
    gen_random_uuid() AS "Id",
    m."Id" AS "MaterialId",
    m."PartnerId" AS "PartnerId",
    COALESCE(m."CompanyId", '00000000-0000-0000-0000-000000000000'::uuid) AS "CompanyId",
    m."CreatedAt" AS "CreatedAt",
    m."UpdatedAt" AS "UpdatedAt",
    FALSE AS "IsDeleted"
FROM "Materials" m
WHERE m."PartnerId" IS NOT NULL
  AND NOT EXISTS (
      SELECT 1 
      FROM "MaterialPartners" mp 
      WHERE mp."MaterialId" = m."Id" 
        AND mp."PartnerId" = m."PartnerId"
  );

-- Add comment
COMMENT ON TABLE "MaterialPartners" IS 'Join table for many-to-many relationship between Materials and Partners. Migration completed: existing PartnerId data has been migrated to this table.';

