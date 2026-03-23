-- SQL Script to Activate Material Collection Alert System
-- Run this script in your PostgreSQL database

-- Step 1: Create Side Effect Definition
DO $$
DECLARE
    v_side_effect_id UUID;
    v_company_id UUID;
BEGIN
    -- Get first company ID (single company mode - use first available)
    SELECT "Id" INTO v_company_id
    FROM "Companies"
    LIMIT 1;
    
    -- If no company exists, use Guid.Empty (00000000-0000-0000-0000-000000000000)
    IF v_company_id IS NULL THEN
        v_company_id := '00000000-0000-0000-0000-000000000000'::uuid;
    END IF;
    
    -- Check if side effect already exists
    SELECT "Id" INTO v_side_effect_id
    FROM "side_effect_definitions"
    WHERE "key" = 'checkMaterialCollection' AND "entity_type" = 'Order' AND "company_id" = v_company_id
    LIMIT 1;
    
    IF v_side_effect_id IS NULL THEN
        v_side_effect_id := gen_random_uuid();
        INSERT INTO "side_effect_definitions" (
            "Id", "company_id", "key", "entity_type", "name", "description", 
            "executor_type", "executor_config_json", "is_active", "is_deleted", 
            "created_at", "updated_at", "display_order"
        )
        VALUES (
            v_side_effect_id,
            v_company_id,
            'checkMaterialCollection',
            'Order',
            'Check Material Collection',
            'Checks if SI has required materials when order is assigned',
            'CheckMaterialCollectionSideEffectExecutor',
            NULL,
            true,
            false,
            CURRENT_TIMESTAMP,
            CURRENT_TIMESTAMP,
            10
        );
        RAISE NOTICE 'Created SideEffectDefinition: %', v_side_effect_id;
    ELSE
        -- Update existing definition to ensure it's active
        UPDATE "side_effect_definitions"
        SET 
            "is_active" = true,
            "is_deleted" = false,
            "updated_at" = CURRENT_TIMESTAMP
        WHERE "Id" = v_side_effect_id;
        RAISE NOTICE 'Updated existing SideEffectDefinition: %', v_side_effect_id;
    END IF;
END $$;

-- Step 2: Link Side Effect to Workflow Transition (Pending -> Assigned)
DO $$
DECLARE
    v_transition_id UUID;
    v_current_config JSONB;
    v_new_config JSONB;
    v_workflow_def_id UUID;
BEGIN
    -- Find workflow definition for Order entity
    SELECT "Id" INTO v_workflow_def_id
    FROM "WorkflowDefinitions"
    WHERE "EntityType" = 'Order'
      AND "IsActive" = true
      AND "IsDeleted" = false
    ORDER BY "CreatedAt" DESC
    LIMIT 1;
    
    IF v_workflow_def_id IS NULL THEN
        RAISE NOTICE 'Warning: No active workflow definition found for Order entity';
        RAISE NOTICE 'You may need to create a workflow definition first';
    ELSE
        -- Find the transition from Pending to Assigned
        SELECT "Id", "SideEffectsConfigJson" INTO v_transition_id, v_current_config
        FROM "WorkflowTransitions"
        WHERE "WorkflowDefinitionId" = v_workflow_def_id
          AND ("FromStatus" IS NULL OR "FromStatus" = 'Pending')
          AND "ToStatus" = 'Assigned'
          AND "IsActive" = true
          AND "IsDeleted" = false
        LIMIT 1;
        
        IF v_transition_id IS NULL THEN
            RAISE NOTICE 'Warning: No active transition found from Pending to Assigned';
            RAISE NOTICE 'You may need to create this transition manually or check your workflow definition';
        ELSE
            -- Initialize config if null
            IF v_current_config IS NULL THEN
                v_current_config := '{}'::jsonb;
            END IF;
            
            -- Add checkMaterialCollection to side effects config
            v_new_config := v_current_config || jsonb_build_object('checkMaterialCollection', true);
            
            -- Only update if config changed
            IF v_new_config != v_current_config THEN
                UPDATE "WorkflowTransitions"
                SET 
                    "SideEffectsConfigJson" = v_new_config,
                    "UpdatedAt" = CURRENT_TIMESTAMP
                WHERE "Id" = v_transition_id;
                RAISE NOTICE 'Updated WorkflowTransition % to include checkMaterialCollection side effect', v_transition_id;
            ELSE
                RAISE NOTICE 'WorkflowTransition % already has checkMaterialCollection side effect configured', v_transition_id;
            END IF;
        END IF;
    END IF;
