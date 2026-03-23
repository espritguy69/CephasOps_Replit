-- Idempotent: create PayoutAnomalyAlerts table if not exists (for payout anomaly alerting).
-- Run after applying migration 20260308180000_AddPayoutAnomalyAlerts, or use this if you apply migrations via script.

DO $$
BEGIN
  IF NOT EXISTS (
    SELECT 1 FROM information_schema.tables
    WHERE table_schema = 'public' AND table_name = 'PayoutAnomalyAlerts'
  ) THEN
    CREATE TABLE "PayoutAnomalyAlerts" (
      "Id" uuid NOT NULL,
      "AnomalyFingerprintId" character varying(64) NOT NULL,
      "Channel" character varying(32) NOT NULL,
      "SentAtUtc" timestamp with time zone NOT NULL,
      "Status" character varying(32) NOT NULL,
      "RetryCount" integer NOT NULL,
      "ErrorMessage" character varying(2000) NULL,
      "RecipientId" character varying(256) NULL,
      CONSTRAINT "PK_PayoutAnomalyAlerts" PRIMARY KEY ("Id")
    );
    CREATE INDEX "IX_PayoutAnomalyAlerts_AnomalyFingerprintId" ON "PayoutAnomalyAlerts" ("AnomalyFingerprintId");
    CREATE INDEX "IX_PayoutAnomalyAlerts_AnomalyFingerprintId_Channel" ON "PayoutAnomalyAlerts" ("AnomalyFingerprintId", "Channel");
    CREATE INDEX "IX_PayoutAnomalyAlerts_SentAtUtc" ON "PayoutAnomalyAlerts" ("SentAtUtc");
  END IF;
END $$;
