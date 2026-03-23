-- ============================================
-- Workflow evidence check (idempotent)
-- Run before/after seeding to verify required workflows and transitions.
-- Outputs: which workflows exist, missing transitions, PASS/FAIL summary.
-- ============================================
-- Prerequisite: WorkflowDefinitions and WorkflowTransitions exist; soft-delete
-- migration applied (IsDeleted column present). Run after DB migrations.
-- ============================================

-- 1) Workflows that exist (active, not deleted)
SELECT '=== 1. WORKFLOW DEFINITIONS (active, not deleted) ===' AS section;
SELECT
    w."Id",
    w."Name",
    w."EntityType",
    w."CompanyId",
    w."IsActive",
    COALESCE(w."IsDeleted", false) AS "IsDeleted",
    w."CreatedAt"
FROM "WorkflowDefinitions" w
WHERE w."IsActive" = true
  AND (w."IsDeleted" = false OR w."IsDeleted" IS NULL)
ORDER BY w."EntityType", w."CreatedAt" DESC;

-- 2) Required Order workflow transitions (canonical set from 07_gpon_order_workflow + baseline)
--    We compare expected vs actual for EntityType = 'Order'.
WITH expected_order_transitions AS (
    SELECT * FROM (VALUES
        ('Pending', 'Assigned'),
        ('Pending', 'Cancelled'),
        ('Assigned', 'OnTheWay'),
        ('Assigned', 'Blocker'),
        ('Assigned', 'ReschedulePendingApproval'),
        ('Assigned', 'Cancelled'),
        ('OnTheWay', 'MetCustomer'),
        ('OnTheWay', 'Blocker'),
        ('MetCustomer', 'OrderCompleted'),
        ('MetCustomer', 'Blocker'),
        ('Blocker', 'MetCustomer'),
        ('Blocker', 'Assigned'),
        ('Blocker', 'ReschedulePendingApproval'),
        ('Blocker', 'Cancelled'),
        ('ReschedulePendingApproval', 'Assigned'),
        ('ReschedulePendingApproval', 'Cancelled'),
        ('OrderCompleted', 'DocketsReceived'),
        ('DocketsReceived', 'DocketsVerified'),
        ('DocketsReceived', 'DocketsRejected'),
        ('DocketsRejected', 'DocketsReceived'),
        ('DocketsVerified', 'DocketsUploaded'),
        ('DocketsUploaded', 'ReadyForInvoice'),
        ('ReadyForInvoice', 'Invoiced'),
        ('Invoiced', 'SubmittedToPortal'),
        ('SubmittedToPortal', 'Completed'),
        ('Invoiced', 'Rejected'),
        ('SubmittedToPortal', 'Rejected'),
        ('Rejected', 'ReadyForInvoice'),
        ('Rejected', 'Reinvoice'),
        ('Reinvoice', 'Invoiced')
    ) AS t("FromStatus", "ToStatus")
),
order_workflow AS (
    SELECT w."Id" AS "WorkflowDefinitionId", w."CompanyId"
    FROM "WorkflowDefinitions" w
    WHERE w."EntityType" = 'Order'
      AND w."IsActive" = true
      AND (w."IsDeleted" = false OR w."IsDeleted" IS NULL)
    ORDER BY w."CreatedAt" DESC
    LIMIT 1
),
actual_order_transitions AS (
    SELECT t."FromStatus", t."ToStatus"
    FROM "WorkflowTransitions" t
    INNER JOIN order_workflow ow ON t."WorkflowDefinitionId" = ow."WorkflowDefinitionId"
       AND t."CompanyId" = ow."CompanyId"
    WHERE t."IsActive" = true
      AND (t."IsDeleted" = false OR t."IsDeleted" IS NULL)
)
SELECT '=== 2. ORDER WORKFLOW: MISSING TRANSITIONS (expected but not in DB) ===' AS section;
SELECT e."FromStatus", e."ToStatus"
FROM expected_order_transitions e
LEFT JOIN actual_order_transitions a
  ON (a."FromStatus" IS NOT DISTINCT FROM e."FromStatus" AND a."ToStatus" = e."ToStatus")
WHERE a."FromStatus" IS NULL
ORDER BY e."FromStatus", e."ToStatus";

