-- CephasOps Production Readiness — Monitoring queries (Postgres-only)
-- All use existing tables; no telemetry code. Run via scheduled job or ad hoc.

-- -----------------------------------------------------------------------------
-- Failure rate per day (drafts with NeedsReview or Rejected)
-- -----------------------------------------------------------------------------
SELECT date_trunc('day', "CreatedAt") AS day,
       COUNT(*) FILTER (WHERE "ValidationStatus" IN ('NeedsReview','Rejected')) AS failures,
       COUNT(*) AS total
FROM "ParsedOrderDrafts"
WHERE "CreatedAt" >= current_date - interval '30 days'
GROUP BY 1
ORDER BY 1;

-- -----------------------------------------------------------------------------
-- MissingRequiredFields frequency (parse ValidationNotes for "required" or "Missing=")
-- -----------------------------------------------------------------------------
SELECT "ValidationNotes",
       COUNT(*) AS cnt
FROM "ParsedOrderDrafts"
WHERE "ValidationStatus" = 'NeedsReview'
  AND ("ValidationNotes" LIKE '%required%' OR "ValidationNotes" LIKE '%Missing=%')
  AND "CreatedAt" >= current_date - interval '14 days'
GROUP BY "ValidationNotes"
ORDER BY cnt DESC
LIMIT 20;

-- -----------------------------------------------------------------------------
-- Confidence trend (avg by day)
-- -----------------------------------------------------------------------------
SELECT date_trunc('day', "CreatedAt") AS day,
       AVG("ConfidenceScore") AS avg_confidence,
       COUNT(*) AS drafts
FROM "ParsedOrderDrafts"
WHERE "CreatedAt" >= current_date - interval '30 days'
GROUP BY 1
ORDER BY 1;

-- -----------------------------------------------------------------------------
-- Top failing partners (by PartnerId on draft)
-- If PartnerId is often null, group by OrderTypeCode and/or SourceFileName pattern.
-- -----------------------------------------------------------------------------
SELECT "PartnerId", "OrderTypeCode",
       COUNT(*) FILTER (WHERE "ValidationStatus" IN ('NeedsReview','Rejected')) AS failures,
       COUNT(*) AS total
FROM "ParsedOrderDrafts"
WHERE "CreatedAt" >= current_date - interval '14 days'
GROUP BY "PartnerId", "OrderTypeCode"
ORDER BY failures DESC
LIMIT 20;

-- -----------------------------------------------------------------------------
-- ParseError / ParseSession failures (by day)
-- -----------------------------------------------------------------------------
SELECT date_trunc('day', ps."CreatedAt") AS day,
       COUNT(*) AS session_failures
FROM "ParseSessions" ps
WHERE ps."Status" = 'Failed'
  AND ps."CreatedAt" >= current_date - interval '30 days'
GROUP BY 1
ORDER BY 1;
