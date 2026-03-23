-- ============================================
-- Normalize BuildingTypeId from legacy PropertyType
-- ============================================
-- Idempotent. Sets Buildings.BuildingTypeId from BuildingTypes where
-- BuildingTypes.Name matches Buildings.PropertyType (trimmed, case-insensitive).
-- Run after BuildingTypes are seeded (e.g. via DatabaseSeeder or 20260106014834_SeedAllReferenceData).
-- See: docs/06_ai/BUILDING_TYPE_FIX_IMPLEMENTATION.md
-- ============================================

DO $$
DECLARE
    v_updated INT;
BEGIN
    UPDATE "Buildings" b
    SET "BuildingTypeId" = bt."Id",
        "UpdatedAt"      = CURRENT_TIMESTAMP
    FROM "BuildingTypes" bt
    WHERE b."CompanyId" = bt."CompanyId"
      AND b."BuildingTypeId" IS NULL
      AND b."PropertyType" IS NOT NULL
      AND TRIM(b."PropertyType") <> ''
      AND LOWER(TRIM(bt."Name")) = LOWER(TRIM(b."PropertyType"));

    GET DIAGNOSTICS v_updated = ROW_COUNT;
    RAISE NOTICE 'Normalized BuildingTypeId for % building(s) from PropertyType.', v_updated;
END $$;
