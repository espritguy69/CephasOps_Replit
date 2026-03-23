-- ============================================
-- Master Data Seed Script
-- ============================================
-- Seeds: Departments, Materials, MaterialCategories
-- Dependencies: Companies
-- ============================================

DO $$
DECLARE
    v_company_id UUID;
    v_gpon_department_id UUID;
BEGIN
    -- Get company ID (can be NULL for single-company mode)
    SELECT "Id" INTO v_company_id FROM "Companies" ORDER BY "CreatedAt" LIMIT 1;
    
    -- ============================================
    -- 1. Departments (GPON)
    -- ============================================
    SELECT "Id" INTO v_gpon_department_id 
    FROM "Departments" 
    WHERE ("Code" = 'GPON' OR "Name" ILIKE '%GPON%') 
    LIMIT 1;
    
    IF v_gpon_department_id IS NULL THEN
        v_gpon_department_id := gen_random_uuid();
        INSERT INTO "Departments" (
            "Id", "CompanyId", "Name", "Code", "Description", "IsActive", "CreatedAt", "UpdatedAt"
        ) VALUES (
            v_gpon_department_id,
            v_company_id,
            'GPON',
            'GPON',
            'GPON Operations Department',
            true,
            NOW(),
            NOW()
        );
        RAISE NOTICE 'Created GPON department';
    ELSE
        RAISE NOTICE 'GPON department already exists';
    END IF;
    
    -- ============================================
    -- 2. Materials (~50+ items)
    -- ============================================
    INSERT INTO "Materials" (
        "Id", "CompanyId", "DepartmentId", "ItemCode", "Description", "Category", 
        "UnitOfMeasure", "IsSerialised", "DefaultCost", "IsActive", "CreatedAt", "UpdatedAt"
    ) VALUES
        -- ONT / Router (Serialized)
        (gen_random_uuid(), v_company_id, v_gpon_department_id, 'CAE-000-0820', 'Huawei HG8145X6 - Dual-band WiFi 6 ONT', 'ONT / Router', 'Unit', true, 0, true, NOW(), NOW()),
        (gen_random_uuid(), v_company_id, v_gpon_department_id, 'CAE-000-0780', 'Huawei HG8145V5 - Dual-band ONT', 'ONT / Router', 'Unit', true, 0, true, NOW(), NOW()),
        (gen_random_uuid(), v_company_id, v_gpon_department_id, 'CAE-000-0830', 'Huawei HN8245X6s-8N-30 (2GB) - Enhanced ONT with 2GB', 'ONT / Router', 'Unit', true, 0, true, NOW(), NOW()),
        (gen_random_uuid(), v_company_id, v_gpon_department_id, 'CAE-000-0840', 'Huawei HG8245X6-8N-30 (1GB) - ONT (1GB)', 'ONT / Router', 'Unit', true, 0, true, NOW(), NOW()),
        (gen_random_uuid(), v_company_id, v_gpon_department_id, 'CAE-000-0851', 'Huawei K153 - Router/ONT', 'Router / ONT', 'Unit', true, 0, true, NOW(), NOW()),
        (gen_random_uuid(), v_company_id, v_gpon_department_id, 'CAE-000-0860', 'Huawei HG8145B7N - FTTH ONT', 'ONT', 'Unit', true, 0, true, NOW(), NOW()),
        
        -- Router / AP (Serialized)
        (gen_random_uuid(), v_company_id, v_gpon_department_id, 'CAE-000-0770', 'Huawei WA8021V5 - WiFi 5 Access Point', 'Router / AP', 'Unit', true, 0, true, NOW(), NOW()),
        
        -- Router (Serialized)
        (gen_random_uuid(), v_company_id, v_gpon_department_id, 'CAE-000-0760', 'TP-Link HC420 - Wireless Router', 'Router', 'Unit', true, 0, true, NOW(), NOW()),
        (gen_random_uuid(), v_company_id, v_gpon_department_id, 'CAE-000-0750', 'TP-Link EC440 - Dual-band Router', 'Router', 'Unit', true, 0, true, NOW(), NOW()),
        (gen_random_uuid(), v_company_id, v_gpon_department_id, 'CAE-000-0320', 'TP-Link Archer C1200 - Home WiFi Router', 'Router', 'Unit', true, 0, true, NOW(), NOW()),
        (gen_random_uuid(), v_company_id, v_gpon_department_id, 'CAE-000-0550', 'TP-Link EC230-G1 - Mesh Router', 'Router / Mesh', 'Unit', true, 0, true, NOW(), NOW()),
        (gen_random_uuid(), v_company_id, v_gpon_department_id, 'CAE-000-0290', 'D-Link 850L - Broadband Router', 'Router', 'Unit', true, 0, true, NOW(), NOW()),
        (gen_random_uuid(), v_company_id, v_gpon_department_id, 'CAE-000-0350', 'D-Link DIR-882 - High-performance Router', 'Router', 'Unit', true, 0, true, NOW(), NOW()),
        (gen_random_uuid(), v_company_id, v_gpon_department_id, 'CAE-000-0900', 'ZyXEL EX3300-T0 - WiFi Router', 'Router', 'Unit', true, 0, true, NOW(), NOW()),
        (gen_random_uuid(), v_company_id, v_gpon_department_id, 'CAE-000-0850', 'Huawei V163 - Customer Premise Router', 'Router', 'Unit', true, 0, true, NOW(), NOW()),
        (gen_random_uuid(), v_company_id, v_gpon_department_id, 'CAE-CEL-0010', 'Skyworth RN685 (Celcom) - Celcom HSBA Router', 'Router', 'Unit', true, 0, true, NOW(), NOW()),
        (gen_random_uuid(), v_company_id, v_gpon_department_id, 'CAE-000-1000', 'Skyworth RN685 (Digi) - Digi HSBA Router', 'Router', 'Unit', true, 0, true, NOW(), NOW()),
        (gen_random_uuid(), v_company_id, v_gpon_department_id, 'CAE-CEL-1001', 'TP-Link EX510 (Digi) - Digi HSBA Router', 'Router', 'Unit', true, 0, true, NOW(), NOW()),
        (gen_random_uuid(), v_company_id, v_gpon_department_id, 'CAE-CEL-0020', 'TP-Link EX510 (Celcom) - Celcom HSBA Router', 'Router', 'Unit', true, 0, true, NOW(), NOW()),
        (gen_random_uuid(), v_company_id, v_gpon_department_id, 'CAE-UME-0010', 'D-Link DIR-X1860Z (Umobile) - WiFi 6 Router', 'Router', 'Unit', true, 0, true, NOW(), NOW()),
        (gen_random_uuid(), v_company_id, v_gpon_department_id, 'CAE-CDI-1001', 'TP-Link EX510 (CelcomDigi) - CelcomDigi HSBA Router', 'Router', 'Unit', true, 0, true, NOW(), NOW()),
        (gen_random_uuid(), v_company_id, v_gpon_department_id, 'CAE-CDI-1002', 'TP-Link EX820 (CelcomDigi) - WiFi 6 Router', 'Router', 'Unit', true, 0, true, NOW(), NOW()),
        
        -- ONU (Serialized)
        (gen_random_uuid(), v_company_id, v_gpon_department_id, 'PON-AHW-0350', 'Huawei HG8240H5 - Optical Network Unit', 'ONU', 'Unit', true, 0, true, NOW(), NOW()),
        (gen_random_uuid(), v_company_id, v_gpon_department_id, 'PON-AHW-0353', 'Huawei HG8140H5 - Optical Network Unit', 'ONU', 'Unit', true, 0, true, NOW(), NOW()),
        
        -- Phone (Serialized)
        (gen_random_uuid(), v_company_id, v_gpon_department_id, 'CAE-000-0210', 'Motorola C1001LA - Cordless Phone (Black)', 'Phone', 'Unit', true, 0, true, NOW(), NOW()),
        
        -- IAD (Serialized)
        (gen_random_uuid(), v_company_id, v_gpon_department_id, 'ACS-IAD-0050', 'Yeastar IAD 4 ports - 4-Port Integrated Access Device', 'IAD', 'Unit', true, 0, true, NOW(), NOW()),
        (gen_random_uuid(), v_company_id, v_gpon_department_id, 'ACS-IAD-0070', 'D-Link IAD 4 Ports - 4-Port IAD', 'IAD', 'Unit', true, 0, true, NOW(), NOW()),
        (gen_random_uuid(), v_company_id, v_gpon_department_id, 'IAD DLINK', 'DVG-5004S - VoIP IAD', 'IAD', 'Unit', true, 0, true, NOW(), NOW()),
        (gen_random_uuid(), v_company_id, v_gpon_department_id, 'ACS-IAD-0020', 'IAD 8 Ports - 8-Port Integrated Access Device', 'IAD', 'Unit', true, 0, true, NOW(), NOW()),
        (gen_random_uuid(), v_company_id, v_gpon_department_id, 'ACS-IAD-0080', 'Synway IAD 4 Ports - Synway IAD (4 Ports)', 'IAD', 'Unit', true, 0, true, NOW(), NOW()),
        
        -- Connector (Non-Serialized)
        (gen_random_uuid(), v_company_id, v_gpon_department_id, 'OFA-000-1000', 'SC/UPC Fast Connector - FAST Connector – Litech (Blue)', 'Connector', 'Unit', false, 0, true, NOW(), NOW()),
        (gen_random_uuid(), v_company_id, v_gpon_department_id, 'OFA-000-1010', 'SC/APC Fast Connector - FAST Connector – Litech (Green APC)', 'Connector', 'Unit', false, 0, true, NOW(), NOW()),
        
        -- Patchcord (Non-Serialized)
        (gen_random_uuid(), v_company_id, v_gpon_department_id, 'OFA-PTC-0540', 'SC-SC SM Simplex 3m - Fiber Patchcord (3m)', 'Patchcord', 'Piece', false, 0, true, NOW(), NOW()),
        (gen_random_uuid(), v_company_id, v_gpon_department_id, 'OFA-PTC-0070', 'SC/APC-SC SM Simplex 6m - Fiber Patchcord (6m)', 'Patchcord', 'Piece', false, 0, true, NOW(), NOW()),
        (gen_random_uuid(), v_company_id, v_gpon_department_id, 'OFA-PTC-0830', 'SC/UPC-SC/UPC 10m - Fiber Patchcord (10m)', 'Patchcord', 'Piece', false, 0, true, NOW(), NOW()),
        (gen_random_uuid(), v_company_id, v_gpon_department_id, 'OFA-PTC-0840', 'SC/UPC-SC/UPC 15m - Fiber Patchcord (15m)', 'Patchcord', 'Piece', false, 0, true, NOW(), NOW()),
        (gen_random_uuid(), v_company_id, v_gpon_department_id, 'OFA-PTC-0820', 'SC/UPC Patchcord 6m - Fiber Patchcord (6m)', 'Patchcord', 'Piece', false, 0, true, NOW(), NOW()),
        
        -- Drop Cable (Non-Serialized)
        (gen_random_uuid(), v_company_id, v_gpon_department_id, 'OFC-002-SMDC', 'Drop Cable SM 2 Core - Fiber Drop Cable', 'Drop Cable', 'Meter', false, 0, true, NOW(), NOW()),
        (gen_random_uuid(), v_company_id, v_gpon_department_id, 'OFC-DRC-0080', 'SC/APC Drop Cable 80m - Outdoor Drop Cable 80m', 'Drop Cable', 'Unit', false, 0, true, NOW(), NOW()),
        (gen_random_uuid(), v_company_id, v_gpon_department_id, 'OFC-DRC-0100', 'SC/APC Drop Cable 100m - Outdoor Drop Cable 100m', 'Drop Cable', 'Unit', false, 0, true, NOW(), NOW()),
        (gen_random_uuid(), v_company_id, v_gpon_department_id, 'OFC-DRC-P080', 'Drop Cable 80m (RDF Pole) - RDF Pole Outdoor Drop Cable', 'Drop Cable', 'Unit', false, 0, true, NOW(), NOW()),
        
        -- Accessories (Non-Serialized)
        (gen_random_uuid(), v_company_id, v_gpon_department_id, 'FTB-001-001', 'Fiber Termination Box - Outdoor FTB (2 Core)', 'Accessories', 'Unit', false, 0, true, NOW(), NOW()),
        (gen_random_uuid(), v_company_id, v_gpon_department_id, 'CAE-000-0852', 'Huawei ATB - Access Termination Box', 'Accessories', 'Unit', false, 0, true, NOW(), NOW()),
        
        -- Fiber Cable (Non-Serialized)
        (gen_random_uuid(), v_company_id, v_gpon_department_id, 'CAE-000-0853', 'Huawei Transparent Fibre 50m - Clear Indoor Fiber Cable', 'Fiber Cable', 'Meter', false, 0, true, NOW(), NOW()),
        
        -- Distribution (Non-Serialized)
        (gen_random_uuid(), v_company_id, v_gpon_department_id, 'CAE-000-0854', 'Huawei FDU - Fiber Distribution Unit', 'Distribution', 'Unit', false, 0, true, NOW(), NOW()),
        (gen_random_uuid(), v_company_id, v_gpon_department_id, 'CAE-000-0855', 'Huawei FMC - Fiber Management Cabinet', 'Distribution', 'Unit', false, 0, true, NOW(), NOW())
    ON CONFLICT ("CompanyId", "ItemCode") DO NOTHING;
    
    RAISE NOTICE 'Seeded Materials (~50 records)';
    
    -- ============================================
    -- 3. MaterialCategories (from materials + defaults)
    -- ============================================
    -- First, create categories from existing materials
    INSERT INTO "MaterialCategories" (
        "Id", "CompanyId", "Name", "Description", "DisplayOrder", "IsActive", "CreatedAt", "UpdatedAt"
    )
    SELECT
        gen_random_uuid(),
        v_company_id,
        cat_name,
        NULL,
        ROW_NUMBER() OVER (ORDER BY cat_name),
        true,
        NOW(),
        NOW()
    FROM (
        SELECT DISTINCT "Category" AS cat_name
        FROM "Materials"
        WHERE "Category" IS NOT NULL 
          AND "Category" != ''
          AND ("CompanyId" = v_company_id OR (v_company_id IS NULL AND "CompanyId" = '00000000-0000-0000-0000-000000000000'::UUID))
    ) AS distinct_cats
    WHERE NOT EXISTS (
        SELECT 1 FROM "MaterialCategories" 
        WHERE "Name" = distinct_cats.cat_name
          AND ("CompanyId" = v_company_id OR (v_company_id IS NULL AND "CompanyId" = '00000000-0000-0000-0000-000000000000'::UUID))
    );
    
    -- Then, add default categories if none exist
    INSERT INTO "MaterialCategories" (
        "Id", "CompanyId", "Name", "Description", "DisplayOrder", "IsActive", "CreatedAt", "UpdatedAt"
    ) VALUES
        (gen_random_uuid(), v_company_id, 'ONU', 'Optical Network Units - Customer premises equipment', 1, true, NOW(), NOW()),
        (gen_random_uuid(), v_company_id, 'Fiber Cable', 'Fiber optic cables (indoor, outdoor, aerial)', 2, true, NOW(), NOW()),
        (gen_random_uuid(), v_company_id, 'Splitter', 'Optical splitters for fiber distribution', 3, true, NOW(), NOW()),
        (gen_random_uuid(), v_company_id, 'Accessories', 'Termination boxes, connectors, adapters', 4, true, NOW(), NOW()),
        (gen_random_uuid(), v_company_id, 'Distribution', 'Distribution units, cabinets, enclosures', 5, true, NOW(), NOW()),
        (gen_random_uuid(), v_company_id, 'Tools', 'Installation tools and equipment', 6, true, NOW(), NOW()),
        (gen_random_uuid(), v_company_id, 'Consumables', 'Consumable items (cable ties, labels, etc.)', 7, true, NOW(), NOW()),
        (gen_random_uuid(), v_company_id, 'Spare Parts', 'Spare parts and replacement components', 8, true, NOW(), NOW())
    ON CONFLICT ("CompanyId", "Name") DO NOTHING;
    
    RAISE NOTICE 'Seeded MaterialCategories';
END $$;

-- ============================================
-- Verification
-- ============================================
DO $$
DECLARE
    v_departments_count INT;
    v_materials_count INT;
    v_material_categories_count INT;
BEGIN
    SELECT COUNT(*) INTO v_departments_count FROM "Departments" WHERE "Code" = 'GPON';
    SELECT COUNT(*) INTO v_materials_count FROM "Materials";
    SELECT COUNT(*) INTO v_material_categories_count FROM "MaterialCategories";
    
    RAISE NOTICE '========================================';
    RAISE NOTICE 'Master Data Seeding Complete';
    RAISE NOTICE '========================================';
    RAISE NOTICE 'GPON Departments: %', v_departments_count;
    RAISE NOTICE 'Materials: %', v_materials_count;
    RAISE NOTICE 'MaterialCategories: %', v_material_categories_count;
    RAISE NOTICE '========================================';
END $$;