-- 3) Extra transitions (in DB but not in expected set) – informational only
SELECT '=== 3. ORDER WORKFLOW: EXTRA TRANSITIONS (in DB, not in canonical list) ===' AS section;
WITH expected_order_transitions AS (
    SELECT * FROM (VALUES
        ('Pending', 'Assigned'), ('Pending', 'Cancelled'),
        ('Assigned', 'OnTheWay'), ('Assigned', 'Blocker'), ('Assigned', 'ReschedulePendingApproval'), ('Assigned', 'Cancelled'),
        ('OnTheWay', 'MetCustomer'), ('OnTheWay', 'Blocker'),
        ('MetCustomer', 'OrderCompleted'), ('MetCustomer', 'Blocker'),
        ('Blocker', 'MetCustomer'), ('Blocker', 'Assigned'), ('Blocker', 'ReschedulePendingApproval'), ('Blocker', 'Cancelled'),
        ('ReschedulePendingApproval', 'Assigned'), ('ReschedulePendingApproval', 'Cancelled'),
        ('OrderCompleted', 'DocketsReceived'),
        ('DocketsReceived', 'DocketsVerified'), ('DocketsReceived', 'DocketsRejected'),
        ('DocketsRejected', 'DocketsReceived'),
        ('DocketsVerified', 'DocketsUploaded'),
        ('DocketsUploaded', 'ReadyForInvoice'),
        ('ReadyForInvoice', 'Invoiced'),
        ('Invoiced', 'SubmittedToPortal'), ('SubmittedToPortal', 'Completed'),
        ('Invoiced', 'Rejected'), ('SubmittedToPortal', 'Rejected'),
        ('Rejected', 'ReadyForInvoice'), ('Rejected', 'Reinvoice'), ('Reinvoice', 'Invoiced')
    ) AS t("FromStatus", "ToStatus")
),
order_workflow AS (
    SELECT w."Id" AS "WorkflowDefinitionId"
    FROM "WorkflowDefinitions" w
    WHERE w."EntityType" = 'Order' AND w."IsActive" = true
      AND (w."IsDeleted" = false OR w."IsDeleted" IS NULL)
    ORDER BY w."CreatedAt" DESC LIMIT 1
),
actual_order_transitions AS (
    SELECT t."FromStatus", t."ToStatus"
    FROM "WorkflowTransitions" t
    INNER JOIN order_workflow ow ON t."WorkflowDefinitionId" = ow."WorkflowDefinitionId"
    WHERE (t."IsDeleted" = false OR t."IsDeleted" IS NULL)
)
SELECT a."FromStatus", a."ToStatus"
FROM actual_order_transitions a
LEFT JOIN expected_order_transitions e
  ON (e."FromStatus" IS NOT DISTINCT FROM a."FromStatus" AND e."ToStatus" = a."ToStatus")
WHERE e."FromStatus" IS NULL
ORDER BY a."FromStatus", a."ToStatus";

-- 4) PASS/FAIL summary per workflow
SELECT '=== 4. SUMMARY: PASS/FAIL PER WORKFLOW ===' AS section;
WITH expected_order_transitions AS (
    SELECT * FROM (VALUES
        ('Pending', 'Assigned'), ('Pending', 'Cancelled'),
        ('Assigned', 'OnTheWay'), ('Assigned', 'Blocker'), ('Assigned', 'ReschedulePendingApproval'), ('Assigned', 'Cancelled'),
        ('OnTheWay', 'MetCustomer'), ('OnTheWay', 'Blocker'),
        ('MetCustomer', 'OrderCompleted'), ('MetCustomer', 'Blocker'),
        ('Blocker', 'MetCustomer'), ('Blocker', 'Assigned'), ('Blocker', 'ReschedulePendingApproval'), ('Blocker', 'Cancelled'),
        ('ReschedulePendingApproval', 'Assigned'), ('ReschedulePendingApproval', 'Cancelled'),
        ('OrderCompleted', 'DocketsReceived'),
        ('DocketsReceived', 'DocketsVerified'), ('DocketsReceived', 'DocketsRejected'),
        ('DocketsRejected', 'DocketsReceived'),
        ('DocketsVerified', 'DocketsUploaded'),
        ('DocketsUploaded', 'ReadyForInvoice'),
        ('ReadyForInvoice', 'Invoiced'),
        ('Invoiced', 'SubmittedToPortal'), ('SubmittedToPortal', 'Completed'),
        ('Invoiced', 'Rejected'), ('SubmittedToPortal', 'Rejected'),
        ('Rejected', 'ReadyForInvoice'), ('Rejected', 'Reinvoice'), ('Reinvoice', 'Invoiced')
    ) AS t("FromStatus", "ToStatus")
),
order_workflow AS (
    SELECT w."Id" AS "WorkflowDefinitionId", w."Name", w."EntityType"
    FROM "WorkflowDefinitions" w
    WHERE w."EntityType" = 'Order' AND w."IsActive" = true
      AND (w."IsDeleted" = false OR w."IsDeleted" IS NULL)
    ORDER BY w."CreatedAt" DESC LIMIT 1
),
actual_order_transitions AS (
    SELECT t."FromStatus", t."ToStatus"
    FROM "WorkflowTransitions" t
    INNER JOIN order_workflow ow ON t."WorkflowDefinitionId" = ow."WorkflowDefinitionId"
    WHERE (t."IsDeleted" = false OR t."IsDeleted" IS NULL)
),
missing AS (
    SELECT COUNT(*) AS cnt
    FROM expected_order_transitions e
    LEFT JOIN actual_order_transitions a
      ON (a."FromStatus" IS NOT DISTINCT FROM e."FromStatus" AND a."ToStatus" = e."ToStatus")
    WHERE a."FromStatus" IS NULL
)
SELECT
    COALESCE(ow."EntityType", 'Order') AS "Workflow",
    COALESCE(ow."Name", '(no definition)') AS "WorkflowName",
    CASE WHEN ow."WorkflowDefinitionId" IS NULL THEN 'FAIL' WHEN m.cnt > 0 THEN 'FAIL' ELSE 'PASS' END AS "Result",
    CASE WHEN ow."WorkflowDefinitionId" IS NULL THEN 30 ELSE m.cnt END AS "MissingTransitions",
    30 AS "ExpectedTransitionCount"
FROM (SELECT 1) AS dummy
LEFT JOIN order_workflow ow ON true
LEFT JOIN missing m ON true;
