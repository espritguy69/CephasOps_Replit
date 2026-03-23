-- Add BuildingName and BuildingStatus columns to ParsedOrderDrafts
ALTER TABLE "ParsedOrderDrafts" ADD COLUMN IF NOT EXISTS "BuildingName" character varying(500);
ALTER TABLE "ParsedOrderDrafts" ADD COLUMN IF NOT EXISTS "BuildingStatus" character varying(50);

-- Mark migration as applied
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
SELECT '20251203082425_AddBuildingNameAndStatusToOrderDraft', '10.0.0'
WHERE NOT EXISTS (
    SELECT 1 FROM "__EFMigrationsHistory"
    WHERE "MigrationId" = '20251203082425_AddBuildingNameAndStatusToOrderDraft'
);

