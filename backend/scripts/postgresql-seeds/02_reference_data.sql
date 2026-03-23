-- ============================================
-- Reference Data Seed Script
-- ============================================
-- Seeds: OrderTypes, OrderCategories, BuildingTypes, SplitterTypes
-- Dependencies: Companies, Departments (GPON)
-- ============================================

DO $$
DECLARE
    v_company_id UUID;
    v_gpon_department_id UUID;
BEGIN
    -- Get company ID (can be NULL for single-company mode)
    SELECT "Id" INTO v_company_id FROM "Companies" ORDER BY "CreatedAt" LIMIT 1;
    
    -- Get GPON department ID
    SELECT "Id" INTO v_gpon_department_id 
    FROM "Departments" 
    WHERE ("Code" = 'GPON' OR "Name" ILIKE '%GPON%') 
    LIMIT 1;
    
    -- ============================================
    -- 1. OrderTypes
    -- ============================================
    INSERT INTO "OrderTypes" (
        "Id", "CompanyId", "DepartmentId", "Name", "Code", "Description", 
        "DisplayOrder", "IsActive", "CreatedAt", "UpdatedAt"
    )
    SELECT gen_random_uuid(), v_company_id, v_gpon_department_id, v.name, v.code, v.description, v.display_order, true, NOW(), NOW()
    FROM (VALUES
        ('Activation', 'ACTIVATION', 'New installation + activation of service', 1),
        ('Modification Indoor', 'MODIFICATION_INDOOR', 'Indoor modification of existing service', 2),
        ('Modification Outdoor', 'MODIFICATION_OUTDOOR', 'Outdoor modification of existing service', 3),
        ('Assurance', 'ASSURANCE', 'Fault repair and troubleshooting', 4),
        ('Value Added Service', 'VALUE_ADDED_SERVICE', 'Additional services beyond standard installation/repair', 5)
    ) AS v(name, code, description, display_order)
    WHERE NOT EXISTS (
        SELECT 1 FROM "OrderTypes" ot WHERE ot."Code" = v.code
    );
    
    RAISE NOTICE 'Seeded OrderTypes (5 records)';
    
    -- ============================================
    -- 2. OrderCategories (formerly InstallationTypes)
    -- ============================================
    INSERT INTO "OrderCategories" (
        "Id", "CompanyId", "DepartmentId", "Name", "Code", "Description", 
        "DisplayOrder", "IsActive", "CreatedAt", "UpdatedAt"
    )
    SELECT gen_random_uuid(), v_company_id, v_gpon_department_id, v.name, v.code, v.description, v.display_order, true, NOW(), NOW()
    FROM (VALUES
        ('TIME-FTTH', 'TIME-FTTH', 'Fibre to the Home', 1),
        ('TIME-FTTR', 'TIME-FTTR', 'Fibre to the Room', 2),
        ('TIME-FTTC', 'TIME-FTTC', 'Fibre to the Charge', 3)
    ) AS v(name, code, description, display_order)
    WHERE NOT EXISTS (
        SELECT 1 FROM "OrderCategories" oc WHERE oc."Code" = v.code
    );
    
    RAISE NOTICE 'Seeded OrderCategories (3 records: TIME-FTTH, TIME-FTTR, TIME-FTTC)';
    
    -- ============================================
    -- 3. BuildingTypes
    -- ============================================
    INSERT INTO "BuildingTypes" (
        "Id", "CompanyId", "DepartmentId", "Name", "Code", "Description", 
        "DisplayOrder", "IsActive", "CreatedAt", "UpdatedAt"
    )
    SELECT gen_random_uuid(), v_company_id, v_gpon_department_id, v.name, v.code, v.description, v.display_order, true, NOW(), NOW()
    FROM (VALUES
        ('Condominium', 'CONDO', 'High-rise residential building', 1),
        ('Apartment', 'APARTMENT', 'Multi-unit residential building', 2),
        ('Service Apartment', 'SERVICE_APT', 'Serviced residential units', 3),
        ('Flat', 'FLAT', 'Low-rise residential units', 4),
        ('Terrace House', 'TERRACE', 'Row houses', 5),
        ('Semi-Detached', 'SEMI_DETACHED', 'Semi-detached houses', 6),
        ('Bungalow', 'BUNGALOW', 'Single-story detached house', 7),
        ('Townhouse', 'TOWNHOUSE', 'Multi-story attached houses', 8),
        ('Office Tower', 'OFFICE_TOWER', 'High-rise office building', 10),
        ('Office Building', 'OFFICE', 'Low to mid-rise office building', 11),
        ('Shop Office', 'SHOP_OFFICE', 'Mixed shop and office building', 12),
        ('Shopping Mall', 'MALL', 'Retail shopping complex', 13),
        ('Hotel', 'HOTEL', 'Hotel or resort building', 14),
        ('Mixed Development', 'MIXED', 'Mixed residential and commercial', 20),
        ('Industrial', 'INDUSTRIAL', 'Industrial or warehouse building', 30),
        ('Warehouse', 'WAREHOUSE', 'Storage or warehouse facility', 31),
        ('Educational', 'EDUCATIONAL', 'School or educational institution', 32),
        ('Government', 'GOVERNMENT', 'Government building', 33),
        ('Other', 'OTHER', 'Other building type', 99)
    ) AS v(name, code, description, display_order)
    WHERE NOT EXISTS (
        SELECT 1 FROM "BuildingTypes" bt WHERE bt."Code" = v.code
    );
    
    RAISE NOTICE 'Seeded BuildingTypes (15 records)';
    
    -- ============================================
    -- 4. SplitterTypes
    -- ============================================
    INSERT INTO "SplitterTypes" (
        "Id", "CompanyId", "DepartmentId", "Name", "Code", "TotalPorts", 
        "StandbyPortNumber", "Description", "DisplayOrder", "IsActive", "CreatedAt", "UpdatedAt"
    )
    SELECT gen_random_uuid(), v_company_id, v_gpon_department_id, v.name, v.code, v.total_ports, v.standby_port, v.description, v.display_order, true, NOW(), NOW()
    FROM (VALUES
        ('1:8', '1_8', 8, NULL::INT, '1:8 Splitter (8 ports)', 1),
        ('1:12', '1_12', 12, NULL::INT, '1:12 Splitter (12 ports)', 2),
        ('1:32', '1_32', 32, 32, '1:32 Splitter (32 ports, port 32 is standby)', 3)
    ) AS v(name, code, total_ports, standby_port, description, display_order)
    WHERE NOT EXISTS (
        SELECT 1 FROM "SplitterTypes" st WHERE st."Code" = v.code
    );
    
    RAISE NOTICE 'Seeded SplitterTypes (3 records)';
END $$;

-- ============================================
-- Verification
-- ============================================
DO $$
DECLARE
    v_order_types_count INT;
    v_order_categories_count INT;
    v_building_types_count INT;
    v_splitter_types_count INT;
BEGIN
    SELECT COUNT(*) INTO v_order_types_count FROM "OrderTypes";
    SELECT COUNT(*) INTO v_order_categories_count FROM "OrderCategories";
    SELECT COUNT(*) INTO v_building_types_count FROM "BuildingTypes";
    SELECT COUNT(*) INTO v_splitter_types_count FROM "SplitterTypes";
    
    RAISE NOTICE '========================================';
    RAISE NOTICE 'Reference Data Seeding Complete';
    RAISE NOTICE '========================================';
    RAISE NOTICE 'OrderTypes: %', v_order_types_count;
    RAISE NOTICE 'OrderCategories: %', v_order_categories_count;
    RAISE NOTICE 'BuildingTypes: %', v_building_types_count;
    RAISE NOTICE 'SplitterTypes: %', v_splitter_types_count;
    RAISE NOTICE '========================================';
END $$;

