-- Add ParentOrderTypeId to OrderTypes (schema migration 20260308100000_AddOrderTypeParentOrderTypeId)
-- Run if column is missing before fix-order-types-hierarchy-data.sql

DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'OrderTypes' AND column_name = 'ParentOrderTypeId') THEN
    ALTER TABLE "OrderTypes" ADD COLUMN "ParentOrderTypeId" uuid NULL;
    CREATE INDEX "IX_OrderTypes_ParentOrderTypeId" ON "OrderTypes" ("ParentOrderTypeId");
    ALTER TABLE "OrderTypes" ADD CONSTRAINT "FK_OrderTypes_OrderTypes_ParentOrderTypeId"
      FOREIGN KEY ("ParentOrderTypeId") REFERENCES "OrderTypes" ("Id") ON DELETE RESTRICT;
  END IF;
END $$;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
SELECT '20260308100000_AddOrderTypeParentOrderTypeId', '10.0.0'
WHERE NOT EXISTS (SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260308100000_AddOrderTypeParentOrderTypeId');
