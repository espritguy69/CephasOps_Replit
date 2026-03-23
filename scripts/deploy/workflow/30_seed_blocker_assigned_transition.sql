-- ============================================
-- Seed Blocker → Assigned Transition (idempotent)
-- Ensures the transition Blocker → Assigned exists for the Order workflow.
-- Use when the DB was seeded with only minimal workflow (e.g. create-order-workflow-if-missing.sql)
-- and is missing this exit from Blocker. Re-running does not duplicate. No deletes.
-- See: docs/workflow-seeding-runbook.md, docs/operations/db_workflow_baseline_spec.md §2 (row 11)
-- ============================================

DO $$
DECLARE
    v_workflow_def_id UUID;
    v_company_id UUID;
    v_inserted INT;
BEGIN
    SELECT "Id" INTO v_company_id FROM "Companies" LIMIT 1;
    IF v_company_id IS NULL THEN
        v_company_id := '00000000-0000-0000-0000-000000000000'::uuid;
    END IF;

    SELECT "Id" INTO v_workflow_def_id
    FROM "WorkflowDefinitions"
    WHERE "EntityType" = 'Order' AND "IsActive" = true AND ("IsDeleted" = false OR "IsDeleted" IS NULL)
    ORDER BY "CreatedAt" DESC LIMIT 1;

    IF v_workflow_def_id IS NULL THEN
        RAISE EXCEPTION 'No active Order WorkflowDefinition found. Run 10_seed_order_workflow_if_missing.sql first.';
    END IF;

    INSERT INTO "WorkflowTransitions" ("Id", "CompanyId", "WorkflowDefinitionId", "FromStatus", "ToStatus", "AllowedRolesJson", "IsActive", "IsDeleted", "DisplayOrder", "CreatedAt", "UpdatedAt")
    SELECT gen_random_uuid(), v_company_id, v_workflow_def_id, 'Blocker', 'Assigned', '["Ops","Admin","HOD"]'::jsonb, true, false, 12, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
    WHERE NOT EXISTS (SELECT 1 FROM "WorkflowTransitions" WHERE "WorkflowDefinitionId" = v_workflow_def_id AND "FromStatus" = 'Blocker' AND "ToStatus" = 'Assigned' AND ("IsDeleted" = false OR "IsDeleted" IS NULL));

    GET DIAGNOSTICS v_inserted = ROW_COUNT;
    IF v_inserted > 0 THEN
        RAISE NOTICE 'Inserted transition: Blocker -> Assigned';
    ELSE
        RAISE NOTICE 'Blocker -> Assigned already exists; no insert.';
    END IF;
END $$;
