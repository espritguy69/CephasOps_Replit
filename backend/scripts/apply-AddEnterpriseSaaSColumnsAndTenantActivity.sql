-- AddEnterpriseSaaSColumnsAndTenantActivity (20260313140000)
-- Idempotent: adds columns and table only if they don't exist.
-- Run when TenantMetricsDaily already exists (e.g. after SaasScalingSubscriptionAndMetrics).

-- TenantMetricsDaily: HealthScore, HealthStatus, RateLimitExceededCount
DO $$
BEGIN
  IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'TenantMetricsDaily') THEN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'TenantMetricsDaily' AND column_name = 'HealthScore') THEN
      ALTER TABLE "TenantMetricsDaily" ADD COLUMN "HealthScore" integer NULL;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'TenantMetricsDaily' AND column_name = 'HealthStatus') THEN
      ALTER TABLE "TenantMetricsDaily" ADD COLUMN "HealthStatus" character varying(20) NULL;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'TenantMetricsDaily' AND column_name = 'RateLimitExceededCount') THEN
      ALTER TABLE "TenantMetricsDaily" ADD COLUMN "RateLimitExceededCount" integer NOT NULL DEFAULT 0;
    END IF;
  END IF;
END $$;

-- TenantActivityEvents
DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'TenantActivityEvents') THEN
    CREATE TABLE "TenantActivityEvents" (
      "Id" uuid NOT NULL,
      "TenantId" uuid NOT NULL,
      "EventType" character varying(100) NOT NULL,
      "EntityType" character varying(100) NULL,
      "EntityId" uuid NULL,
      "Description" character varying(500) NULL,
      "UserId" uuid NULL,
      "TimestampUtc" timestamp with time zone NOT NULL,
      "MetadataJson" text NULL,
      CONSTRAINT "PK_TenantActivityEvents" PRIMARY KEY ("Id")
    );
    CREATE INDEX "IX_TenantActivityEvents_TenantId_TimestampUtc" ON "TenantActivityEvents" ("TenantId", "TimestampUtc");
  END IF;
END $$;