END $$;

-- Step 3: Create Stock Locations for Service Installers (if not exists)
DO $$
DECLARE
    v_created_count INTEGER := 0;
BEGIN
    INSERT INTO "StockLocations" (
        "Id", "CompanyId", "Name", "Type", "LinkedServiceInstallerId", 
        "IsActive", "CreatedAt", "UpdatedAt"
    )
    SELECT 
        gen_random_uuid(),
        NULL, -- Single company mode
        si."Name" || ' Stock',
        'SI',
        si."Id",
        true,
        CURRENT_TIMESTAMP,
        CURRENT_TIMESTAMP
    FROM "ServiceInstallers" si
    WHERE si."IsActive" = true
      AND si."IsDeleted" = false
      AND NOT EXISTS (
        SELECT 1 FROM "StockLocations" sl 
        WHERE sl."LinkedServiceInstallerId" = si."Id" 
          AND sl."Type" = 'SI'
          AND sl."IsDeleted" = false
      );
    
    GET DIAGNOSTICS v_created_count = ROW_COUNT;
    RAISE NOTICE 'Created % StockLocation(s) for Service Installers', v_created_count;
END $$;

-- Step 4: Verify Configuration
DO $$
DECLARE
    v_side_effect_count INTEGER;
    v_transition_count INTEGER;
    v_stock_location_count INTEGER;
BEGIN
    -- Check side effect definition
    SELECT COUNT(*) INTO v_side_effect_count
    FROM "side_effect_definitions"
    WHERE "key" = 'checkMaterialCollection' 
      AND "entity_type" = 'Order'
      AND "is_active" = true
      AND "is_deleted" = false;
    
    -- Check workflow transition
    SELECT COUNT(*) INTO v_transition_count
    FROM "WorkflowTransitions" wt
    INNER JOIN "WorkflowDefinitions" wd ON wt."WorkflowDefinitionId" = wd."Id"
    WHERE wd."EntityType" = 'Order'
      AND (wt."FromStatus" IS NULL OR wt."FromStatus" = 'Pending')
      AND wt."ToStatus" = 'Assigned'
      AND wt."IsActive" = true
      AND wt."IsDeleted" = false
      AND wt."SideEffectsConfigJson"::text LIKE '%checkMaterialCollection%';
    
    -- Check stock locations
    SELECT COUNT(*) INTO v_stock_location_count
    FROM "StockLocations"
    WHERE "Type" = 'SI'
      AND "IsActive" = true
      AND "IsDeleted" = false;
    
    RAISE NOTICE '';
    RAISE NOTICE '========================================';
    RAISE NOTICE 'Material Collection System Activation Summary';
    RAISE NOTICE '========================================';
    RAISE NOTICE 'Side Effect Definitions: %', v_side_effect_count;
    RAISE NOTICE 'Workflow Transitions Configured: %', v_transition_count;
    RAISE NOTICE 'SI Stock Locations: %', v_stock_location_count;
    RAISE NOTICE '========================================';
    
    IF v_side_effect_count > 0 AND v_transition_count > 0 THEN
        RAISE NOTICE 'Material Collection System is ACTIVATED';
    ELSE
        RAISE NOTICE 'WARNING: Material Collection System may not be fully configured';
        IF v_side_effect_count = 0 THEN
            RAISE NOTICE '   - Side effect definition is missing';
        END IF;
        IF v_transition_count = 0 THEN
            RAISE NOTICE '   - Workflow transition is not configured';
        END IF;
    END IF;
    RAISE NOTICE '';
END $$;

