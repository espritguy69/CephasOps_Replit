-- Add createInstallerTask side effect to Pending -> Assigned transition.
-- Idempotent: merges into existing SideEffectsConfigJson.
-- Run after 07_gpon_order_workflow.sql (or after create-order-workflow-if-missing.sql).

DO $$
DECLARE
    v_company_id UUID;
    v_workflow_def_id UUID;
    v_transition_id UUID;
    v_current_config jsonb;
    v_new_config jsonb;
BEGIN
    SELECT "Id" INTO v_company_id FROM "Companies" LIMIT 1;
    IF v_company_id IS NULL THEN
        RAISE NOTICE 'No company found, skipping';
        RETURN;
    END IF;

    SELECT "Id" INTO v_workflow_def_id
    FROM "WorkflowDefinitions"
    WHERE "EntityType" = 'Order' AND "IsActive" = true AND "IsDeleted" = false
    ORDER BY "CreatedAt" DESC LIMIT 1;

    IF v_workflow_def_id IS NULL THEN
        RAISE NOTICE 'No Order workflow definition found, skipping';
        RETURN;
    END IF;

    SELECT "Id", "SideEffectsConfigJson"
    INTO v_transition_id, v_current_config
    FROM "WorkflowTransitions"
    WHERE "WorkflowDefinitionId" = v_workflow_def_id
      AND "FromStatus" = 'Pending' AND "ToStatus" = 'Assigned'
      AND "IsDeleted" = false
    LIMIT 1;

    IF v_transition_id IS NULL THEN
        RAISE NOTICE 'Pending -> Assigned transition not found, skipping';
        RETURN;
    END IF;

    IF v_current_config IS NULL THEN
        v_current_config := '{}'::jsonb;
    END IF;
    v_new_config := v_current_config || jsonb_build_object('createInstallerTask', true);

    IF v_new_config IS DISTINCT FROM v_current_config THEN
        UPDATE "WorkflowTransitions"
        SET "SideEffectsConfigJson" = v_new_config, "UpdatedAt" = CURRENT_TIMESTAMP
        WHERE "Id" = v_transition_id;
        RAISE NOTICE 'Updated WorkflowTransition % to include createInstallerTask', v_transition_id;
    ELSE
        RAISE NOTICE 'WorkflowTransition % already has createInstallerTask configured', v_transition_id;
    END IF;
END $$;
