-- Reassign any children from the soft-deleted duplicate to the canonical Assurance, then hard-delete the duplicate.
-- Canonical Assurance (the one we keep): c960e440-29e4-4dcd-863a-5df50c2cda9f
UPDATE "OrderTypes"
SET "ParentOrderTypeId" = 'c960e440-29e4-4dcd-863a-5df50c2cda9f', "UpdatedAt" = NOW()
WHERE "ParentOrderTypeId" IN (SELECT "Id" FROM "OrderTypes" WHERE "Code" = 'ASSURANCE' AND "ParentOrderTypeId" IS NULL AND "IsDeleted" = true);

-- Now hard-delete the soft-deleted duplicate Assurance parent(s)
DELETE FROM "OrderTypes"
WHERE "Code" = 'ASSURANCE' AND "ParentOrderTypeId" IS NULL AND "IsDeleted" = true
RETURNING "Id", "Name";
