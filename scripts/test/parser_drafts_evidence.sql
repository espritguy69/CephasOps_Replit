-- Last 50 ParsedOrderDrafts by CreatedAt (non-deleted only)
SELECT "CreatedAt", "ValidationStatus", "ConfidenceScore"
FROM "ParsedOrderDrafts"
WHERE "IsDeleted" = false
ORDER BY "CreatedAt" DESC
LIMIT 50;

-- Count created today (2026-03-03 UTC)
SELECT COUNT(*) AS drafts_today FROM "ParsedOrderDrafts" WHERE "IsDeleted" = false AND "CreatedAt" >= '2026-03-03 00:00:00+00';

-- Max CreatedAt
SELECT MAX("CreatedAt") AS newest_created FROM "ParsedOrderDrafts" WHERE "IsDeleted" = false;
