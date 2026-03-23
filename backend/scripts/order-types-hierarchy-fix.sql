-- Order Types hierarchy data fix
-- Run this if migrations cannot be applied (e.g. API running or pending model changes).
-- Safe to run multiple times. Only updates ParentOrderTypeId and names; Order IDs unchanged.
-- Usage: psql -h localhost -p 5432 -d cephasops -U postgres -f order-types-hierarchy-fix.sql

BEGIN;

-- (1) Ensure parent order types exist (idempotent; from FixOrderTypesHierarchyData)
INSERT INTO "OrderTypes" ("Id", "CompanyId", "DepartmentId", "ParentOrderTypeId", "Name", "Code", "Description", "DisplayOrder", "IsActive", "CreatedAt", "UpdatedAt", "IsDeleted")
SELECT gen_random_uuid(), d."CompanyId", d."DepartmentId", NULL, 'Activation', 'ACTIVATION', 'New installation + activation of service', 1, true, NOW(), NOW(), false
FROM (SELECT DISTINCT ON (COALESCE("CompanyId"::text, 'x')) "CompanyId", "DepartmentId" FROM "OrderTypes" LIMIT 500) d
WHERE NOT EXISTS (SELECT 1 FROM "OrderTypes" p WHERE p."Code" = 'ACTIVATION' AND p."ParentOrderTypeId" IS NULL AND (p."CompanyId" IS NOT DISTINCT FROM d."CompanyId"));

INSERT INTO "OrderTypes" ("Id", "CompanyId", "DepartmentId", "ParentOrderTypeId", "Name", "Code", "Description", "DisplayOrder", "IsActive", "CreatedAt", "UpdatedAt", "IsDeleted")
SELECT gen_random_uuid(), d."CompanyId", d."DepartmentId", NULL, 'Modification', 'MODIFICATION', 'Modification of existing service', 2, true, NOW(), NOW(), false
FROM (SELECT DISTINCT ON (COALESCE("CompanyId"::text, 'x')) "CompanyId", "DepartmentId" FROM "OrderTypes" LIMIT 500) d
WHERE NOT EXISTS (SELECT 1 FROM "OrderTypes" p WHERE p."Code" = 'MODIFICATION' AND p."ParentOrderTypeId" IS NULL AND (p."CompanyId" IS NOT DISTINCT FROM d."CompanyId"));

INSERT INTO "OrderTypes" ("Id", "CompanyId", "DepartmentId", "ParentOrderTypeId", "Name", "Code", "Description", "DisplayOrder", "IsActive", "CreatedAt", "UpdatedAt", "IsDeleted")
SELECT gen_random_uuid(), d."CompanyId", d."DepartmentId", NULL, 'Assurance', 'ASSURANCE', 'Fault repair and troubleshooting', 3, true, NOW(), NOW(), false
FROM (SELECT DISTINCT ON (COALESCE("CompanyId"::text, 'x')) "CompanyId", "DepartmentId" FROM "OrderTypes" LIMIT 500) d
WHERE NOT EXISTS (SELECT 1 FROM "OrderTypes" p WHERE p."Code" = 'ASSURANCE' AND p."ParentOrderTypeId" IS NULL AND (p."CompanyId" IS NOT DISTINCT FROM d."CompanyId"));

INSERT INTO "OrderTypes" ("Id", "CompanyId", "DepartmentId", "ParentOrderTypeId", "Name", "Code", "Description", "DisplayOrder", "IsActive", "CreatedAt", "UpdatedAt", "IsDeleted")
SELECT gen_random_uuid(), d."CompanyId", d."DepartmentId", NULL, 'Value Added Service', 'VALUE_ADDED_SERVICE', 'Additional services beyond standard installation/repair', 4, true, NOW(), NOW(), false
FROM (SELECT DISTINCT ON (COALESCE("CompanyId"::text, 'x')) "CompanyId", "DepartmentId" FROM "OrderTypes" LIMIT 500) d
WHERE NOT EXISTS (SELECT 1 FROM "OrderTypes" p WHERE p."Code" = 'VALUE_ADDED_SERVICE' AND p."ParentOrderTypeId" IS NULL AND (p."CompanyId" IS NOT DISTINCT FROM d."CompanyId"));

-- (2) Link legacy codes to parents (FixOrderTypesHierarchyData)
UPDATE "OrderTypes" AS child
SET "ParentOrderTypeId" = parent."Id", "UpdatedAt" = NOW()
FROM "OrderTypes" AS parent
WHERE parent."Code" = 'MODIFICATION' AND parent."ParentOrderTypeId" IS NULL
  AND (parent."CompanyId" IS NOT DISTINCT FROM child."CompanyId")
  AND child."Code" IN ('MODIFICATION_INDOOR', 'MODIFICATION_OUTDOOR') AND child."ParentOrderTypeId" IS NULL;

