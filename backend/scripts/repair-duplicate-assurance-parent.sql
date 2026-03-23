-- =============================================================================
-- DIAGNOSTIC: List all top-level ASSURANCE order types and their reference counts
-- Run this first to see which record is canonical vs duplicate.
-- =============================================================================
-- Uncomment and run:
/*
SELECT
  ot."Id",
  ot."CompanyId",
  ot."Name",
  ot."Code",
  ot."ParentOrderTypeId",
  ot."DisplayOrder",
  ot."IsActive",
  ot."CreatedAt",
  ot."IsDeleted",
  (SELECT COUNT(*) FROM "OrderTypes" c WHERE c."ParentOrderTypeId" = ot."Id" AND (c."IsDeleted" IS NOT DISTINCT FROM false)) AS child_subtypes,
  (SELECT COUNT(*) FROM "Orders" o WHERE o."OrderTypeId" = ot."Id") AS orders_refs,
  (SELECT COUNT(*) FROM "JobEarningRecords" j WHERE j."OrderTypeId" = ot."Id") AS job_earning_refs,
  (SELECT COUNT(*) FROM "BuildingDefaultMaterials" b WHERE b."OrderTypeId" = ot."Id") AS building_default_material_refs,
  (SELECT COUNT(*) FROM "BillingRatecards" b WHERE b."OrderTypeId" = ot."Id") AS billing_ratecard_refs,
  (SELECT COUNT(*) FROM "GponPartnerJobRates" g WHERE g."OrderTypeId" = ot."Id") AS gpon_partner_job_rate_refs,
  (SELECT COUNT(*) FROM "GponSiJobRates" g WHERE g."OrderTypeId" = ot."Id") AS gpon_si_job_rate_refs,
  (SELECT COUNT(*) FROM "GponSiCustomRates" g WHERE g."OrderTypeId" = ot."Id") AS gpon_si_custom_rate_refs,
  (SELECT COUNT(*) FROM "ParserTemplates" pt WHERE pt."OrderTypeId" = ot."Id") AS parser_template_refs
FROM "OrderTypes" ot
WHERE ot."Code" = 'ASSURANCE'
  AND ot."ParentOrderTypeId" IS NULL
  AND (ot."IsDeleted" IS NOT DISTINCT FROM false)
ORDER BY ot."CompanyId", ot."CreatedAt";
*/

-- =============================================================================
-- REPAIR: Reassign all references from duplicate ASSURANCE parent(s) to the
-- canonical one, then soft-delete the duplicate(s).
-- Canonical = same company, prefer most child subtypes, then most Orders refs, then oldest CreatedAt.
-- =============================================================================

-- Step 1: Build map (duplicate_id -> canonical_id) for ASSURANCE parents only
CREATE TEMP TABLE IF NOT EXISTS _assurance_dup_map (duplicate_id uuid, canonical_id uuid);
TRUNCATE _assurance_dup_map;

INSERT INTO _assurance_dup_map (duplicate_id, canonical_id)
SELECT p."Id", c.canonical_id
FROM "OrderTypes" p
INNER JOIN (
  SELECT "Id" AS canonical_id, "CompanyId"
  FROM (
    SELECT "Id", "CompanyId", "Code",
      ROW_NUMBER() OVER (
        PARTITION BY "CompanyId"
        ORDER BY (SELECT COUNT(*) FROM "OrderTypes" c WHERE c."ParentOrderTypeId" = "OrderTypes"."Id" AND (c."IsDeleted" IS NOT DISTINCT FROM false)) DESC,
                 (SELECT COUNT(*) FROM "Orders" o WHERE o."OrderTypeId" = "OrderTypes"."Id") DESC,
                 "DisplayOrder" ASC,
                 "CreatedAt" ASC
      ) AS rn
    FROM "OrderTypes"
    WHERE UPPER(TRIM("Code")) = 'ASSURANCE' AND "ParentOrderTypeId" IS NULL AND ("IsDeleted" IS NOT DISTINCT FROM false)
  ) ranked
  WHERE rn = 1
) c ON (p."CompanyId" IS NOT DISTINCT FROM c."CompanyId") AND UPPER(TRIM(p."Code")) = 'ASSURANCE'
  AND p."ParentOrderTypeId" IS NULL AND (p."IsDeleted" IS NOT DISTINCT FROM false) AND p."Id" <> c.canonical_id;

