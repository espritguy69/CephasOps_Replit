-- GPON Rate Groups (Phase 1) + Base Work Rates (Phase 2)
-- Idempotent: creates tables only if they don't exist.
-- Requires: OrderTypes, OrderCategories, InstallationMethods tables (from existing schema).

-- Phase 1: RateGroups + OrderTypeSubtypeRateGroups
DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'RateGroups') THEN
    CREATE TABLE "RateGroups" (
      "Id" uuid NOT NULL,
      "CompanyId" uuid NULL,
      "Name" character varying(200) NOT NULL,
      "Code" character varying(50) NOT NULL,
      "Description" character varying(500) NULL,
      "IsActive" boolean NOT NULL,
      "DisplayOrder" integer NOT NULL,
      "CreatedAt" timestamp with time zone NOT NULL,
      "UpdatedAt" timestamp with time zone NOT NULL,
      "IsDeleted" boolean NOT NULL,
      "DeletedAt" timestamp with time zone NULL,
      "DeletedByUserId" uuid NULL,
      "RowVersion" bytea NULL,
      CONSTRAINT "PK_RateGroups" PRIMARY KEY ("Id")
    );
    CREATE UNIQUE INDEX "IX_RateGroups_CompanyId_Code" ON "RateGroups" ("CompanyId", "Code") WHERE "IsDeleted" = false;
    CREATE INDEX "IX_RateGroups_CompanyId_IsActive" ON "RateGroups" ("CompanyId", "IsActive");

    CREATE TABLE "OrderTypeSubtypeRateGroups" (
      "Id" uuid NOT NULL,
      "OrderTypeId" uuid NOT NULL,
      "OrderSubtypeId" uuid NULL,
      "RateGroupId" uuid NOT NULL,
      "CompanyId" uuid NULL,
      CONSTRAINT "PK_OrderTypeSubtypeRateGroups" PRIMARY KEY ("Id"),
      CONSTRAINT "FK_OrderTypeSubtypeRateGroups_OrderTypes_OrderTypeId" FOREIGN KEY ("OrderTypeId") REFERENCES "OrderTypes" ("Id") ON DELETE RESTRICT,
      CONSTRAINT "FK_OrderTypeSubtypeRateGroups_OrderTypes_OrderSubtypeId" FOREIGN KEY ("OrderSubtypeId") REFERENCES "OrderTypes" ("Id") ON DELETE RESTRICT,
      CONSTRAINT "FK_OrderTypeSubtypeRateGroups_RateGroups_RateGroupId" FOREIGN KEY ("RateGroupId") REFERENCES "RateGroups" ("Id") ON DELETE RESTRICT
    );
    CREATE INDEX "IX_OrderTypeSubtypeRateGroups_OrderSubtypeId" ON "OrderTypeSubtypeRateGroups" ("OrderSubtypeId");
    CREATE INDEX "IX_OrderTypeSubtypeRateGroups_RateGroupId" ON "OrderTypeSubtypeRateGroups" ("RateGroupId");
    CREATE UNIQUE INDEX "IX_OrderTypeSubtypeRateGroups_Company_Type_Subtype" ON "OrderTypeSubtypeRateGroups" ("CompanyId", "OrderTypeId", "OrderSubtypeId");

    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260308140000_AddRateGroupAndOrderTypeSubtypeRateGroup', '10.0.3');
  END IF;
END $$;

-- Phase 2: BaseWorkRates
DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'BaseWorkRates') THEN
    CREATE TABLE "BaseWorkRates" (
      "Id" uuid NOT NULL,
      "CompanyId" uuid NULL,
      "RateGroupId" uuid NOT NULL,
      "OrderCategoryId" uuid NULL,
      "InstallationMethodId" uuid NULL,
      "OrderSubtypeId" uuid NULL,
      "Amount" numeric(18,4) NOT NULL,
      "Currency" character varying(3) NOT NULL,
      "EffectiveFrom" timestamp with time zone NULL,
      "EffectiveTo" timestamp with time zone NULL,
      "Priority" integer NOT NULL,
      "IsActive" boolean NOT NULL,
      "Notes" character varying(500) NULL,
      "CreatedAt" timestamp with time zone NOT NULL,
      "UpdatedAt" timestamp with time zone NOT NULL,
      "IsDeleted" boolean NOT NULL,
      "DeletedAt" timestamp with time zone NULL,
      "DeletedByUserId" uuid NULL,
      "RowVersion" bytea NULL,
      CONSTRAINT "PK_BaseWorkRates" PRIMARY KEY ("Id"),
      CONSTRAINT "FK_BaseWorkRates_RateGroups_RateGroupId" FOREIGN KEY ("RateGroupId") REFERENCES "RateGroups" ("Id") ON DELETE RESTRICT,
      CONSTRAINT "FK_BaseWorkRates_OrderCategories_OrderCategoryId" FOREIGN KEY ("OrderCategoryId") REFERENCES "OrderCategories" ("Id") ON DELETE RESTRICT,
      CONSTRAINT "FK_BaseWorkRates_InstallationMethods_InstallationMethodId" FOREIGN KEY ("InstallationMethodId") REFERENCES "InstallationMethods" ("Id") ON DELETE RESTRICT,
      CONSTRAINT "FK_BaseWorkRates_OrderTypes_OrderSubtypeId" FOREIGN KEY ("OrderSubtypeId") REFERENCES "OrderTypes" ("Id") ON DELETE RESTRICT
    );

    CREATE INDEX "IX_BaseWorkRates_RateGroupId" ON "BaseWorkRates" ("RateGroupId");
    CREATE INDEX "IX_BaseWorkRates_OrderCategoryId" ON "BaseWorkRates" ("OrderCategoryId");
    CREATE INDEX "IX_BaseWorkRates_InstallationMethodId" ON "BaseWorkRates" ("InstallationMethodId");
    CREATE INDEX "IX_BaseWorkRates_OrderSubtypeId" ON "BaseWorkRates" ("OrderSubtypeId");
    CREATE INDEX "IX_BaseWorkRates_CompanyId_RateGroupId_IsActive" ON "BaseWorkRates" ("CompanyId", "RateGroupId", "IsActive") WHERE "IsDeleted" = false;
    CREATE INDEX "IX_BaseWorkRates_Lookup" ON "BaseWorkRates" ("RateGroupId", "OrderCategoryId", "InstallationMethodId", "OrderSubtypeId") WHERE "IsDeleted" = false AND "IsActive" = true;

    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260308150000_AddBaseWorkRates', '10.0.3');
  END IF;
END $$;