UPDATE "OrderTypes" AS child
SET "ParentOrderTypeId" = parent."Id", "UpdatedAt" = NOW()
FROM "OrderTypes" AS parent
WHERE parent."Code" = 'ASSURANCE' AND parent."ParentOrderTypeId" IS NULL
  AND (parent."CompanyId" IS NOT DISTINCT FROM child."CompanyId")
  AND child."Code" IN ('STANDARD', 'REPULL') AND child."ParentOrderTypeId" IS NULL;

UPDATE "OrderTypes" AS child
SET "ParentOrderTypeId" = parent."Id", "UpdatedAt" = NOW()
FROM "OrderTypes" AS parent
WHERE parent."Code" = 'VALUE_ADDED_SERVICE' AND parent."ParentOrderTypeId" IS NULL
  AND (parent."CompanyId" IS NOT DISTINCT FROM child."CompanyId")
  AND child."Code" IN ('UPGRADE', 'IAD', 'FIXED_IP') AND child."ParentOrderTypeId" IS NULL;

-- (3) Link alternate codes (FixOrderTypesHierarchyDataAlternateCodes) - case-insensitive
UPDATE "OrderTypes" AS child
SET "ParentOrderTypeId" = parent."Id", "UpdatedAt" = NOW()
FROM "OrderTypes" AS parent
WHERE parent."Code" = 'MODIFICATION' AND parent."ParentOrderTypeId" IS NULL
  AND (parent."CompanyId" IS NOT DISTINCT FROM child."CompanyId")
  AND child."ParentOrderTypeId" IS NULL
  AND UPPER(TRIM(child."Code")) IN ('INDOOR', 'OUTDOOR', 'MODIFICATION_INDOOR', 'MODIFICATION_OUTDOOR', 'MODIFICATION INDOOR', 'MODIFICATION OUTDOOR');

UPDATE "OrderTypes" AS child
SET "ParentOrderTypeId" = parent."Id", "UpdatedAt" = NOW()
FROM "OrderTypes" AS parent
WHERE parent."Code" = 'ASSURANCE' AND parent."ParentOrderTypeId" IS NULL
  AND (parent."CompanyId" IS NOT DISTINCT FROM child."CompanyId")
  AND child."ParentOrderTypeId" IS NULL
  AND UPPER(TRIM(child."Code")) IN ('ASSURANCE-REPULL', 'REPULL', 'STANDARD');

UPDATE "OrderTypes" AS child
SET "ParentOrderTypeId" = parent."Id", "UpdatedAt" = NOW()
FROM "OrderTypes" AS parent
WHERE parent."Code" = 'VALUE_ADDED_SERVICE' AND parent."ParentOrderTypeId" IS NULL
  AND (parent."CompanyId" IS NOT DISTINCT FROM child."CompanyId")
  AND child."ParentOrderTypeId" IS NULL
  AND UPPER(TRIM(child."Code")) IN ('FIXEDIP', 'FIXED_IP', 'UPGRADE', 'IAD');

-- (4) Normalize display names
UPDATE "OrderTypes" SET "Name" = 'Indoor', "UpdatedAt" = NOW() WHERE UPPER(TRIM("Code")) IN ('INDOOR', 'MODIFICATION_INDOOR', 'MODIFICATION INDOOR');
UPDATE "OrderTypes" SET "Name" = 'Outdoor', "UpdatedAt" = NOW() WHERE UPPER(TRIM("Code")) IN ('OUTDOOR', 'MODIFICATION_OUTDOOR', 'MODIFICATION OUTDOOR');
UPDATE "OrderTypes" SET "Name" = 'Repull', "UpdatedAt" = NOW() WHERE UPPER(TRIM("Code")) IN ('ASSURANCE-REPULL', 'REPULL');
UPDATE "OrderTypes" SET "Name" = 'Standard', "UpdatedAt" = NOW() WHERE UPPER(TRIM("Code")) = 'STANDARD';
UPDATE "OrderTypes" SET "Name" = 'Fixed IP', "UpdatedAt" = NOW() WHERE UPPER(TRIM("Code")) IN ('FIXEDIP', 'FIXED_IP');
UPDATE "OrderTypes" SET "Name" = 'Upgrade', "UpdatedAt" = NOW() WHERE UPPER(TRIM("Code")) = 'UPGRADE';
UPDATE "OrderTypes" SET "Name" = 'IAD', "UpdatedAt" = NOW() WHERE UPPER(TRIM("Code")) = 'IAD';

COMMIT;
