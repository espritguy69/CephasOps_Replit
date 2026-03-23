-- Remove createInstallerTask from Pending -> Assigned transition.
-- Use after moving installer task creation to OrderAssignedEvent handler (event-driven operations).
-- Idempotent: removes key if present.

DO $$
DECLARE
    v_workflow_def_id UUID;
    v_transition_id UUID;
    v_current_config jsonb;
    v_new_config jsonb;
BEGIN
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
    v_new_config := v_current_config - 'createInstallerTask';

    IF v_new_config IS DISTINCT FROM v_current_config THEN
        UPDATE "WorkflowTransitions"
        SET "SideEffectsConfigJson" = v_new_config, "UpdatedAt" = CURRENT_TIMESTAMP
        WHERE "Id" = v_transition_id;
        RAISE NOTICE 'Removed createInstallerTask from WorkflowTransition % (event-driven path is canonical)', v_transition_id;
    ELSE
        RAISE NOTICE 'WorkflowTransition % already has no createInstallerTask', v_transition_id;
    END IF;
END $$;
