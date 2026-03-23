-- ServiceProfiles and OrderCategoryServiceProfiles (GPON service family grouping for future pricing)
-- Idempotent: creates tables only if they don't exist.

DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'ServiceProfiles') THEN
    CREATE TABLE "ServiceProfiles" (
      "Id" uuid NOT NULL,
      "Code" character varying(50) NOT NULL,
      "Name" character varying(200) NOT NULL,
      "Description" character varying(500) NULL,
      "IsActive" boolean NOT NULL,
      "DisplayOrder" integer NOT NULL,
      "CompanyId" uuid NULL,
      "CreatedAt" timestamp with time zone NOT NULL,
      "UpdatedAt" timestamp with time zone NOT NULL,
      "IsDeleted" boolean NOT NULL,
      "DeletedAt" timestamp with time zone NULL,
      "DeletedByUserId" uuid NULL,
      "RowVersion" bytea NULL,
      CONSTRAINT "PK_ServiceProfiles" PRIMARY KEY ("Id")
    );

    CREATE UNIQUE INDEX "IX_ServiceProfiles_CompanyId_Code" ON "ServiceProfiles" ("CompanyId", "Code") WHERE "IsDeleted" = false;
    CREATE INDEX "IX_ServiceProfiles_CompanyId_IsActive" ON "ServiceProfiles" ("CompanyId", "IsActive") WHERE "IsDeleted" = false;
  END IF;
END $$;

DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'OrderCategoryServiceProfiles') THEN
    CREATE TABLE "OrderCategoryServiceProfiles" (
      "Id" uuid NOT NULL,
      "OrderCategoryId" uuid NOT NULL,
      "ServiceProfileId" uuid NOT NULL,
      "CompanyId" uuid NULL,
      "CreatedAt" timestamp with time zone NOT NULL,
      "UpdatedAt" timestamp with time zone NOT NULL,
      "IsDeleted" boolean NOT NULL,
      "DeletedAt" timestamp with time zone NULL,
      "DeletedByUserId" uuid NULL,
      "RowVersion" bytea NULL,
      CONSTRAINT "PK_OrderCategoryServiceProfiles" PRIMARY KEY ("Id"),
      CONSTRAINT "FK_OrderCategoryServiceProfiles_OrderCategories_OrderCategoryId" FOREIGN KEY ("OrderCategoryId") REFERENCES "OrderCategories" ("Id") ON DELETE RESTRICT,
      CONSTRAINT "FK_OrderCategoryServiceProfiles_ServiceProfiles_ServiceProfileId" FOREIGN KEY ("ServiceProfileId") REFERENCES "ServiceProfiles" ("Id") ON DELETE RESTRICT
    );

    CREATE UNIQUE INDEX "IX_OrderCategoryServiceProfiles_CompanyId_OrderCategoryId" ON "OrderCategoryServiceProfiles" ("CompanyId", "OrderCategoryId") WHERE "IsDeleted" = false;
    CREATE INDEX "IX_OrderCategoryServiceProfiles_OrderCategoryId" ON "OrderCategoryServiceProfiles" ("OrderCategoryId");
    CREATE INDEX "IX_OrderCategoryServiceProfiles_ServiceProfileId" ON "OrderCategoryServiceProfiles" ("ServiceProfileId");
  END IF;
END $$;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260308133701_AddServiceProfiles', '10.0.3')
ON CONFLICT ("MigrationId") DO NOTHING;
