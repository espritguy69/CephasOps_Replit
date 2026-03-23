-- ============================================
-- Inventory Data Seed Script
-- ============================================
-- Seeds: MovementTypes, LocationTypes
-- Dependencies: Companies
-- ============================================

DO $$
DECLARE
    v_company_id UUID;
BEGIN
    -- Get company ID (can be NULL for single-company mode)
    SELECT "Id" INTO v_company_id FROM "Companies" ORDER BY "CreatedAt" LIMIT 1;
    
    -- ============================================
    -- 1. MovementTypes
    -- ============================================
    INSERT INTO "MovementTypes" (
        "Id", "CompanyId", "Code", "Name", "Description", "Direction", 
        "RequiresFromLocation", "RequiresToLocation", "RequiresOrderId", 
        "RequiresServiceInstallerId", "RequiresPartnerId", 
        "AffectsStockBalance", "StockImpact", "IsActive", "SortOrder",
        "CreatedAt", "UpdatedAt", "IsDeleted"
    ) VALUES
        -- Inbound movements (increase stock)
        (gen_random_uuid(), v_company_id, 'GRN', 'Goods Receipt Note', 'Receipt of materials from supplier', 'In', false, true, false, false, false, true, 'Positive', true, 1, NOW(), NOW(), false),
        (gen_random_uuid(), v_company_id, 'ReturnFromSI', 'Return from Service Installer', 'Materials returned from service installer to warehouse', 'In', false, true, false, true, false, true, 'Positive', true, 2, NOW(), NOW(), false),
        (gen_random_uuid(), v_company_id, 'ReturnFromCustomer', 'Return from Customer', 'Materials returned from customer site', 'In', false, true, true, false, false, true, 'Positive', true, 3, NOW(), NOW(), false),
        
        -- Outbound movements (decrease stock)
        (gen_random_uuid(), v_company_id, 'IssueToSI', 'Issue to Service Installer', 'Materials issued to service installer for installation', 'Out', true, false, false, true, false, true, 'Negative', true, 4, NOW(), NOW(), false),
        (gen_random_uuid(), v_company_id, 'IssueToOrder', 'Issue to Order', 'Materials issued directly to order/customer site', 'Out', true, false, true, false, false, true, 'Negative', true, 5, NOW(), NOW(), false),
        (gen_random_uuid(), v_company_id, 'ReturnFaulty', 'Return Faulty', 'Faulty materials returned to warehouse/RMA', 'In', false, true, true, true, false, true, 'Positive', true, 6, NOW(), NOW(), false),
        
        -- Transfer movements (neutral stock impact)
        (gen_random_uuid(), v_company_id, 'Transfer', 'Transfer', 'Transfer materials between locations', 'Transfer', true, true, false, false, false, true, 'Neutral', true, 7, NOW(), NOW(), false),
        (gen_random_uuid(), v_company_id, 'TransferToRMA', 'Transfer to RMA', 'Transfer faulty materials to RMA location', 'Transfer', true, true, false, false, false, true, 'Neutral', true, 8, NOW(), NOW(), false),
        
        -- Adjustment movements
        (gen_random_uuid(), v_company_id, 'Adjustment', 'Stock Adjustment', 'Stock count adjustment (increase or decrease)', 'Adjust', false, true, false, false, false, true, 'Positive', true, 9, NOW(), NOW(), false),
        (gen_random_uuid(), v_company_id, 'AdjustmentDown', 'Stock Adjustment (Decrease)', 'Stock count adjustment (decrease)', 'Adjust', true, false, false, false, false, true, 'Negative', true, 10, NOW(), NOW(), false),
        
        -- Write-off movements
        (gen_random_uuid(), v_company_id, 'WriteOff', 'Write Off', 'Materials written off (damaged, expired, etc.)', 'Out', true, false, false, false, false, true, 'Negative', true, 11, NOW(), NOW(), false)
    ON CONFLICT ("CompanyId", "Code") DO NOTHING;
    
    RAISE NOTICE 'Seeded MovementTypes (11 records)';
    
    -- ============================================
    -- 2. LocationTypes
    -- ============================================
    INSERT INTO "LocationTypes" (
        "Id", "CompanyId", "Code", "Name", "Description",
        "RequiresServiceInstallerId", "RequiresBuildingId", "RequiresWarehouseId",
        "AutoCreate", "AutoCreateTrigger", "IsActive", "SortOrder",
        "CreatedAt", "UpdatedAt", "IsDeleted"
    ) VALUES
        (gen_random_uuid(), v_company_id, 'Warehouse', 'Warehouse', 'Main warehouse location', false, false, false, true, 'WarehouseCreated', true, 1, NOW(), NOW(), false),
        (gen_random_uuid(), v_company_id, 'SI', 'Service Installer', 'Service installer stock location', true, false, false, true, 'ServiceInstallerCreated', true, 2, NOW(), NOW(), false),
        (gen_random_uuid(), v_company_id, 'CustomerSite', 'Customer Site', 'Customer installation site', false, true, false, true, 'BuildingCreated', true, 3, NOW(), NOW(), false),
        (gen_random_uuid(), v_company_id, 'RMA', 'RMA Location', 'Return Merchandise Authorization location', false, false, false, false, NULL, true, 4, NOW(), NOW(), false),
        (gen_random_uuid(), v_company_id, 'Transit', 'Transit', 'Materials in transit', false, false, false, false, NULL, true, 5, NOW(), NOW(), false),
        (gen_random_uuid(), v_company_id, 'Supplier', 'Supplier', 'Supplier location (for tracking)', false, false, false, false, NULL, true, 6, NOW(), NOW(), false)
    ON CONFLICT ("CompanyId", "Code") DO NOTHING;
    
    RAISE NOTICE 'Seeded LocationTypes (6 records)';
END $$;

-- ============================================
-- Verification
-- ============================================
DO $$
DECLARE
    v_movement_types_count INT;
    v_location_types_count INT;
BEGIN
    SELECT COUNT(*) INTO v_movement_types_count FROM "MovementTypes";
    SELECT COUNT(*) INTO v_location_types_count FROM "LocationTypes";
    
    RAISE NOTICE '========================================';
    RAISE NOTICE 'Inventory Data Seeding Complete';
    RAISE NOTICE '========================================';
    RAISE NOTICE 'MovementTypes: %', v_movement_types_count;
    RAISE NOTICE 'LocationTypes: %', v_location_types_count;
    RAISE NOTICE '========================================';
END $$;

