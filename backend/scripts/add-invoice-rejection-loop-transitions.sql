-- Add invoice rejection loop transitions to Order Workflow
-- See docs/operations/db_workflow_baseline_spec.md §3 (Optional transitions O1-O5)
-- Code OrderStatus enum uses "Rejected" (display name: Invoice Rejected)

DO $$
DECLARE
    v_workflow_def_id UUID;
    v_company_id UUID;
    v_exists BOOLEAN;
BEGIN
    SELECT "Id" INTO v_company_id FROM "Companies" LIMIT 1;
    IF v_company_id IS NULL THEN
        v_company_id := '00000000-0000-0000-0000-000000000000'::uuid;
    END IF;

    SELECT "Id" INTO v_workflow_def_id
    FROM "WorkflowDefinitions"
    WHERE "EntityType" = 'Order' AND "IsActive" = true AND "IsDeleted" = false
    ORDER BY "CreatedAt" DESC
    LIMIT 1;

    IF v_workflow_def_id IS NULL THEN
        RAISE EXCEPTION 'No active Order WorkflowDefinition found. Run create-order-workflow-if-missing.sql first.';
    END IF;

    -- O1: Invoiced -> Rejected
    SELECT EXISTS(SELECT 1 FROM "WorkflowTransitions"
        WHERE "WorkflowDefinitionId" = v_workflow_def_id AND "FromStatus" = 'Invoiced' AND "ToStatus" = 'Rejected' AND "IsDeleted" = false)
    INTO v_exists;
    IF NOT v_exists THEN
        INSERT INTO "WorkflowTransitions" ("Id", "CompanyId", "WorkflowDefinitionId", "FromStatus", "ToStatus", "AllowedRolesJson", "IsActive", "IsDeleted", "DisplayOrder", "CreatedAt", "UpdatedAt")
        VALUES (gen_random_uuid(), v_company_id, v_workflow_def_id, 'Invoiced', 'Rejected', '["System","Ops","Admin"]'::jsonb, true, false, 100, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);
        RAISE NOTICE 'Created Invoiced -> Rejected';
    END IF;

    -- O2: SubmittedToPortal -> Rejected
    SELECT EXISTS(SELECT 1 FROM "WorkflowTransitions"
        WHERE "WorkflowDefinitionId" = v_workflow_def_id AND "FromStatus" = 'SubmittedToPortal' AND "ToStatus" = 'Rejected' AND "IsDeleted" = false)
    INTO v_exists;
    IF NOT v_exists THEN
        INSERT INTO "WorkflowTransitions" ("Id", "CompanyId", "WorkflowDefinitionId", "FromStatus", "ToStatus", "AllowedRolesJson", "IsActive", "IsDeleted", "DisplayOrder", "CreatedAt", "UpdatedAt")
        VALUES (gen_random_uuid(), v_company_id, v_workflow_def_id, 'SubmittedToPortal', 'Rejected', '["System","Ops","Admin"]'::jsonb, true, false, 101, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);
        RAISE NOTICE 'Created SubmittedToPortal -> Rejected';
    END IF;

    -- O3: Rejected -> ReadyForInvoice
    SELECT EXISTS(SELECT 1 FROM "WorkflowTransitions"
        WHERE "WorkflowDefinitionId" = v_workflow_def_id AND "FromStatus" = 'Rejected' AND "ToStatus" = 'ReadyForInvoice' AND "IsDeleted" = false)
    INTO v_exists;
    IF NOT v_exists THEN
        INSERT INTO "WorkflowTransitions" ("Id", "CompanyId", "WorkflowDefinitionId", "FromStatus", "ToStatus", "AllowedRolesJson", "IsActive", "IsDeleted", "DisplayOrder", "CreatedAt", "UpdatedAt")
        VALUES (gen_random_uuid(), v_company_id, v_workflow_def_id, 'Rejected', 'ReadyForInvoice', '["Ops","Admin"]'::jsonb, true, false, 102, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);
        RAISE NOTICE 'Created Rejected -> ReadyForInvoice';
    END IF;

    -- O4: Rejected -> Reinvoice
    SELECT EXISTS(SELECT 1 FROM "WorkflowTransitions"
        WHERE "WorkflowDefinitionId" = v_workflow_def_id AND "FromStatus" = 'Rejected' AND "ToStatus" = 'Reinvoice' AND "IsDeleted" = false)
    INTO v_exists;
    IF NOT v_exists THEN
        INSERT INTO "WorkflowTransitions" ("Id", "CompanyId", "WorkflowDefinitionId", "FromStatus", "ToStatus", "AllowedRolesJson", "IsActive", "IsDeleted", "DisplayOrder", "CreatedAt", "UpdatedAt")
        VALUES (gen_random_uuid(), v_company_id, v_workflow_def_id, 'Rejected', 'Reinvoice', '["Ops","Admin"]'::jsonb, true, false, 103, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);
        RAISE NOTICE 'Created Rejected -> Reinvoice';
    END IF;

    -- O5: Reinvoice -> Invoiced
    SELECT EXISTS(SELECT 1 FROM "WorkflowTransitions"
        WHERE "WorkflowDefinitionId" = v_workflow_def_id AND "FromStatus" = 'Reinvoice' AND "ToStatus" = 'Invoiced' AND "IsDeleted" = false)
    INTO v_exists;
    IF NOT v_exists THEN
        INSERT INTO "WorkflowTransitions" ("Id", "CompanyId", "WorkflowDefinitionId", "FromStatus", "ToStatus", "AllowedRolesJson", "IsActive", "IsDeleted", "DisplayOrder", "CreatedAt", "UpdatedAt")
        VALUES (gen_random_uuid(), v_company_id, v_workflow_def_id, 'Reinvoice', 'Invoiced', '["Ops","Admin"]'::jsonb, true, false, 104, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);
        RAISE NOTICE 'Created Reinvoice -> Invoiced';
    END IF;

    RAISE NOTICE 'Invoice rejection loop transitions ready.';
END $$;
