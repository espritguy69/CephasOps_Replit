-- Fix: deduplicate subtypes and create the second unique index (parent dedupe and first index already applied).
CREATE TEMP TABLE IF NOT EXISTS _subtype_dup_map (duplicate_id uuid, canonical_id uuid);
TRUNCATE _subtype_dup_map;
INSERT INTO _subtype_dup_map (duplicate_id, canonical_id)
SELECT s."Id", c.canonical_id
FROM "OrderTypes" s
INNER JOIN (
  SELECT "Id" AS canonical_id, "CompanyId", "ParentOrderTypeId", UPPER(TRIM("Code")) AS code_key
  FROM (
    SELECT "Id", "CompanyId", "ParentOrderTypeId", "Code",
      ROW_NUMBER() OVER (
        PARTITION BY "CompanyId", "ParentOrderTypeId", UPPER(TRIM("Code"))
        ORDER BY "DisplayOrder" ASC, "CreatedAt" ASC
      ) AS rn
    FROM "OrderTypes"
    WHERE "ParentOrderTypeId" IS NOT NULL AND ("IsDeleted" IS NOT DISTINCT FROM false)
  ) ranked
  WHERE rn = 1
) c ON (s."CompanyId" IS NOT DISTINCT FROM c."CompanyId") AND s."ParentOrderTypeId" = c."ParentOrderTypeId" AND UPPER(TRIM(s."Code")) = c.code_key
  AND ("IsDeleted" IS NOT DISTINCT FROM false) AND s."Id" <> c.canonical_id;

UPDATE "Orders" o SET "OrderTypeId" = d.canonical_id FROM _subtype_dup_map d WHERE o."OrderTypeId" = d.duplicate_id;
UPDATE "JobEarningRecords" j SET "OrderTypeId" = d.canonical_id FROM _subtype_dup_map d WHERE j."OrderTypeId" = d.duplicate_id;
UPDATE "BuildingDefaultMaterials" b SET "OrderTypeId" = d.canonical_id FROM _subtype_dup_map d WHERE b."OrderTypeId" = d.duplicate_id;
UPDATE "BillingRatecards" b SET "OrderTypeId" = d.canonical_id FROM _subtype_dup_map d WHERE b."OrderTypeId" = d.duplicate_id;
UPDATE "GponPartnerJobRates" g SET "OrderTypeId" = d.canonical_id FROM _subtype_dup_map d WHERE g."OrderTypeId" = d.duplicate_id;
UPDATE "GponSiJobRates" g SET "OrderTypeId" = d.canonical_id FROM _subtype_dup_map d WHERE g."OrderTypeId" = d.duplicate_id;
UPDATE "GponSiCustomRates" g SET "OrderTypeId" = d.canonical_id FROM _subtype_dup_map d WHERE g."OrderTypeId" = d.duplicate_id;
UPDATE "ParserTemplates" pt SET "OrderTypeId" = d.canonical_id FROM _subtype_dup_map d WHERE pt."OrderTypeId" = d.duplicate_id;
UPDATE "OrderTypes" ot SET "IsDeleted" = true, "DeletedAt" = NOW(), "UpdatedAt" = NOW()
FROM _subtype_dup_map d WHERE ot."Id" = d.duplicate_id;

CREATE UNIQUE INDEX IF NOT EXISTS "IX_OrderTypes_CompanyId_ParentId_Code_Subtypes"
ON "OrderTypes" ("CompanyId", "ParentOrderTypeId", "Code")
WHERE "ParentOrderTypeId" IS NOT NULL AND ("IsDeleted" IS NOT DISTINCT FROM false);
