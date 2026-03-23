-- ============================================
-- Email Flow Monitoring Script
-- Use this to track emails through the entire pipeline
-- ============================================

-- 1. Recent Email Messages
SELECT 
    '=== RECENT EMAIL MESSAGES ===' as info;

SELECT 
    em."Id",
    em."Subject",
    em."FromAddress",
    em."ReceivedAt",
    em."ParserStatus",
    em."IsVip",
    ea."Name" as "EmailAccountName"
FROM "EmailMessages" em
LEFT JOIN "EmailAccounts" ea ON em."EmailAccountId" = ea."Id"
ORDER BY em."ReceivedAt" DESC
LIMIT 10;

-- 2. Recent Parse Sessions
SELECT 
    '=== RECENT PARSE SESSIONS ===' as info;

SELECT 
    ps."Id",
    ps."Status",
    ps."ParsedOrdersCount",
    ps."CreatedAt",
    em."Subject" as "EmailSubject",
    pt."Name" as "TemplateName"
FROM "ParseSessions" ps
LEFT JOIN "EmailMessages" em ON ps."EmailMessageId" = em."Id"
LEFT JOIN "ParserTemplates" pt ON ps."ParserTemplateId" = pt."Id"
ORDER BY ps."CreatedAt" DESC
LIMIT 10;

-- 3. Recent Parsed Order Drafts
SELECT 
    '=== RECENT PARSED ORDER DRAFTS ===' as info;

SELECT 
    pod."Id",
    pod."ServiceId",
    pod."CustomerName",
    pod."ValidationStatus",
    pod."ConfidenceScore",
    pod."CreatedOrderId",
    pod."CreatedAt",
    ps."Status" as "SessionStatus"
FROM "ParsedOrderDrafts" pod
LEFT JOIN "ParseSessions" ps ON pod."ParseSessionId" = ps."Id"
ORDER BY pod."CreatedAt" DESC
LIMIT 10;

-- 4. Drafts Ready for Review
SELECT 
    '=== DRAFTS READY FOR REVIEW ===' as info;

SELECT 
    pod."Id",
    pod."ServiceId",
    pod."CustomerName",
    pod."ValidationStatus",
    pod."ConfidenceScore",
    pod."CreatedOrderId",
    ps."Status" as "SessionStatus"
FROM "ParsedOrderDrafts" pod
LEFT JOIN "ParseSessions" ps ON pod."ParseSessionId" = ps."Id"
WHERE pod."CreatedOrderId" IS NULL
AND pod."ValidationStatus" != 'Rejected'
ORDER BY pod."CreatedAt" DESC
LIMIT 10;

-- 5. Orders Created from Drafts
SELECT 
    '=== ORDERS FROM DRAFTS ===' as info;

SELECT 
    o."Id",
    o."OrderNumber",
    o."ServiceId",
    o."Status",
    o."CustomerName",
    o."AppointmentDate",
    o."AssignedSiId",
    pod."Id" as "DraftId",
    pod."ValidationStatus" as "DraftStatus"
FROM "Orders" o
INNER JOIN "ParsedOrderDrafts" pod ON o."Id" = pod."CreatedOrderId"
ORDER BY o."CreatedAt" DESC
LIMIT 10;

-- 6. Email Account Polling Status
SELECT 
    '=== EMAIL ACCOUNT POLLING STATUS ===' as info;

SELECT 
    ea."Id",
    ea."Name",
    ea."IsActive",
    ea."PollIntervalSec",
    ea."LastPolledAt",
    CASE 
        WHEN ea."LastPolledAt" IS NULL THEN 'Never Polled'
        WHEN ea."LastPolledAt" < NOW() - INTERVAL '5 minutes' THEN 'Stale (>5 min)'
        ELSE 'Recent'
    END as "PollStatus",
    (SELECT COUNT(*) FROM "BackgroundJobs" bj 
     WHERE bj."JobType" = 'EmailIngest' 
     AND bj."PayloadJson" LIKE '%' || ea."Id"::text || '%'
     AND bj."State" IN ('Queued', 'Running')) as "ActiveJobs"
FROM "EmailAccounts" ea
WHERE ea."IsActive" = true
ORDER BY ea."LastPolledAt" DESC NULLS LAST;

