-- RateModifiers table (GPON modular pricing adjustments)
-- Idempotent: creates table only if it doesn't exist.

DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'RateModifiers') THEN
    CREATE TABLE "RateModifiers" (
      "Id" uuid NOT NULL,
      "CompanyId" uuid NULL,
      "ModifierType" integer NOT NULL,
      "ModifierValueId" uuid NULL,
      "ModifierValueString" character varying(100) NULL,
      "AdjustmentType" integer NOT NULL,
      "AdjustmentValue" numeric(18,4) NOT NULL,
      "Priority" integer NOT NULL,
      "IsActive" boolean NOT NULL,
      "Notes" character varying(500) NULL,
      "CreatedAt" timestamp with time zone NOT NULL,
      "UpdatedAt" timestamp with time zone NOT NULL,
      "IsDeleted" boolean NOT NULL,
      "DeletedAt" timestamp with time zone NULL,
      "DeletedByUserId" uuid NULL,
      "RowVersion" bytea NULL,
      CONSTRAINT "PK_RateModifiers" PRIMARY KEY ("Id")
    );

    CREATE INDEX "IX_RateModifiers_CompanyId_ModifierType_IsActive" ON "RateModifiers" ("CompanyId", "ModifierType", "IsActive") WHERE "IsDeleted" = false;

    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260308160000_AddRateModifiers', '10.0.3');
  END IF;
END $$;
