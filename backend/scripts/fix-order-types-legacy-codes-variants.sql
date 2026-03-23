-- Link legacy order type codes that use spaces/variants (ASSURANCE REPULL, FIXED IP, VAS)
-- Run after fix-order-types-hierarchy-data.sql if your DB has these Code values

-- ASSURANCE REPULL -> parent ASSURANCE (same CompanyId)
UPDATE "OrderTypes" AS child
SET "ParentOrderTypeId" = parent."Id", "UpdatedAt" = NOW(), "Name" = 'Repull'
FROM "OrderTypes" AS parent
WHERE parent."Code" = 'ASSURANCE' AND parent."ParentOrderTypeId" IS NULL
  AND (parent."CompanyId" IS NOT DISTINCT FROM child."CompanyId")
  AND child."Code" = 'ASSURANCE REPULL' AND child."ParentOrderTypeId" IS NULL;

-- FIXED IP -> parent VALUE_ADDED_SERVICE
UPDATE "OrderTypes" AS child
SET "ParentOrderTypeId" = parent."Id", "UpdatedAt" = NOW(), "Name" = 'Fixed IP'
FROM "OrderTypes" AS parent
WHERE parent."Code" = 'VALUE_ADDED_SERVICE' AND parent."ParentOrderTypeId" IS NULL
  AND (parent."CompanyId" IS NOT DISTINCT FROM child."CompanyId")
  AND child."Code" = 'FIXED IP' AND child."ParentOrderTypeId" IS NULL;

-- VAS (Value Added Service alias) -> parent VALUE_ADDED_SERVICE
UPDATE "OrderTypes" AS child
SET "ParentOrderTypeId" = parent."Id", "UpdatedAt" = NOW()
FROM "OrderTypes" AS parent
WHERE parent."Code" = 'VALUE_ADDED_SERVICE' AND parent."ParentOrderTypeId" IS NULL
  AND (parent."CompanyId" IS NOT DISTINCT FROM child."CompanyId")
  AND child."Code" = 'VAS' AND child."ParentOrderTypeId" IS NULL;
