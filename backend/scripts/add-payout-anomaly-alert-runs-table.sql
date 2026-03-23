-- Idempotent: create PayoutAnomalyAlertRuns table if not exists (scheduled/manual alert run history).
-- Run after applying migration 20260310180000_AddPayoutAnomalyAlertRuns.

DO $$
BEGIN
  IF NOT EXISTS (
    SELECT 1 FROM information_schema.tables
    WHERE table_schema = 'public' AND table_name = 'PayoutAnomalyAlertRuns'
  ) THEN
    CREATE TABLE "PayoutAnomalyAlertRuns" (
      "Id" uuid NOT NULL,
      "StartedAt" timestamp with time zone NOT NULL,
      "CompletedAt" timestamp with time zone NULL,
      "EvaluatedCount" integer NOT NULL,
      "SentCount" integer NOT NULL,
      "SkippedCount" integer NOT NULL,
      "ErrorCount" integer NOT NULL,
      "TriggerSource" character varying(32) NOT NULL,
      CONSTRAINT "PK_PayoutAnomalyAlertRuns" PRIMARY KEY ("Id")
    );
    CREATE INDEX "IX_PayoutAnomalyAlertRuns_StartedAt" ON "PayoutAnomalyAlertRuns" ("StartedAt" DESC);
  END IF;
END $$;
