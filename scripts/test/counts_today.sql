SELECT COUNT(*) AS emailmessages_today, MAX("CreatedAt") AS newest
FROM "EmailMessages"
WHERE "CreatedAt" >= date_trunc('day', now());

SELECT COUNT(*) AS parsesessions_today, MAX("CreatedAt") AS newest
FROM "ParseSessions"
WHERE "CreatedAt" >= date_trunc('day', now());

SELECT COUNT(*) AS drafts_today, MAX("CreatedAt") AS newest
FROM "ParsedOrderDrafts"
WHERE "CreatedAt" >= date_trunc('day', now());
