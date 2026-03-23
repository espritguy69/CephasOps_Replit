-- OperationalInsights (Field Ops Intelligence) + BillingPlanFeatures + TenantFeatureFlags
-- Idempotent: creates tables only if they don't exist.
-- Run after schema exists (BillingPlans, TenantSubscriptions).

-- OperationalInsights
DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'OperationalInsights') THEN
    CREATE TABLE "OperationalInsights" (
      "Id" uuid NOT NULL,
      "CompanyId" uuid NOT NULL,
      "Type" character varying(128) NOT NULL,
      "PayloadJson" character varying(8000) NULL,
      "OccurredAtUtc" timestamp with time zone NOT NULL,
      "EntityType" character varying(64) NULL,
      "EntityId" uuid NULL,
      CONSTRAINT "PK_OperationalInsights" PRIMARY KEY ("Id")
    );
    CREATE INDEX "IX_OperationalInsights_CompanyId_OccurredAtUtc" ON "OperationalInsights" ("CompanyId", "OccurredAtUtc");
    CREATE INDEX "IX_OperationalInsights_CompanyId_Type" ON "OperationalInsights" ("CompanyId", "Type");
  END IF;
END $$;

-- BillingPlanFeatures
DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'BillingPlanFeatures') THEN
    CREATE TABLE "BillingPlanFeatures" (
      "Id" uuid NOT NULL,
      "BillingPlanId" uuid NOT NULL,
      "FeatureKey" character varying(128) NOT NULL,
      "CreatedAtUtc" timestamp with time zone NOT NULL,
      CONSTRAINT "PK_BillingPlanFeatures" PRIMARY KEY ("Id"),
      CONSTRAINT "FK_BillingPlanFeatures_BillingPlans_BillingPlanId" FOREIGN KEY ("BillingPlanId") REFERENCES "BillingPlans" ("Id") ON DELETE CASCADE
    );
    CREATE UNIQUE INDEX "IX_BillingPlanFeatures_BillingPlanId_FeatureKey" ON "BillingPlanFeatures" ("BillingPlanId", "FeatureKey");
  END IF;
END $$;

-- TenantFeatureFlags
DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'TenantFeatureFlags') THEN
    CREATE TABLE "TenantFeatureFlags" (
      "Id" uuid NOT NULL,
      "TenantId" uuid NOT NULL,
      "FeatureKey" character varying(128) NOT NULL,
      "IsEnabled" boolean NOT NULL,
      "UpdatedAtUtc" timestamp with time zone NOT NULL,
      CONSTRAINT "PK_TenantFeatureFlags" PRIMARY KEY ("Id")
    );
    CREATE UNIQUE INDEX "IX_TenantFeatureFlags_TenantId_FeatureKey" ON "TenantFeatureFlags" ("TenantId", "FeatureKey");
  END IF;
END $$;
