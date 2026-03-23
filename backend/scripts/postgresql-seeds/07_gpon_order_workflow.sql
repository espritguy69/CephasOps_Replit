-- ============================================
-- 07 GPON Order Workflow - Full lifecycle transitions
-- ============================================
-- Idempotent. Creates WorkflowDefinition + all GPON order transitions.
-- Run after 01-06. Required for fresh DB bootstrap.
-- See: docs/business/order_lifecycle_and_statuses.md
-- ============================================

DO $$
DECLARE
    v_workflow_def_id UUID;
    v_company_id UUID;
    v_exists BOOLEAN;
    v_order INT := 0;
BEGIN
    SELECT "Id" INTO v_company_id FROM "Companies" LIMIT 1;
    IF v_company_id IS NULL THEN
        v_company_id := '00000000-0000-0000-0000-000000000000'::uuid;
    END IF;

    -- 1. Create or get WorkflowDefinition
    -- Try with IsDeleted first, fall back without it
    BEGIN
        EXECUTE 'SELECT "Id" FROM "WorkflowDefinitions" WHERE "EntityType" = ''Order'' AND "IsActive" = true AND "IsDeleted" = false ORDER BY "CreatedAt" DESC LIMIT 1'
            INTO v_workflow_def_id;
    EXCEPTION WHEN undefined_column THEN
        SELECT "Id" INTO v_workflow_def_id
        FROM "WorkflowDefinitions"
        WHERE "EntityType" = 'Order' AND "IsActive" = true
        ORDER BY "CreatedAt" DESC LIMIT 1;
    END;

    IF v_workflow_def_id IS NULL THEN
        v_workflow_def_id := gen_random_uuid();
        -- Insert without IsDeleted if column doesn't exist
        BEGIN
            INSERT INTO "WorkflowDefinitions" ("Id", "CompanyId", "Name", "EntityType", "Description", "IsActive", "IsDeleted", "CreatedAt", "UpdatedAt")
            VALUES (v_workflow_def_id, v_company_id, 'Order Workflow', 'Order', 'GPON order lifecycle - full transitions', true, false, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);
        EXCEPTION WHEN undefined_column THEN
            INSERT INTO "WorkflowDefinitions" ("Id", "CompanyId", "Name", "EntityType", "Description", "IsActive", "CreatedAt", "UpdatedAt")
            VALUES (v_workflow_def_id, v_company_id, 'Order Workflow', 'Order', 'GPON order lifecycle - full transitions', true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);
        END;
        RAISE NOTICE 'Created WorkflowDefinition: %', v_workflow_def_id;
    ELSE
        RAISE NOTICE 'Using existing WorkflowDefinition: %', v_workflow_def_id;
    END IF;

    -- 2. Main flow transitions (idempotent insert)
    -- Pending
    PERFORM 1 FROM "WorkflowTransitions" WHERE "WorkflowDefinitionId" = v_workflow_def_id AND "FromStatus" = 'Pending' AND "ToStatus" = 'Assigned' ;
    IF NOT FOUND THEN
        INSERT INTO "WorkflowTransitions" ("Id", "CompanyId", "WorkflowDefinitionId", "FromStatus", "ToStatus", "AllowedRolesJson", "SideEffectsConfigJson", "IsActive", "DisplayOrder", "CreatedAt", "UpdatedAt")
        VALUES (gen_random_uuid(), v_company_id, v_workflow_def_id, 'Pending', 'Assigned', '["Admin","Scheduler","Manager"]'::jsonb, '{"checkMaterialCollection":true}'::jsonb, true, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);
    END IF;
    PERFORM 1 FROM "WorkflowTransitions" WHERE "WorkflowDefinitionId" = v_workflow_def_id AND "FromStatus" = 'Pending' AND "ToStatus" = 'Cancelled' ;
    IF NOT FOUND THEN
        INSERT INTO "WorkflowTransitions" ("Id", "CompanyId", "WorkflowDefinitionId", "FromStatus", "ToStatus", "AllowedRolesJson", "IsActive", "DisplayOrder", "CreatedAt", "UpdatedAt")
        VALUES (gen_random_uuid(), v_company_id, v_workflow_def_id, 'Pending', 'Cancelled', '["Admin","Ops"]'::jsonb, true, 2, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);
    END IF;

    -- Assigned
    PERFORM 1 FROM "WorkflowTransitions" WHERE "WorkflowDefinitionId" = v_workflow_def_id AND "FromStatus" = 'Assigned' AND "ToStatus" = 'OnTheWay' ;
    IF NOT FOUND THEN
        INSERT INTO "WorkflowTransitions" ("Id", "CompanyId", "WorkflowDefinitionId", "FromStatus", "ToStatus", "AllowedRolesJson", "IsActive", "DisplayOrder", "CreatedAt", "UpdatedAt")
        VALUES (gen_random_uuid(), v_company_id, v_workflow_def_id, 'Assigned', 'OnTheWay', '["SI","Admin"]'::jsonb, true, 3, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);
    END IF;
    PERFORM 1 FROM "WorkflowTransitions" WHERE "WorkflowDefinitionId" = v_workflow_def_id AND "FromStatus" = 'Assigned' AND "ToStatus" = 'Blocker' ;
    IF NOT FOUND THEN
        INSERT INTO "WorkflowTransitions" ("Id", "CompanyId", "WorkflowDefinitionId", "FromStatus", "ToStatus", "AllowedRolesJson", "IsActive", "DisplayOrder", "CreatedAt", "UpdatedAt")
        VALUES (gen_random_uuid(), v_company_id, v_workflow_def_id, 'Assigned', 'Blocker', '["SI","Admin","Ops"]'::jsonb, true, 4, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);
    END IF;
    PERFORM 1 FROM "WorkflowTransitions" WHERE "WorkflowDefinitionId" = v_workflow_def_id AND "FromStatus" = 'Assigned' AND "ToStatus" = 'ReschedulePendingApproval' ;
    IF NOT FOUND THEN
        INSERT INTO "WorkflowTransitions" ("Id", "CompanyId", "WorkflowDefinitionId", "FromStatus", "ToStatus", "AllowedRolesJson", "IsActive", "DisplayOrder", "CreatedAt", "UpdatedAt")
        VALUES (gen_random_uuid(), v_company_id, v_workflow_def_id, 'Assigned', 'ReschedulePendingApproval', '["Admin","Scheduler"]'::jsonb, true, 5, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);
    END IF;
    PERFORM 1 FROM "WorkflowTransitions" WHERE "WorkflowDefinitionId" = v_workflow_def_id AND "FromStatus" = 'Assigned' AND "ToStatus" = 'Cancelled' ;
    IF NOT FOUND THEN
        INSERT INTO "WorkflowTransitions" ("Id", "CompanyId", "WorkflowDefinitionId", "FromStatus", "ToStatus", "AllowedRolesJson", "IsActive", "DisplayOrder", "CreatedAt", "UpdatedAt")
        VALUES (gen_random_uuid(), v_company_id, v_workflow_def_id, 'Assigned', 'Cancelled', '["Admin","Ops"]'::jsonb, true, 6, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);
    END IF;

    -- OnTheWay
    PERFORM 1 FROM "WorkflowTransitions" WHERE "WorkflowDefinitionId" = v_workflow_def_id AND "FromStatus" = 'OnTheWay' AND "ToStatus" = 'MetCustomer' ;
    IF NOT FOUND THEN
        INSERT INTO "WorkflowTransitions" ("Id", "CompanyId", "WorkflowDefinitionId", "FromStatus", "ToStatus", "AllowedRolesJson", "IsActive", "DisplayOrder", "CreatedAt", "UpdatedAt")
        VALUES (gen_random_uuid(), v_company_id, v_workflow_def_id, 'OnTheWay', 'MetCustomer', '["SI","Admin"]'::jsonb, true, 7, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);
    END IF;
    PERFORM 1 FROM "WorkflowTransitions" WHERE "WorkflowDefinitionId" = v_workflow_def_id AND "FromStatus" = 'OnTheWay' AND "ToStatus" = 'Blocker' ;
    IF NOT FOUND THEN
        INSERT INTO "WorkflowTransitions" ("Id", "CompanyId", "WorkflowDefinitionId", "FromStatus", "ToStatus", "AllowedRolesJson", "IsActive", "DisplayOrder", "CreatedAt", "UpdatedAt")
        VALUES (gen_random_uuid(), v_company_id, v_workflow_def_id, 'OnTheWay', 'Blocker', '["SI","Admin","Ops"]'::jsonb, true, 8, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);
    END IF;

    -- MetCustomer
    PERFORM 1 FROM "WorkflowTransitions" WHERE "WorkflowDefinitionId" = v_workflow_def_id AND "FromStatus" = 'MetCustomer' AND "ToStatus" = 'OrderCompleted' ;
    IF NOT FOUND THEN
        INSERT INTO "WorkflowTransitions" ("Id", "CompanyId", "WorkflowDefinitionId", "FromStatus", "ToStatus", "AllowedRolesJson", "IsActive", "DisplayOrder", "CreatedAt", "UpdatedAt")
        VALUES (gen_random_uuid(), v_company_id, v_workflow_def_id, 'MetCustomer', 'OrderCompleted', '["SI","Admin"]'::jsonb, true, 9, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);
    END IF;
    PERFORM 1 FROM "WorkflowTransitions" WHERE "WorkflowDefinitionId" = v_workflow_def_id AND "FromStatus" = 'MetCustomer' AND "ToStatus" = 'Blocker' ;
    IF NOT FOUND THEN
        INSERT INTO "WorkflowTransitions" ("Id", "CompanyId", "WorkflowDefinitionId", "FromStatus", "ToStatus", "AllowedRolesJson", "IsActive", "DisplayOrder", "CreatedAt", "UpdatedAt")
        VALUES (gen_random_uuid(), v_company_id, v_workflow_def_id, 'MetCustomer', 'Blocker', '["SI","Admin","Ops"]'::jsonb, true, 10, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);
    END IF;

    -- Blocker exits (MetCustomer, Assigned, ReschedulePendingApproval, Cancelled)
    PERFORM 1 FROM "WorkflowTransitions" WHERE "WorkflowDefinitionId" = v_workflow_def_id AND "FromStatus" = 'Blocker' AND "ToStatus" = 'MetCustomer' ;
    IF NOT FOUND THEN
        INSERT INTO "WorkflowTransitions" ("Id", "CompanyId", "WorkflowDefinitionId", "FromStatus", "ToStatus", "AllowedRolesJson", "IsActive", "DisplayOrder", "CreatedAt", "UpdatedAt")
        VALUES (gen_random_uuid(), v_company_id, v_workflow_def_id, 'Blocker', 'MetCustomer', '["SI","Admin","Ops"]'::jsonb, true, 11, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);
    END IF;
    PERFORM 1 FROM "WorkflowTransitions" WHERE "WorkflowDefinitionId" = v_workflow_def_id AND "FromStatus" = 'Blocker' AND "ToStatus" = 'Assigned' ;
    IF NOT FOUND THEN
        INSERT INTO "WorkflowTransitions" ("Id", "CompanyId", "WorkflowDefinitionId", "FromStatus", "ToStatus", "AllowedRolesJson", "IsActive", "DisplayOrder", "CreatedAt", "UpdatedAt")
        VALUES (gen_random_uuid(), v_company_id, v_workflow_def_id, 'Blocker', 'Assigned', '["Ops","Admin","HOD"]'::jsonb, true, 12, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);
    END IF;
    PERFORM 1 FROM "WorkflowTransitions" WHERE "WorkflowDefinitionId" = v_workflow_def_id AND "FromStatus" = 'Blocker' AND "ToStatus" = 'ReschedulePendingApproval' ;
    IF NOT FOUND THEN
        INSERT INTO "WorkflowTransitions" ("Id", "CompanyId", "WorkflowDefinitionId", "FromStatus", "ToStatus", "AllowedRolesJson", "IsActive", "DisplayOrder", "CreatedAt", "UpdatedAt")
        VALUES (gen_random_uuid(), v_company_id, v_workflow_def_id, 'Blocker', 'ReschedulePendingApproval', '["Admin","Ops"]'::jsonb, true, 13, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);
    END IF;
    PERFORM 1 FROM "WorkflowTransitions" WHERE "WorkflowDefinitionId" = v_workflow_def_id AND "FromStatus" = 'Blocker' AND "ToStatus" = 'Cancelled' ;
    IF NOT FOUND THEN
        INSERT INTO "WorkflowTransitions" ("Id", "CompanyId", "WorkflowDefinitionId", "FromStatus", "ToStatus", "AllowedRolesJson", "IsActive", "DisplayOrder", "CreatedAt", "UpdatedAt")
        VALUES (gen_random_uuid(), v_company_id, v_workflow_def_id, 'Blocker', 'Cancelled', '["Admin","Ops"]'::jsonb, true, 14, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);
    END IF;

    -- ReschedulePendingApproval
    PERFORM 1 FROM "WorkflowTransitions" WHERE "WorkflowDefinitionId" = v_workflow_def_id AND "FromStatus" = 'ReschedulePendingApproval' AND "ToStatus" = 'Assigned' ;
    IF NOT FOUND THEN
        INSERT INTO "WorkflowTransitions" ("Id", "CompanyId", "WorkflowDefinitionId", "FromStatus", "ToStatus", "AllowedRolesJson", "IsActive", "DisplayOrder", "CreatedAt", "UpdatedAt")
        VALUES (gen_random_uuid(), v_company_id, v_workflow_def_id, 'ReschedulePendingApproval', 'Assigned', '["Admin","Scheduler"]'::jsonb, true, 15, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);
    END IF;
    PERFORM 1 FROM "WorkflowTransitions" WHERE "WorkflowDefinitionId" = v_workflow_def_id AND "FromStatus" = 'ReschedulePendingApproval' AND "ToStatus" = 'Cancelled' ;
    IF NOT FOUND THEN
        INSERT INTO "WorkflowTransitions" ("Id", "CompanyId", "WorkflowDefinitionId", "FromStatus", "ToStatus", "AllowedRolesJson", "IsActive", "DisplayOrder", "CreatedAt", "UpdatedAt")
        VALUES (gen_random_uuid(), v_company_id, v_workflow_def_id, 'ReschedulePendingApproval', 'Cancelled', '["Admin"]'::jsonb, true, 16, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);
    END IF;

    -- Docket path
    PERFORM 1 FROM "WorkflowTransitions" WHERE "WorkflowDefinitionId" = v_workflow_def_id AND "FromStatus" = 'OrderCompleted' AND "ToStatus" = 'DocketsReceived' ;
    IF NOT FOUND THEN
        INSERT INTO "WorkflowTransitions" ("Id", "CompanyId", "WorkflowDefinitionId", "FromStatus", "ToStatus", "AllowedRolesJson", "IsActive", "DisplayOrder", "CreatedAt", "UpdatedAt")
        VALUES (gen_random_uuid(), v_company_id, v_workflow_def_id, 'OrderCompleted', 'DocketsReceived', '["Admin","Ops"]'::jsonb, true, 17, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);
    END IF;
    PERFORM 1 FROM "WorkflowTransitions" WHERE "WorkflowDefinitionId" = v_workflow_def_id AND "FromStatus" = 'DocketsReceived' AND "ToStatus" = 'DocketsVerified' ;
    IF NOT FOUND THEN
        INSERT INTO "WorkflowTransitions" ("Id", "CompanyId", "WorkflowDefinitionId", "FromStatus", "ToStatus", "AllowedRolesJson", "IsActive", "DisplayOrder", "CreatedAt", "UpdatedAt")
        VALUES (gen_random_uuid(), v_company_id, v_workflow_def_id, 'DocketsReceived', 'DocketsVerified', '["Admin","Ops"]'::jsonb, true, 18, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);
    END IF;
    -- Docket rejection: DocketsReceived -> DocketsRejected (admin rejects with reason)
    PERFORM 1 FROM "WorkflowTransitions" WHERE "WorkflowDefinitionId" = v_workflow_def_id AND "FromStatus" = 'DocketsReceived' AND "ToStatus" = 'DocketsRejected' ;
    IF NOT FOUND THEN
        INSERT INTO "WorkflowTransitions" ("Id", "CompanyId", "WorkflowDefinitionId", "FromStatus", "ToStatus", "AllowedRolesJson", "IsActive", "DisplayOrder", "CreatedAt", "UpdatedAt")
        VALUES (gen_random_uuid(), v_company_id, v_workflow_def_id, 'DocketsReceived', 'DocketsRejected', '["Admin","Ops"]'::jsonb, true, 181, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);
    END IF;
    -- Accept corrected docket: DocketsRejected -> DocketsReceived
    PERFORM 1 FROM "WorkflowTransitions" WHERE "WorkflowDefinitionId" = v_workflow_def_id AND "FromStatus" = 'DocketsRejected' AND "ToStatus" = 'DocketsReceived' ;
    IF NOT FOUND THEN
        INSERT INTO "WorkflowTransitions" ("Id", "CompanyId", "WorkflowDefinitionId", "FromStatus", "ToStatus", "AllowedRolesJson", "IsActive", "DisplayOrder", "CreatedAt", "UpdatedAt")
        VALUES (gen_random_uuid(), v_company_id, v_workflow_def_id, 'DocketsRejected', 'DocketsReceived', '["Admin","Ops"]'::jsonb, true, 182, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);
    END IF;
    PERFORM 1 FROM "WorkflowTransitions" WHERE "WorkflowDefinitionId" = v_workflow_def_id AND "FromStatus" = 'DocketsVerified' AND "ToStatus" = 'DocketsUploaded' ;
    IF NOT FOUND THEN
        INSERT INTO "WorkflowTransitions" ("Id", "CompanyId", "WorkflowDefinitionId", "FromStatus", "ToStatus", "AllowedRolesJson", "IsActive", "DisplayOrder", "CreatedAt", "UpdatedAt")
        VALUES (gen_random_uuid(), v_company_id, v_workflow_def_id, 'DocketsVerified', 'DocketsUploaded', '["Admin","Ops"]'::jsonb, true, 19, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);
    END IF;
    PERFORM 1 FROM "WorkflowTransitions" WHERE "WorkflowDefinitionId" = v_workflow_def_id AND "FromStatus" = 'DocketsUploaded' AND "ToStatus" = 'ReadyForInvoice' ;
    IF NOT FOUND THEN
        INSERT INTO "WorkflowTransitions" ("Id", "CompanyId", "WorkflowDefinitionId", "FromStatus", "ToStatus", "AllowedRolesJson", "IsActive", "DisplayOrder", "CreatedAt", "UpdatedAt")
        VALUES (gen_random_uuid(), v_company_id, v_workflow_def_id, 'DocketsUploaded', 'ReadyForInvoice', '["System","Admin"]'::jsonb, true, 20, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);
    END IF;

    -- Billing path
    PERFORM 1 FROM "WorkflowTransitions" WHERE "WorkflowDefinitionId" = v_workflow_def_id AND "FromStatus" = 'ReadyForInvoice' AND "ToStatus" = 'Invoiced' ;
    IF NOT FOUND THEN
        INSERT INTO "WorkflowTransitions" ("Id", "CompanyId", "WorkflowDefinitionId", "FromStatus", "ToStatus", "AllowedRolesJson", "IsActive", "DisplayOrder", "CreatedAt", "UpdatedAt")
        VALUES (gen_random_uuid(), v_company_id, v_workflow_def_id, 'ReadyForInvoice', 'Invoiced', '["Admin","Ops"]'::jsonb, true, 21, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);
    END IF;
    PERFORM 1 FROM "WorkflowTransitions" WHERE "WorkflowDefinitionId" = v_workflow_def_id AND "FromStatus" = 'Invoiced' AND "ToStatus" = 'SubmittedToPortal' ;
    IF NOT FOUND THEN
        INSERT INTO "WorkflowTransitions" ("Id", "CompanyId", "WorkflowDefinitionId", "FromStatus", "ToStatus", "AllowedRolesJson", "IsActive", "DisplayOrder", "CreatedAt", "UpdatedAt")
        VALUES (gen_random_uuid(), v_company_id, v_workflow_def_id, 'Invoiced', 'SubmittedToPortal', '["Admin","Ops","System"]'::jsonb, true, 22, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);
    END IF;
    PERFORM 1 FROM "WorkflowTransitions" WHERE "WorkflowDefinitionId" = v_workflow_def_id AND "FromStatus" = 'SubmittedToPortal' AND "ToStatus" = 'Completed' ;
    IF NOT FOUND THEN
        INSERT INTO "WorkflowTransitions" ("Id", "CompanyId", "WorkflowDefinitionId", "FromStatus", "ToStatus", "AllowedRolesJson", "IsActive", "DisplayOrder", "CreatedAt", "UpdatedAt")
        VALUES (gen_random_uuid(), v_company_id, v_workflow_def_id, 'SubmittedToPortal', 'Completed', '["System","Admin"]'::jsonb, true, 23, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);
    END IF;

    -- Invoice rejection loop (Rejected = InvoiceRejected display; code uses Rejected for backward compat)
    PERFORM 1 FROM "WorkflowTransitions" WHERE "WorkflowDefinitionId" = v_workflow_def_id AND "FromStatus" = 'Invoiced' AND "ToStatus" = 'Rejected' ;
    IF NOT FOUND THEN
        INSERT INTO "WorkflowTransitions" ("Id", "CompanyId", "WorkflowDefinitionId", "FromStatus", "ToStatus", "AllowedRolesJson", "IsActive", "DisplayOrder", "CreatedAt", "UpdatedAt")
        VALUES (gen_random_uuid(), v_company_id, v_workflow_def_id, 'Invoiced', 'Rejected', '["System","Ops","Admin"]'::jsonb, true, 100, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);
    END IF;
    PERFORM 1 FROM "WorkflowTransitions" WHERE "WorkflowDefinitionId" = v_workflow_def_id AND "FromStatus" = 'SubmittedToPortal' AND "ToStatus" = 'Rejected' ;
    IF NOT FOUND THEN
        INSERT INTO "WorkflowTransitions" ("Id", "CompanyId", "WorkflowDefinitionId", "FromStatus", "ToStatus", "AllowedRolesJson", "IsActive", "DisplayOrder", "CreatedAt", "UpdatedAt")
        VALUES (gen_random_uuid(), v_company_id, v_workflow_def_id, 'SubmittedToPortal', 'Rejected', '["System","Ops","Admin"]'::jsonb, true, 101, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);
    END IF;
    PERFORM 1 FROM "WorkflowTransitions" WHERE "WorkflowDefinitionId" = v_workflow_def_id AND "FromStatus" = 'Rejected' AND "ToStatus" = 'ReadyForInvoice' ;
    IF NOT FOUND THEN
        INSERT INTO "WorkflowTransitions" ("Id", "CompanyId", "WorkflowDefinitionId", "FromStatus", "ToStatus", "AllowedRolesJson", "IsActive", "DisplayOrder", "CreatedAt", "UpdatedAt")
        VALUES (gen_random_uuid(), v_company_id, v_workflow_def_id, 'Rejected', 'ReadyForInvoice', '["Ops","Admin"]'::jsonb, true, 102, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);
    END IF;
    PERFORM 1 FROM "WorkflowTransitions" WHERE "WorkflowDefinitionId" = v_workflow_def_id AND "FromStatus" = 'Rejected' AND "ToStatus" = 'Reinvoice' ;
    IF NOT FOUND THEN
        INSERT INTO "WorkflowTransitions" ("Id", "CompanyId", "WorkflowDefinitionId", "FromStatus", "ToStatus", "AllowedRolesJson", "IsActive", "DisplayOrder", "CreatedAt", "UpdatedAt")
        VALUES (gen_random_uuid(), v_company_id, v_workflow_def_id, 'Rejected', 'Reinvoice', '["Ops","Admin"]'::jsonb, true, 103, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);
    END IF;
    PERFORM 1 FROM "WorkflowTransitions" WHERE "WorkflowDefinitionId" = v_workflow_def_id AND "FromStatus" = 'Reinvoice' AND "ToStatus" = 'Invoiced' ;
    IF NOT FOUND THEN
        INSERT INTO "WorkflowTransitions" ("Id", "CompanyId", "WorkflowDefinitionId", "FromStatus", "ToStatus", "AllowedRolesJson", "IsActive", "DisplayOrder", "CreatedAt", "UpdatedAt")
        VALUES (gen_random_uuid(), v_company_id, v_workflow_def_id, 'Reinvoice', 'Invoiced', '["Ops","Admin"]'::jsonb, true, 104, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);
    END IF;

    RAISE NOTICE 'GPON Order Workflow ready: all transitions seeded.';
END $$;
