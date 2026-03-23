-- Create Order Workflow Definition if it doesn't exist
-- This is needed for the material collection side effect to work

DO $$
DECLARE
    v_workflow_def_id UUID;
    v_company_id UUID;
    v_transition_id UUID;
    v_current_config JSONB;
    v_new_config JSONB;
BEGIN
    -- Get first company ID
    SELECT "Id" INTO v_company_id
    FROM "Companies"
    LIMIT 1;
    
    IF v_company_id IS NULL THEN
        v_company_id := '00000000-0000-0000-0000-000000000000'::uuid;
    END IF;
    
    -- Check if workflow definition exists
    SELECT "Id" INTO v_workflow_def_id
    FROM "WorkflowDefinitions"
    WHERE "EntityType" = 'Order'
      AND "IsActive" = true
      AND "IsDeleted" = false
    ORDER BY "CreatedAt" DESC
    LIMIT 1;
    
    IF v_workflow_def_id IS NULL THEN
        -- Create workflow definition
        v_workflow_def_id := gen_random_uuid();
        INSERT INTO "WorkflowDefinitions" (
            "Id", "CompanyId", "Name", "EntityType", "Description", 
            "IsActive", "IsDeleted", "CreatedAt", "UpdatedAt"
        )
        VALUES (
            v_workflow_def_id,
            v_company_id,
            'Order Workflow',
            'Order',
            'Default workflow for Order entity',
            true,
            false,
            CURRENT_TIMESTAMP,
            CURRENT_TIMESTAMP
        );
        RAISE NOTICE 'Created WorkflowDefinition: %', v_workflow_def_id;
    ELSE
        RAISE NOTICE 'Using existing WorkflowDefinition: %', v_workflow_def_id;
    END IF;
    
    -- Now find or create the Pending -> Assigned transition
    SELECT "Id", "SideEffectsConfigJson" INTO v_transition_id, v_current_config
    FROM "WorkflowTransitions"
    WHERE "WorkflowDefinitionId" = v_workflow_def_id
      AND ("FromStatus" IS NULL OR "FromStatus" = 'Pending')
      AND "ToStatus" = 'Assigned'
      AND "IsActive" = true
      AND "IsDeleted" = false
    LIMIT 1;
    
    IF v_transition_id IS NULL THEN
        -- Create the transition
        v_transition_id := gen_random_uuid();
        v_new_config := jsonb_build_object('checkMaterialCollection', true);
        
        INSERT INTO "WorkflowTransitions" (
            "Id", "CompanyId", "WorkflowDefinitionId", "FromStatus", "ToStatus",
            "AllowedRolesJson", "GuardConditionsJson", "SideEffectsConfigJson",
            "IsActive", "IsDeleted", "DisplayOrder", "CreatedAt", "UpdatedAt"
        )
        VALUES (
            v_transition_id,
            v_company_id,
            v_workflow_def_id,
            'Pending',
            'Assigned',
            '["Admin", "Scheduler", "Manager"]'::jsonb,
            NULL,
            v_new_config,
            true,
            false,
            1,
            CURRENT_TIMESTAMP,
            CURRENT_TIMESTAMP
        );
        RAISE NOTICE 'Created WorkflowTransition: % (Pending -> Assigned)', v_transition_id;
    ELSE
        -- Update existing transition to include material collection
        IF v_current_config IS NULL THEN
            v_current_config := '{}'::jsonb;
        END IF;
        
        v_new_config := v_current_config || jsonb_build_object('checkMaterialCollection', true);
        
        IF v_new_config != v_current_config THEN
            UPDATE "WorkflowTransitions"
            SET 
                "SideEffectsConfigJson" = v_new_config,
                "UpdatedAt" = CURRENT_TIMESTAMP
            WHERE "Id" = v_transition_id;
            RAISE NOTICE 'Updated WorkflowTransition % to include checkMaterialCollection', v_transition_id;
        ELSE
            RAISE NOTICE 'WorkflowTransition % already has checkMaterialCollection configured', v_transition_id;
        END IF;
    END IF;
    
    RAISE NOTICE '';
    RAISE NOTICE 'Order Workflow is now configured with material collection side effect';
    RAISE NOTICE '';
END $$;

