-- ============================================
-- Parser List Evidence: DB (last 24h)
-- Run against cephasops DB. Use for "1) Evidence: DB".
-- ============================================

-- 1) ParseSession: latest timestamps (last 24h)
SELECT '=== PARSE SESSIONS (last 24h) ===' AS info;
SELECT
    ps."Id",
    ps."Status",
    ps."CreatedAt",
    ps."CompletedAt",
    ps."ParsedOrdersCount",
    ps."SourceType"
FROM "ParseSessions" ps
WHERE ps."CreatedAt" >= NOW() - INTERVAL '24 hours'
ORDER BY ps."CreatedAt" DESC
LIMIT 20;

-- 2) ParsedOrderDraft: latest 20 with createdAt, confidence, status, parseSessionId
SELECT '=== PARSED ORDER DRAFTS (latest 20) ===' AS info;
SELECT
    pod."Id",
    pod."CreatedAt",
    pod."ConfidenceScore",
    pod."ValidationStatus",
    pod."ParseSessionId",
    pod."ServiceId",
    pod."CustomerName",
    pod."CreatedOrderId"
FROM "ParsedOrderDrafts" pod
ORDER BY pod."CreatedAt" DESC
LIMIT 20;

-- 3) Count drafts in last 24h
SELECT '=== DRAFT COUNTS (last 24h) ===' AS info;
SELECT
    COUNT(*) FILTER (WHERE pod."CreatedAt" >= NOW() - INTERVAL '24 hours') AS drafts_last_24h,
    COUNT(*) AS drafts_total
FROM "ParsedOrderDrafts" pod;

-- Conclusion: If drafts_last_24h > 0 and latest "CreatedAt" is recent → drafts exist (YES).
--             If drafts_last_24h = 0 and no recent CreatedAt → no new drafts (NO).
