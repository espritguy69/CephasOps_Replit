-- ============================================
-- Seed Order Workflow (idempotent)
-- Creates WorkflowDefinition for EntityType=Order if missing, and ensures
-- all required GPON order lifecycle transitions exist.
-- Re-running does not duplicate rows. No deletes.
-- See: docs/workflow-seeding-runbook.md, docs/operations/db_workflow_baseline_spec.md
-- ============================================

DO $$
DECLARE
    v_workflow_def_id UUID;
    v_company_id UUID;
    v_inserted_def INT := 0;
BEGIN
    SELECT "Id" INTO v_company_id FROM "Companies" LIMIT 1;
    IF v_company_id IS NULL THEN
        v_company_id := '00000000-0000-0000-0000-000000000000'::uuid;
    END IF;

    -- 1. Create or get WorkflowDefinition
    SELECT "Id" INTO v_workflow_def_id
    FROM "WorkflowDefinitions"
    WHERE "EntityType" = 'Order' AND "IsActive" = true AND ("IsDeleted" = false OR "IsDeleted" IS NULL)
    ORDER BY "CreatedAt" DESC LIMIT 1;

    IF v_workflow_def_id IS NULL THEN
        v_workflow_def_id := gen_random_uuid();
        INSERT INTO "WorkflowDefinitions" ("Id", "CompanyId", "Name", "EntityType", "Description", "IsActive", "IsDeleted", "CreatedAt", "UpdatedAt")
        VALUES (v_workflow_def_id, v_company_id, 'Order Workflow', 'Order', 'GPON order lifecycle - full transitions', true, false, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);
        v_inserted_def := 1;
        RAISE NOTICE 'Inserted WorkflowDefinition: %', v_workflow_def_id;
    ELSE
        RAISE NOTICE 'Using existing WorkflowDefinition: %', v_workflow_def_id;
    END IF;

    -- 2. Transitions: insert only where not exists (idempotent)
    -- Pending
    INSERT INTO "WorkflowTransitions" ("Id", "CompanyId", "WorkflowDefinitionId", "FromStatus", "ToStatus", "AllowedRolesJson", "SideEffectsConfigJson", "IsActive", "IsDeleted", "DisplayOrder", "CreatedAt", "UpdatedAt")
    SELECT gen_random_uuid(), v_company_id, v_workflow_def_id, 'Pending', 'Assigned', '["Admin","Scheduler","Manager"]'::jsonb, '{"checkMaterialCollection":true}'::jsonb, true, false, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
    WHERE NOT EXISTS (SELECT 1 FROM "WorkflowTransitions" WHERE "WorkflowDefinitionId" = v_workflow_def_id AND "FromStatus" = 'Pending' AND "ToStatus" = 'Assigned' AND ("IsDeleted" = false OR "IsDeleted" IS NULL));
    INSERT INTO "WorkflowTransitions" ("Id", "CompanyId", "WorkflowDefinitionId", "FromStatus", "ToStatus", "AllowedRolesJson", "IsActive", "IsDeleted", "DisplayOrder", "CreatedAt", "UpdatedAt")
    SELECT gen_random_uuid(), v_company_id, v_workflow_def_id, 'Pending', 'Cancelled', '["Admin","Ops"]'::jsonb, true, false, 2, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
    WHERE NOT EXISTS (SELECT 1 FROM "WorkflowTransitions" WHERE "WorkflowDefinitionId" = v_workflow_def_id AND "FromStatus" = 'Pending' AND "ToStatus" = 'Cancelled' AND ("IsDeleted" = false OR "IsDeleted" IS NULL));

    INSERT INTO "WorkflowTransitions" ("Id", "CompanyId", "WorkflowDefinitionId", "FromStatus", "ToStatus", "AllowedRolesJson", "IsActive", "IsDeleted", "DisplayOrder", "CreatedAt", "UpdatedAt")
    SELECT gen_random_uuid(), v_company_id, v_workflow_def_id, 'Assigned', 'OnTheWay', '["SI","Admin"]'::jsonb, true, false, 3, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
    WHERE NOT EXISTS (SELECT 1 FROM "WorkflowTransitions" WHERE "WorkflowDefinitionId" = v_workflow_def_id AND "FromStatus" = 'Assigned' AND "ToStatus" = 'OnTheWay' AND ("IsDeleted" = false OR "IsDeleted" IS NULL));

    INSERT INTO "WorkflowTransitions" ("Id", "CompanyId", "WorkflowDefinitionId", "FromStatus", "ToStatus", "AllowedRolesJson", "IsActive", "IsDeleted", "DisplayOrder", "CreatedAt", "UpdatedAt")
    SELECT gen_random_uuid(), v_company_id, v_workflow_def_id, 'Assigned', 'Blocker', '["SI","Admin","Ops"]'::jsonb, true, false, 4, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
    WHERE NOT EXISTS (SELECT 1 FROM "WorkflowTransitions" WHERE "WorkflowDefinitionId" = v_workflow_def_id AND "FromStatus" = 'Assigned' AND "ToStatus" = 'Blocker' AND ("IsDeleted" = false OR "IsDeleted" IS NULL));

    INSERT INTO "WorkflowTransitions" ("Id", "CompanyId", "WorkflowDefinitionId", "FromStatus", "ToStatus", "AllowedRolesJson", "IsActive", "IsDeleted", "DisplayOrder", "CreatedAt", "UpdatedAt")
    SELECT gen_random_uuid(), v_company_id, v_workflow_def_id, 'Assigned', 'ReschedulePendingApproval', '["Admin","Scheduler"]'::jsonb, true, false, 5, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
    WHERE NOT EXISTS (SELECT 1 FROM "WorkflowTransitions" WHERE "WorkflowDefinitionId" = v_workflow_def_id AND "FromStatus" = 'Assigned' AND "ToStatus" = 'ReschedulePendingApproval' AND ("IsDeleted" = false OR "IsDeleted" IS NULL));

    INSERT INTO "WorkflowTransitions" ("Id", "CompanyId", "WorkflowDefinitionId", "FromStatus", "ToStatus", "AllowedRolesJson", "IsActive", "IsDeleted", "DisplayOrder", "CreatedAt", "UpdatedAt")
    SELECT gen_random_uuid(), v_company_id, v_workflow_def_id, 'Assigned', 'Cancelled', '["Admin","Ops"]'::jsonb, true, false, 6, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
    WHERE NOT EXISTS (SELECT 1 FROM "WorkflowTransitions" WHERE "WorkflowDefinitionId" = v_workflow_def_id AND "FromStatus" = 'Assigned' AND "ToStatus" = 'Cancelled' AND ("IsDeleted" = false OR "IsDeleted" IS NULL));

    INSERT INTO "WorkflowTransitions" ("Id", "CompanyId", "WorkflowDefinitionId", "FromStatus", "ToStatus", "AllowedRolesJson", "IsActive", "IsDeleted", "DisplayOrder", "CreatedAt", "UpdatedAt")
    SELECT gen_random_uuid(), v_company_id, v_workflow_def_id, 'OnTheWay', 'MetCustomer', '["SI","Admin"]'::jsonb, true, false, 7, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
    WHERE NOT EXISTS (SELECT 1 FROM "WorkflowTransitions" WHERE "WorkflowDefinitionId" = v_workflow_def_id AND "FromStatus" = 'OnTheWay' AND "ToStatus" = 'MetCustomer' AND ("IsDeleted" = false OR "IsDeleted" IS NULL));

    INSERT INTO "WorkflowTransitions" ("Id", "CompanyId", "WorkflowDefinitionId", "FromStatus", "ToStatus", "AllowedRolesJson", "IsActive", "IsDeleted", "DisplayOrder", "CreatedAt", "UpdatedAt")
    SELECT gen_random_uuid(), v_company_id, v_workflow_def_id, 'OnTheWay', 'Blocker', '["SI","Admin","Ops"]'::jsonb, true, false, 8, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
    WHERE NOT EXISTS (SELECT 1 FROM "WorkflowTransitions" WHERE "WorkflowDefinitionId" = v_workflow_def_id AND "FromStatus" = 'OnTheWay' AND "ToStatus" = 'Blocker' AND ("IsDeleted" = false OR "IsDeleted" IS NULL));

    INSERT INTO "WorkflowTransitions" ("Id", "CompanyId", "WorkflowDefinitionId", "FromStatus", "ToStatus", "AllowedRolesJson", "IsActive", "IsDeleted", "DisplayOrder", "CreatedAt", "UpdatedAt")
    SELECT gen_random_uuid(), v_company_id, v_workflow_def_id, 'MetCustomer', 'OrderCompleted', '["SI","Admin"]'::jsonb, true, false, 9, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
    WHERE NOT EXISTS (SELECT 1 FROM "WorkflowTransitions" WHERE "WorkflowDefinitionId" = v_workflow_def_id AND "FromStatus" = 'MetCustomer' AND "ToStatus" = 'OrderCompleted' AND ("IsDeleted" = false OR "IsDeleted" IS NULL));

    INSERT INTO "WorkflowTransitions" ("Id", "CompanyId", "WorkflowDefinitionId", "FromStatus", "ToStatus", "AllowedRolesJson", "IsActive", "IsDeleted", "DisplayOrder", "CreatedAt", "UpdatedAt")
    SELECT gen_random_uuid(), v_company_id, v_workflow_def_id, 'MetCustomer', 'Blocker', '["SI","Admin","Ops"]'::jsonb, true, false, 10, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
    WHERE NOT EXISTS (SELECT 1 FROM "WorkflowTransitions" WHERE "WorkflowDefinitionId" = v_workflow_def_id AND "FromStatus" = 'MetCustomer' AND "ToStatus" = 'Blocker' AND ("IsDeleted" = false OR "IsDeleted" IS NULL));

    -- Blocker exits
    INSERT INTO "WorkflowTransitions" ("Id", "CompanyId", "WorkflowDefinitionId", "FromStatus", "ToStatus", "AllowedRolesJson", "IsActive", "IsDeleted", "DisplayOrder", "CreatedAt", "UpdatedAt")
    SELECT gen_random_uuid(), v_company_id, v_workflow_def_id, 'Blocker', 'MetCustomer', '["SI","Admin","Ops"]'::jsonb, true, false, 11, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
    WHERE NOT EXISTS (SELECT 1 FROM "WorkflowTransitions" WHERE "WorkflowDefinitionId" = v_workflow_def_id AND "FromStatus" = 'Blocker' AND "ToStatus" = 'MetCustomer' AND ("IsDeleted" = false OR "IsDeleted" IS NULL));

    INSERT INTO "WorkflowTransitions" ("Id", "CompanyId", "WorkflowDefinitionId", "FromStatus", "ToStatus", "AllowedRolesJson", "IsActive", "IsDeleted", "DisplayOrder", "CreatedAt", "UpdatedAt")
    SELECT gen_random_uuid(), v_company_id, v_workflow_def_id, 'Blocker', 'Assigned', '["Ops","Admin","HOD"]'::jsonb, true, false, 12, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
    WHERE NOT EXISTS (SELECT 1 FROM "WorkflowTransitions" WHERE "WorkflowDefinitionId" = v_workflow_def_id AND "FromStatus" = 'Blocker' AND "ToStatus" = 'Assigned' AND ("IsDeleted" = false OR "IsDeleted" IS NULL));

    INSERT INTO "WorkflowTransitions" ("Id", "CompanyId", "WorkflowDefinitionId", "FromStatus", "ToStatus", "AllowedRolesJson", "IsActive", "IsDeleted", "DisplayOrder", "CreatedAt", "UpdatedAt")
    SELECT gen_random_uuid(), v_company_id, v_workflow_def_id, 'Blocker', 'ReschedulePendingApproval', '["Admin","Ops"]'::jsonb, true, false, 13, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
    WHERE NOT EXISTS (SELECT 1 FROM "WorkflowTransitions" WHERE "WorkflowDefinitionId" = v_workflow_def_id AND "FromStatus" = 'Blocker' AND "ToStatus" = 'ReschedulePendingApproval' AND ("IsDeleted" = false OR "IsDeleted" IS NULL));

    INSERT INTO "WorkflowTransitions" ("Id", "CompanyId", "WorkflowDefinitionId", "FromStatus", "ToStatus", "AllowedRolesJson", "IsActive", "IsDeleted", "DisplayOrder", "CreatedAt", "UpdatedAt")
    SELECT gen_random_uuid(), v_company_id, v_workflow_def_id, 'Blocker', 'Cancelled', '["Admin","Ops"]'::jsonb, true, false, 14, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
    WHERE NOT EXISTS (SELECT 1 FROM "WorkflowTransitions" WHERE "WorkflowDefinitionId" = v_workflow_def_id AND "FromStatus" = 'Blocker' AND "ToStatus" = 'Cancelled' AND ("IsDeleted" = false OR "IsDeleted" IS NULL));

    INSERT INTO "WorkflowTransitions" ("Id", "CompanyId", "WorkflowDefinitionId", "FromStatus", "ToStatus", "AllowedRolesJson", "IsActive", "IsDeleted", "DisplayOrder", "CreatedAt", "UpdatedAt")
    SELECT gen_random_uuid(), v_company_id, v_workflow_def_id, 'ReschedulePendingApproval', 'Assigned', '["Admin","Scheduler"]'::jsonb, true, false, 15, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
    WHERE NOT EXISTS (SELECT 1 FROM "WorkflowTransitions" WHERE "WorkflowDefinitionId" = v_workflow_def_id AND "FromStatus" = 'ReschedulePendingApproval' AND "ToStatus" = 'Assigned' AND ("IsDeleted" = false OR "IsDeleted" IS NULL));

    INSERT INTO "WorkflowTransitions" ("Id", "CompanyId", "WorkflowDefinitionId", "FromStatus", "ToStatus", "AllowedRolesJson", "IsActive", "IsDeleted", "DisplayOrder", "CreatedAt", "UpdatedAt")
    SELECT gen_random_uuid(), v_company_id, v_workflow_def_id, 'ReschedulePendingApproval', 'Cancelled', '["Admin"]'::jsonb, true, false, 16, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
    WHERE NOT EXISTS (SELECT 1 FROM "WorkflowTransitions" WHERE "WorkflowDefinitionId" = v_workflow_def_id AND "FromStatus" = 'ReschedulePendingApproval' AND "ToStatus" = 'Cancelled' AND ("IsDeleted" = false OR "IsDeleted" IS NULL));

    -- Docket path
    INSERT INTO "WorkflowTransitions" ("Id", "CompanyId", "WorkflowDefinitionId", "FromStatus", "ToStatus", "AllowedRolesJson", "IsActive", "IsDeleted", "DisplayOrder", "CreatedAt", "UpdatedAt")
    SELECT gen_random_uuid(), v_company_id, v_workflow_def_id, 'OrderCompleted', 'DocketsReceived', '["Admin","Ops"]'::jsonb, true, false, 17, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
    WHERE NOT EXISTS (SELECT 1 FROM "WorkflowTransitions" WHERE "WorkflowDefinitionId" = v_workflow_def_id AND "FromStatus" = 'OrderCompleted' AND "ToStatus" = 'DocketsReceived' AND ("IsDeleted" = false OR "IsDeleted" IS NULL));

    INSERT INTO "WorkflowTransitions" ("Id", "CompanyId", "WorkflowDefinitionId", "FromStatus", "ToStatus", "AllowedRolesJson", "IsActive", "IsDeleted", "DisplayOrder", "CreatedAt", "UpdatedAt")
    SELECT gen_random_uuid(), v_company_id, v_workflow_def_id, 'DocketsReceived', 'DocketsVerified', '["Admin","Ops"]'::jsonb, true, false, 18, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
    WHERE NOT EXISTS (SELECT 1 FROM "WorkflowTransitions" WHERE "WorkflowDefinitionId" = v_workflow_def_id AND "FromStatus" = 'DocketsReceived' AND "ToStatus" = 'DocketsVerified' AND ("IsDeleted" = false OR "IsDeleted" IS NULL));

    INSERT INTO "WorkflowTransitions" ("Id", "CompanyId", "WorkflowDefinitionId", "FromStatus", "ToStatus", "AllowedRolesJson", "IsActive", "IsDeleted", "DisplayOrder", "CreatedAt", "UpdatedAt")
    SELECT gen_random_uuid(), v_company_id, v_workflow_def_id, 'DocketsReceived', 'DocketsRejected', '["Admin","Ops"]'::jsonb, true, false, 181, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
    WHERE NOT EXISTS (SELECT 1 FROM "WorkflowTransitions" WHERE "WorkflowDefinitionId" = v_workflow_def_id AND "FromStatus" = 'DocketsReceived' AND "ToStatus" = 'DocketsRejected' AND ("IsDeleted" = false OR "IsDeleted" IS NULL));

    INSERT INTO "WorkflowTransitions" ("Id", "CompanyId", "WorkflowDefinitionId", "FromStatus", "ToStatus", "AllowedRolesJson", "IsActive", "IsDeleted", "DisplayOrder", "CreatedAt", "UpdatedAt")
    SELECT gen_random_uuid(), v_company_id, v_workflow_def_id, 'DocketsRejected', 'DocketsReceived', '["Admin","Ops"]'::jsonb, true, false, 182, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
    WHERE NOT EXISTS (SELECT 1 FROM "WorkflowTransitions" WHERE "WorkflowDefinitionId" = v_workflow_def_id AND "FromStatus" = 'DocketsRejected' AND "ToStatus" = 'DocketsReceived' AND ("IsDeleted" = false OR "IsDeleted" IS NULL));

    INSERT INTO "WorkflowTransitions" ("Id", "CompanyId", "WorkflowDefinitionId", "FromStatus", "ToStatus", "AllowedRolesJson", "IsActive", "IsDeleted", "DisplayOrder", "CreatedAt", "UpdatedAt")
    SELECT gen_random_uuid(), v_company_id, v_workflow_def_id, 'DocketsVerified', 'DocketsUploaded', '["Admin","Ops"]'::jsonb, true, false, 19, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
    WHERE NOT EXISTS (SELECT 1 FROM "WorkflowTransitions" WHERE "WorkflowDefinitionId" = v_workflow_def_id AND "FromStatus" = 'DocketsVerified' AND "ToStatus" = 'DocketsUploaded' AND ("IsDeleted" = false OR "IsDeleted" IS NULL));

    INSERT INTO "WorkflowTransitions" ("Id", "CompanyId", "WorkflowDefinitionId", "FromStatus", "ToStatus", "AllowedRolesJson", "IsActive", "IsDeleted", "DisplayOrder", "CreatedAt", "UpdatedAt")
    SELECT gen_random_uuid(), v_company_id, v_workflow_def_id, 'DocketsUploaded', 'ReadyForInvoice', '["System","Admin"]'::jsonb, true, false, 20, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
    WHERE NOT EXISTS (SELECT 1 FROM "WorkflowTransitions" WHERE "WorkflowDefinitionId" = v_workflow_def_id AND "FromStatus" = 'DocketsUploaded' AND "ToStatus" = 'ReadyForInvoice' AND ("IsDeleted" = false OR "IsDeleted" IS NULL));

    -- Billing path
    INSERT INTO "WorkflowTransitions" ("Id", "CompanyId", "WorkflowDefinitionId", "FromStatus", "ToStatus", "AllowedRolesJson", "IsActive", "IsDeleted", "DisplayOrder", "CreatedAt", "UpdatedAt")
    SELECT gen_random_uuid(), v_company_id, v_workflow_def_id, 'ReadyForInvoice', 'Invoiced', '["Admin","Ops"]'::jsonb, true, false, 21, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
    WHERE NOT EXISTS (SELECT 1 FROM "WorkflowTransitions" WHERE "WorkflowDefinitionId" = v_workflow_def_id AND "FromStatus" = 'ReadyForInvoice' AND "ToStatus" = 'Invoiced' AND ("IsDeleted" = false OR "IsDeleted" IS NULL));

    INSERT INTO "WorkflowTransitions" ("Id", "CompanyId", "WorkflowDefinitionId", "FromStatus", "ToStatus", "AllowedRolesJson", "IsActive", "IsDeleted", "DisplayOrder", "CreatedAt", "UpdatedAt")
    SELECT gen_random_uuid(), v_company_id, v_workflow_def_id, 'Invoiced', 'SubmittedToPortal', '["Admin","Ops","System"]'::jsonb, true, false, 22, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
    WHERE NOT EXISTS (SELECT 1 FROM "WorkflowTransitions" WHERE "WorkflowDefinitionId" = v_workflow_def_id AND "FromStatus" = 'Invoiced' AND "ToStatus" = 'SubmittedToPortal' AND ("IsDeleted" = false OR "IsDeleted" IS NULL));

    INSERT INTO "WorkflowTransitions" ("Id", "CompanyId", "WorkflowDefinitionId", "FromStatus", "ToStatus", "AllowedRolesJson", "IsActive", "IsDeleted", "DisplayOrder", "CreatedAt", "UpdatedAt")
    SELECT gen_random_uuid(), v_company_id, v_workflow_def_id, 'SubmittedToPortal', 'Completed', '["System","Admin"]'::jsonb, true, false, 23, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
    WHERE NOT EXISTS (SELECT 1 FROM "WorkflowTransitions" WHERE "WorkflowDefinitionId" = v_workflow_def_id AND "FromStatus" = 'SubmittedToPortal' AND "ToStatus" = 'Completed' AND ("IsDeleted" = false OR "IsDeleted" IS NULL));

    -- Invoice rejection loop
    INSERT INTO "WorkflowTransitions" ("Id", "CompanyId", "WorkflowDefinitionId", "FromStatus", "ToStatus", "AllowedRolesJson", "IsActive", "IsDeleted", "DisplayOrder", "CreatedAt", "UpdatedAt")
    SELECT gen_random_uuid(), v_company_id, v_workflow_def_id, 'Invoiced', 'Rejected', '["System","Ops","Admin"]'::jsonb, true, false, 100, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
    WHERE NOT EXISTS (SELECT 1 FROM "WorkflowTransitions" WHERE "WorkflowDefinitionId" = v_workflow_def_id AND "FromStatus" = 'Invoiced' AND "ToStatus" = 'Rejected' AND ("IsDeleted" = false OR "IsDeleted" IS NULL));

    INSERT INTO "WorkflowTransitions" ("Id", "CompanyId", "WorkflowDefinitionId", "FromStatus", "ToStatus", "AllowedRolesJson", "IsActive", "IsDeleted", "DisplayOrder", "CreatedAt", "UpdatedAt")
    SELECT gen_random_uuid(), v_company_id, v_workflow_def_id, 'SubmittedToPortal', 'Rejected', '["System","Ops","Admin"]'::jsonb, true, false, 101, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
    WHERE NOT EXISTS (SELECT 1 FROM "WorkflowTransitions" WHERE "WorkflowDefinitionId" = v_workflow_def_id AND "FromStatus" = 'SubmittedToPortal' AND "ToStatus" = 'Rejected' AND ("IsDeleted" = false OR "IsDeleted" IS NULL));

    INSERT INTO "WorkflowTransitions" ("Id", "CompanyId", "WorkflowDefinitionId", "FromStatus", "ToStatus", "AllowedRolesJson", "IsActive", "IsDeleted", "DisplayOrder", "CreatedAt", "UpdatedAt")
    SELECT gen_random_uuid(), v_company_id, v_workflow_def_id, 'Rejected', 'ReadyForInvoice', '["Ops","Admin"]'::jsonb, true, false, 102, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
    WHERE NOT EXISTS (SELECT 1 FROM "WorkflowTransitions" WHERE "WorkflowDefinitionId" = v_workflow_def_id AND "FromStatus" = 'Rejected' AND "ToStatus" = 'ReadyForInvoice' AND ("IsDeleted" = false OR "IsDeleted" IS NULL));

    INSERT INTO "WorkflowTransitions" ("Id", "CompanyId", "WorkflowDefinitionId", "FromStatus", "ToStatus", "AllowedRolesJson", "IsActive", "IsDeleted", "DisplayOrder", "CreatedAt", "UpdatedAt")
    SELECT gen_random_uuid(), v_company_id, v_workflow_def_id, 'Rejected', 'Reinvoice', '["Ops","Admin"]'::jsonb, true, false, 103, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
    WHERE NOT EXISTS (SELECT 1 FROM "WorkflowTransitions" WHERE "WorkflowDefinitionId" = v_workflow_def_id AND "FromStatus" = 'Rejected' AND "ToStatus" = 'Reinvoice' AND ("IsDeleted" = false OR "IsDeleted" IS NULL));

    INSERT INTO "WorkflowTransitions" ("Id", "CompanyId", "WorkflowDefinitionId", "FromStatus", "ToStatus", "AllowedRolesJson", "IsActive", "IsDeleted", "DisplayOrder", "CreatedAt", "UpdatedAt")
    SELECT gen_random_uuid(), v_company_id, v_workflow_def_id, 'Reinvoice', 'Invoiced', '["Ops","Admin"]'::jsonb, true, false, 104, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
    WHERE NOT EXISTS (SELECT 1 FROM "WorkflowTransitions" WHERE "WorkflowDefinitionId" = v_workflow_def_id AND "FromStatus" = 'Reinvoice' AND "ToStatus" = 'Invoiced' AND ("IsDeleted" = false OR "IsDeleted" IS NULL));

    RAISE NOTICE 'Order workflow seed complete. Inserted definitions: %. Re-run 00_check_workflows.sql to verify transitions.', v_inserted_def;
END $$;
