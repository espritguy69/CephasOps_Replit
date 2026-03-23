-- ============================================
-- Seed Invoice Rejection Loop Transitions (idempotent)
-- Adds optional transitions: Invoiced/SubmittedToPortal → Rejected;
-- Rejected → ReadyForInvoice, Rejected → Reinvoice; Reinvoice → Invoiced.
-- Requires an active Order WorkflowDefinition. Run after 10_seed_order_workflow_if_missing.sql.
-- Re-running does not duplicate rows. No deletes.
-- See: docs/workflow-seeding-runbook.md, docs/operations/db_workflow_baseline_spec.md §3
-- ============================================

DO $$
DECLARE
    v_workflow_def_id UUID;
    v_company_id UUID;
    v_inserted INT := 0;
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

    -- O1: Invoiced -> Rejected
    INSERT INTO "WorkflowTransitions" ("Id", "CompanyId", "WorkflowDefinitionId", "FromStatus", "ToStatus", "AllowedRolesJson", "IsActive", "IsDeleted", "DisplayOrder", "CreatedAt", "UpdatedAt")
    SELECT gen_random_uuid(), v_company_id, v_workflow_def_id, 'Invoiced', 'Rejected', '["System","Ops","Admin"]'::jsonb, true, false, 100, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
    WHERE NOT EXISTS (SELECT 1 FROM "WorkflowTransitions" WHERE "WorkflowDefinitionId" = v_workflow_def_id AND "FromStatus" = 'Invoiced' AND "ToStatus" = 'Rejected' AND ("IsDeleted" = false OR "IsDeleted" IS NULL));
    GET DIAGNOSTICS v_inserted = ROW_COUNT; IF v_inserted > 0 THEN RAISE NOTICE 'Inserted: Invoiced -> Rejected'; END IF;

    -- O2: SubmittedToPortal -> Rejected
    INSERT INTO "WorkflowTransitions" ("Id", "CompanyId", "WorkflowDefinitionId", "FromStatus", "ToStatus", "AllowedRolesJson", "IsActive", "IsDeleted", "DisplayOrder", "CreatedAt", "UpdatedAt")
    SELECT gen_random_uuid(), v_company_id, v_workflow_def_id, 'SubmittedToPortal', 'Rejected', '["System","Ops","Admin"]'::jsonb, true, false, 101, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
    WHERE NOT EXISTS (SELECT 1 FROM "WorkflowTransitions" WHERE "WorkflowDefinitionId" = v_workflow_def_id AND "FromStatus" = 'SubmittedToPortal' AND "ToStatus" = 'Rejected' AND ("IsDeleted" = false OR "IsDeleted" IS NULL));
    GET DIAGNOSTICS v_inserted = ROW_COUNT; IF v_inserted > 0 THEN RAISE NOTICE 'Inserted: SubmittedToPortal -> Rejected'; END IF;

    -- O3: Rejected -> ReadyForInvoice
    INSERT INTO "WorkflowTransitions" ("Id", "CompanyId", "WorkflowDefinitionId", "FromStatus", "ToStatus", "AllowedRolesJson", "IsActive", "IsDeleted", "DisplayOrder", "CreatedAt", "UpdatedAt")
    SELECT gen_random_uuid(), v_company_id, v_workflow_def_id, 'Rejected', 'ReadyForInvoice', '["Ops","Admin"]'::jsonb, true, false, 102, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
    WHERE NOT EXISTS (SELECT 1 FROM "WorkflowTransitions" WHERE "WorkflowDefinitionId" = v_workflow_def_id AND "FromStatus" = 'Rejected' AND "ToStatus" = 'ReadyForInvoice' AND ("IsDeleted" = false OR "IsDeleted" IS NULL));
    GET DIAGNOSTICS v_inserted = ROW_COUNT; IF v_inserted > 0 THEN RAISE NOTICE 'Inserted: Rejected -> ReadyForInvoice'; END IF;

    -- O4: Rejected -> Reinvoice
    INSERT INTO "WorkflowTransitions" ("Id", "CompanyId", "WorkflowDefinitionId", "FromStatus", "ToStatus", "AllowedRolesJson", "IsActive", "IsDeleted", "DisplayOrder", "CreatedAt", "UpdatedAt")
    SELECT gen_random_uuid(), v_company_id, v_workflow_def_id, 'Rejected', 'Reinvoice', '["Ops","Admin"]'::jsonb, true, false, 103, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
    WHERE NOT EXISTS (SELECT 1 FROM "WorkflowTransitions" WHERE "WorkflowDefinitionId" = v_workflow_def_id AND "FromStatus" = 'Rejected' AND "ToStatus" = 'Reinvoice' AND ("IsDeleted" = false OR "IsDeleted" IS NULL));
    GET DIAGNOSTICS v_inserted = ROW_COUNT; IF v_inserted > 0 THEN RAISE NOTICE 'Inserted: Rejected -> Reinvoice'; END IF;

    -- O5: Reinvoice -> Invoiced
    INSERT INTO "WorkflowTransitions" ("Id", "CompanyId", "WorkflowDefinitionId", "FromStatus", "ToStatus", "AllowedRolesJson", "IsActive", "IsDeleted", "DisplayOrder", "CreatedAt", "UpdatedAt")
    SELECT gen_random_uuid(), v_company_id, v_workflow_def_id, 'Reinvoice', 'Invoiced', '["Ops","Admin"]'::jsonb, true, false, 104, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
    WHERE NOT EXISTS (SELECT 1 FROM "WorkflowTransitions" WHERE "WorkflowDefinitionId" = v_workflow_def_id AND "FromStatus" = 'Reinvoice' AND "ToStatus" = 'Invoiced' AND ("IsDeleted" = false OR "IsDeleted" IS NULL));
    GET DIAGNOSTICS v_inserted = ROW_COUNT; IF v_inserted > 0 THEN RAISE NOTICE 'Inserted: Reinvoice -> Invoiced'; END IF;

    RAISE NOTICE 'Invoice rejection loop seed complete. Re-run 00_check_workflows.sql to verify.';
END $$;
