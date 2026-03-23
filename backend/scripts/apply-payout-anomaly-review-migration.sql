-- Idempotent apply of AddPayoutAnomalyReview (20260311120000).
-- Run this if dotnet ef database update fails due to other migrations.

CREATE TABLE IF NOT EXISTS "PayoutAnomalyReviews" (
    "Id" uuid NOT NULL,
    "AnomalyFingerprintId" character varying(64) NOT NULL,
    "AnomalyType" character varying(64) NOT NULL,
    "OrderId" uuid NULL,
    "InstallerId" uuid NULL,
    "PayoutSnapshotId" uuid NULL,
    "Severity" character varying(32) NOT NULL,
    "DetectedAt" timestamp with time zone NOT NULL,
    "Status" character varying(32) NOT NULL,
    "AssignedToUserId" uuid NULL,
    "NotesJson" text NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NULL,
    CONSTRAINT "PK_PayoutAnomalyReviews" PRIMARY KEY ("Id")
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_PayoutAnomalyReviews_AnomalyFingerprintId"
ON "PayoutAnomalyReviews" ("AnomalyFingerprintId");

CREATE INDEX IF NOT EXISTS "IX_PayoutAnomalyReviews_Status"
ON "PayoutAnomalyReviews" ("Status");

CREATE INDEX IF NOT EXISTS "IX_PayoutAnomalyReviews_DetectedAt"
ON "PayoutAnomalyReviews" ("DetectedAt");

-- Record migration as applied so EF Core skips it on future updates
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
SELECT '20260311120000_AddPayoutAnomalyReview', '10.0.0'
WHERE NOT EXISTS (SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260311120000_AddPayoutAnomalyReview');