-- Step 2: Reassign all references from duplicate to canonical
UPDATE "OrderTypes" AS child SET "ParentOrderTypeId" = d.canonical_id, "UpdatedAt" = NOW()
FROM _assurance_dup_map d WHERE child."ParentOrderTypeId" = d.duplicate_id;

UPDATE "Orders" o SET "OrderTypeId" = d.canonical_id FROM _assurance_dup_map d WHERE o."OrderTypeId" = d.duplicate_id;
UPDATE "JobEarningRecords" j SET "OrderTypeId" = d.canonical_id FROM _assurance_dup_map d WHERE j."OrderTypeId" = d.duplicate_id;
UPDATE "BuildingDefaultMaterials" b SET "OrderTypeId" = d.canonical_id FROM _assurance_dup_map d WHERE b."OrderTypeId" = d.duplicate_id;
UPDATE "BillingRatecards" b SET "OrderTypeId" = d.canonical_id FROM _assurance_dup_map d WHERE b."OrderTypeId" = d.duplicate_id;
UPDATE "GponPartnerJobRates" g SET "OrderTypeId" = d.canonical_id FROM _assurance_dup_map d WHERE g."OrderTypeId" = d.duplicate_id;
UPDATE "GponSiJobRates" g SET "OrderTypeId" = d.canonical_id FROM _assurance_dup_map d WHERE g."OrderTypeId" = d.duplicate_id;
UPDATE "GponSiCustomRates" g SET "OrderTypeId" = d.canonical_id FROM _assurance_dup_map d WHERE g."OrderTypeId" = d.duplicate_id;
UPDATE "ParserTemplates" pt SET "OrderTypeId" = d.canonical_id FROM _assurance_dup_map d WHERE pt."OrderTypeId" = d.duplicate_id;

-- Step 3: Hard-delete duplicate Assurance parent(s) (after reassigning refs in step 2)
DELETE FROM "OrderTypes" ot USING _assurance_dup_map d WHERE ot."Id" = d.duplicate_id;

-- Step 4: Hard-delete any other top-level ASSURANCE that has zero refs and zero children
-- (e.g. duplicate in a different CompanyId partition that was never referenced)
DELETE FROM "OrderTypes" ot
WHERE ot."Code" = 'ASSURANCE' AND ot."ParentOrderTypeId" IS NULL AND (ot."IsDeleted" IS NOT DISTINCT FROM false)
  AND (SELECT COUNT(*) FROM "OrderTypes" c WHERE c."ParentOrderTypeId" = ot."Id" AND (c."IsDeleted" IS NOT DISTINCT FROM false)) = 0
  AND (SELECT COUNT(*) FROM "Orders" o WHERE o."OrderTypeId" = ot."Id") = 0
  AND (SELECT COUNT(*) FROM "JobEarningRecords" j WHERE j."OrderTypeId" = ot."Id") = 0
  AND (SELECT COUNT(*) FROM "BuildingDefaultMaterials" b WHERE b."OrderTypeId" = ot."Id") = 0
  AND (SELECT COUNT(*) FROM "BillingRatecards" b WHERE b."OrderTypeId" = ot."Id") = 0
  AND (SELECT COUNT(*) FROM "GponPartnerJobRates" g WHERE g."OrderTypeId" = ot."Id") = 0
  AND (SELECT COUNT(*) FROM "GponSiJobRates" g WHERE g."OrderTypeId" = ot."Id") = 0
  AND (SELECT COUNT(*) FROM "GponSiCustomRates" g WHERE g."OrderTypeId" = ot."Id") = 0
  AND (SELECT COUNT(*) FROM "ParserTemplates" pt WHERE pt."OrderTypeId" = ot."Id") = 0
  AND EXISTS (SELECT 1 FROM "OrderTypes" o2 WHERE o2."Code" = 'ASSURANCE' AND o2."ParentOrderTypeId" IS NULL AND (o2."IsDeleted" IS NOT DISTINCT FROM false) AND o2."Id" <> ot."Id");

-- Show what was repaired (run after the above)
SELECT 'Reassigned and soft-deleted' AS action, duplicate_id, canonical_id FROM _assurance_dup_map;
