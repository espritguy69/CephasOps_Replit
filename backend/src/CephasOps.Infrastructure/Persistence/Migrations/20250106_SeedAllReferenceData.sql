-- ============================================
-- CephasOps - Comprehensive Seed Data Migration
-- ============================================
-- This migration seeds all reference data directly in PostgreSQL
-- PostgreSQL is now the single source of truth for all seed data
-- 
-- Date: 2025-01-06
-- Purpose: Replace C# DatabaseSeeder with PostgreSQL-based seeding
-- 
-- This migration is idempotent - safe to run multiple times
-- ============================================

DO $$
DECLARE
    v_company_id UUID;
    v_gpon_department_id UUID;
    v_super_admin_role_id UUID;
    v_director_role_id UUID;
    v_hod_role_id UUID;
    v_supervisor_role_id UUID;
    v_finance_role_id UUID;
    v_admin_user_id UUID;
    v_finance_user_id UUID;
    v_admin_password_hash TEXT;
    v_finance_password_hash TEXT;
BEGIN
    -- ============================================
    -- Step 1: Enable required extensions
    -- ============================================
    CREATE EXTENSION IF NOT EXISTS "pgcrypto";
    
    -- ============================================
    -- Step 2: Calculate password hashes
    -- ============================================
    -- Password: J@saw007, Salt: CephasOps_Salt_2024
    -- C#: SHA256(password + salt) -> Base64
    v_admin_password_hash := encode(digest('J@saw007' || 'CephasOps_Salt_2024', 'sha256'), 'base64');
    
    -- Password: E5pr!tg@L, Salt: CephasOps_Salt_2024
    v_finance_password_hash := encode(digest('E5pr!tg@L' || 'CephasOps_Salt_2024', 'sha256'), 'base64');
    
    -- ============================================
    -- Step 3: Seed Default Company
    -- ============================================
    SELECT "Id" INTO v_company_id 
    FROM "Companies" 
    WHERE "IsDeleted" = false 
    ORDER BY "CreatedAt" ASC 
    LIMIT 1;
    
    IF v_company_id IS NULL THEN
        v_company_id := gen_random_uuid();
        INSERT INTO "Companies" (
            "Id", "LegalName", "ShortName", "Vertical", "IsActive", "CreatedAt", "IsDeleted"
        ) VALUES (
            v_company_id,
            'Cephas',
            'Cephas',
            'General',
            true,
            NOW(),
            false
        ) ON CONFLICT DO NOTHING;
        
        RAISE NOTICE 'Created default company: Cephas (ID: %)', v_company_id;
    ELSE
        RAISE NOTICE 'Default company already exists (ID: %)', v_company_id;
    END IF;
    
    -- ============================================
    -- Step 4: Seed Roles
    -- ============================================
    -- SuperAdmin Role
    INSERT INTO "Roles" ("Id", "Name", "Scope", "IsDeleted")
    SELECT gen_random_uuid(), 'SuperAdmin', 'Global', false
    WHERE NOT EXISTS (SELECT 1 FROM "Roles" WHERE "Name" = 'SuperAdmin' AND "Scope" = 'Global' AND "IsDeleted" = false)
    RETURNING "Id" INTO v_super_admin_role_id;
    
    IF v_super_admin_role_id IS NULL THEN
        SELECT "Id" INTO v_super_admin_role_id FROM "Roles" WHERE "Name" = 'SuperAdmin' AND "Scope" = 'Global' AND "IsDeleted" = false;
    END IF;
    
    -- Director Role
    INSERT INTO "Roles" ("Id", "Name", "Scope", "IsDeleted")
    SELECT gen_random_uuid(), 'Director', 'Global', false
    WHERE NOT EXISTS (SELECT 1 FROM "Roles" WHERE "Name" = 'Director' AND "Scope" = 'Global' AND "IsDeleted" = false)
    RETURNING "Id" INTO v_director_role_id;
    
    IF v_director_role_id IS NULL THEN
        SELECT "Id" INTO v_director_role_id FROM "Roles" WHERE "Name" = 'Director' AND "Scope" = 'Global' AND "IsDeleted" = false;
    END IF;
    
    -- HeadOfDepartment Role
    INSERT INTO "Roles" ("Id", "Name", "Scope", "IsDeleted")
    SELECT gen_random_uuid(), 'HeadOfDepartment', 'Global', false
    WHERE NOT EXISTS (SELECT 1 FROM "Roles" WHERE "Name" = 'HeadOfDepartment' AND "Scope" = 'Global' AND "IsDeleted" = false)
    RETURNING "Id" INTO v_hod_role_id;
    
    IF v_hod_role_id IS NULL THEN
        SELECT "Id" INTO v_hod_role_id FROM "Roles" WHERE "Name" = 'HeadOfDepartment' AND "Scope" = 'Global' AND "IsDeleted" = false;
    END IF;
    
    -- Supervisor Role
    INSERT INTO "Roles" ("Id", "Name", "Scope", "IsDeleted")
    SELECT gen_random_uuid(), 'Supervisor', 'Global', false
    WHERE NOT EXISTS (SELECT 1 FROM "Roles" WHERE "Name" = 'Supervisor' AND "Scope" = 'Global' AND "IsDeleted" = false)
    RETURNING "Id" INTO v_supervisor_role_id;
    
    IF v_supervisor_role_id IS NULL THEN
        SELECT "Id" INTO v_supervisor_role_id FROM "Roles" WHERE "Name" = 'Supervisor' AND "Scope" = 'Global' AND "IsDeleted" = false;
    END IF;
    
    -- FinanceManager Role
    INSERT INTO "Roles" ("Id", "Name", "Scope", "IsDeleted")
    SELECT gen_random_uuid(), 'FinanceManager', 'Global', false
    WHERE NOT EXISTS (SELECT 1 FROM "Roles" WHERE "Name" = 'FinanceManager' AND "Scope" = 'Global' AND "IsDeleted" = false)
    RETURNING "Id" INTO v_finance_role_id;
    
    IF v_finance_role_id IS NULL THEN
        SELECT "Id" INTO v_finance_role_id FROM "Roles" WHERE "Name" = 'FinanceManager' AND "Scope" = 'Global' AND "IsDeleted" = false;
    END IF;
    
    RAISE NOTICE 'Roles seeded successfully';
    
    -- ============================================
    -- Step 5: Seed Default Admin User
    -- ============================================
    SELECT "Id" INTO v_admin_user_id 
    FROM "Users" 
    WHERE "Email" = 'simon@cephas.com.my' AND "IsDeleted" = false;
    
    IF v_admin_user_id IS NULL THEN
        v_admin_user_id := gen_random_uuid();
        INSERT INTO "Users" (
            "Id", "Name", "Email", "PasswordHash", "IsActive", "CreatedAt", "IsDeleted"
        ) VALUES (
            v_admin_user_id,
            'Simon',
            'simon@cephas.com.my',
            v_admin_password_hash,
            true,
            NOW(),
            false
        );
        
        RAISE NOTICE 'Created default admin user: simon@cephas.com.my';
    ELSE
        -- Update password hash if it doesn't match
        UPDATE "Users" 
        SET "PasswordHash" = v_admin_password_hash, "IsActive" = true
        WHERE "Id" = v_admin_user_id AND "PasswordHash" != v_admin_password_hash;
        
        RAISE NOTICE 'Default admin user already exists';
    END IF;
    
    -- Assign SuperAdmin role
    INSERT INTO "UserRoles" ("UserId", "CompanyId", "RoleId", "CreatedAt")
    SELECT v_admin_user_id, NULL, v_super_admin_role_id, NOW()
    WHERE NOT EXISTS (
        SELECT 1 FROM "UserRoles" 
        WHERE "UserId" = v_admin_user_id AND "RoleId" = v_super_admin_role_id
    );
    
    -- ============================================
    -- Step 6: Seed GPON Department
    -- ============================================
    SELECT "Id" INTO v_gpon_department_id 
    FROM "Departments" 
    WHERE ("Code" = 'GPON' OR "Name" ILIKE '%GPON%') 
      AND ("CompanyId" = v_company_id OR "CompanyId" IS NULL)
      AND "IsDeleted" = false
    LIMIT 1;
    
    IF v_gpon_department_id IS NULL THEN
        v_gpon_department_id := gen_random_uuid();
        INSERT INTO "Departments" (
            "Id", "CompanyId", "Name", "Code", "Description", "IsActive", 
            "CreatedAt", "UpdatedAt", "IsDeleted"
        ) VALUES (
            v_gpon_department_id,
            v_company_id,
            'GPON',
            'GPON',
            'GPON Operations Department',
            true,
            NOW(),
            NOW(),
            false
        );
        
        RAISE NOTICE 'Created GPON department';
    ELSE
        RAISE NOTICE 'GPON department already exists';
    END IF;
    
    -- ============================================
    -- Step 7: Seed Finance HOD User
    -- ============================================
    SELECT "Id" INTO v_finance_user_id 
    FROM "Users" 
    WHERE "Email" = 'finance@cephas.com.my' AND "IsDeleted" = false;
    
    IF v_finance_user_id IS NULL THEN
        v_finance_user_id := gen_random_uuid();
        INSERT INTO "Users" (
            "Id", "Name", "Email", "PasswordHash", "IsActive", "CreatedAt", "IsDeleted"
        ) VALUES (
            v_finance_user_id,
            'Samyu Kavitha',
            'finance@cephas.com.my',
            v_finance_password_hash,
            true,
            NOW(),
            false
        );
        
        RAISE NOTICE 'Created Finance HOD user: finance@cephas.com.my';
    ELSE
        RAISE NOTICE 'Finance HOD user already exists';
    END IF;
    
    -- Assign Finance role
    INSERT INTO "UserRoles" ("UserId", "CompanyId", "RoleId", "CreatedAt")
    SELECT v_finance_user_id, v_company_id, v_finance_role_id, NOW()
    WHERE NOT EXISTS (
        SELECT 1 FROM "UserRoles" 
        WHERE "UserId" = v_finance_user_id AND "RoleId" = v_finance_role_id
    );
    
    -- Link to GPON department
    INSERT INTO "DepartmentMemberships" (
        "Id", "CompanyId", "DepartmentId", "UserId", "Role", "IsDefault", 
        "CreatedAt", "UpdatedAt", "IsDeleted"
    )
    SELECT 
        gen_random_uuid(), 
        v_company_id, 
        v_gpon_department_id, 
        v_finance_user_id, 
        'HOD', 
        true, 
        NOW(), 
        NOW(), 
        false
    WHERE NOT EXISTS (
        SELECT 1 FROM "DepartmentMemberships" 
        WHERE "UserId" = v_finance_user_id AND "DepartmentId" = v_gpon_department_id AND "IsDeleted" = false
    );
    
    -- ============================================
    -- Step 8: Seed Order Types
    -- ============================================
    INSERT INTO "OrderTypes" (
        "Id", "CompanyId", "DepartmentId", "Name", "Code", "Description", 
        "DisplayOrder", "IsActive", "CreatedAt", "UpdatedAt", "IsDeleted"
    ) VALUES
        (gen_random_uuid(), v_company_id, v_gpon_department_id, 'Activation', 'ACTIVATION', 'New installation + activation of service', 1, true, NOW(), NOW(), false),
        (gen_random_uuid(), v_company_id, v_gpon_department_id, 'Modification Indoor', 'MODIFICATION_INDOOR', 'Indoor modification of existing service', 2, true, NOW(), NOW(), false),
        (gen_random_uuid(), v_company_id, v_gpon_department_id, 'Modification Outdoor', 'MODIFICATION_OUTDOOR', 'Outdoor modification of existing service', 3, true, NOW(), NOW(), false),
        (gen_random_uuid(), v_company_id, v_gpon_department_id, 'Assurance', 'ASSURANCE', 'Fault repair and troubleshooting', 4, true, NOW(), NOW(), false),
        (gen_random_uuid(), v_company_id, v_gpon_department_id, 'Value Added Service', 'VALUE_ADDED_SERVICE', 'Additional services beyond standard installation/repair', 5, true, NOW(), NOW(), false)
    ON CONFLICT DO NOTHING;
    
    RAISE NOTICE 'Order Types seeded';
    
    -- ============================================
    -- Step 9: Seed Order Categories
    -- ============================================
    INSERT INTO "OrderCategories" (
        "Id", "CompanyId", "DepartmentId", "Name", "Code", "Description", 
        "DisplayOrder", "IsActive", "CreatedAt", "UpdatedAt", "IsDeleted"
    ) VALUES
        (gen_random_uuid(), v_company_id, v_gpon_department_id, 'FTTH', 'FTTH', 'Fibre to the Home', 1, true, NOW(), NOW(), false),
        (gen_random_uuid(), v_company_id, v_gpon_department_id, 'FTTO', 'FTTO', 'Fibre to the Office', 2, true, NOW(), NOW(), false),
        (gen_random_uuid(), v_company_id, v_gpon_department_id, 'FTTR', 'FTTR', 'Fibre to the Room', 3, true, NOW(), NOW(), false),
        (gen_random_uuid(), v_company_id, v_gpon_department_id, 'FTTC', 'FTTC', 'Fibre to the Curb', 4, true, NOW(), NOW(), false)
    ON CONFLICT DO NOTHING;
    
    RAISE NOTICE 'Order Categories seeded';
    
    -- ============================================
    -- Step 10: Seed Building Types
    -- ============================================
    INSERT INTO "BuildingTypes" (
        "Id", "CompanyId", "DepartmentId", "Name", "Code", "Description", 
        "DisplayOrder", "IsActive", "CreatedAt", "UpdatedAt", "IsDeleted"
    ) VALUES
        -- Residential Types
        (gen_random_uuid(), v_company_id, v_gpon_department_id, 'Condominium', 'CONDO', 'High-rise residential building', 1, true, NOW(), NOW(), false),
        (gen_random_uuid(), v_company_id, v_gpon_department_id, 'Apartment', 'APARTMENT', 'Multi-unit residential building', 2, true, NOW(), NOW(), false),
        (gen_random_uuid(), v_company_id, v_gpon_department_id, 'Service Apartment', 'SERVICE_APT', 'Serviced residential units', 3, true, NOW(), NOW(), false),
        (gen_random_uuid(), v_company_id, v_gpon_department_id, 'Flat', 'FLAT', 'Low-rise residential units', 4, true, NOW(), NOW(), false),
        (gen_random_uuid(), v_company_id, v_gpon_department_id, 'Terrace House', 'TERRACE', 'Row houses', 5, true, NOW(), NOW(), false),
        (gen_random_uuid(), v_company_id, v_gpon_department_id, 'Semi-Detached', 'SEMI_DETACHED', 'Semi-detached houses', 6, true, NOW(), NOW(), false),
        (gen_random_uuid(), v_company_id, v_gpon_department_id, 'Bungalow', 'BUNGALOW', 'Single-story detached house', 7, true, NOW(), NOW(), false),
        (gen_random_uuid(), v_company_id, v_gpon_department_id, 'Townhouse', 'TOWNHOUSE', 'Multi-story attached houses', 8, true, NOW(), NOW(), false),
        -- Commercial Types
        (gen_random_uuid(), v_company_id, v_gpon_department_id, 'Office Tower', 'OFFICE_TOWER', 'High-rise office building', 10, true, NOW(), NOW(), false),
        (gen_random_uuid(), v_company_id, v_gpon_department_id, 'Office Building', 'OFFICE', 'Low to mid-rise office building', 11, true, NOW(), NOW(), false),
        (gen_random_uuid(), v_company_id, v_gpon_department_id, 'Shop Office', 'SHOP_OFFICE', 'Mixed shop and office building', 12, true, NOW(), NOW(), false),
        (gen_random_uuid(), v_company_id, v_gpon_department_id, 'Shopping Mall', 'MALL', 'Retail shopping complex', 13, true, NOW(), NOW(), false),
        (gen_random_uuid(), v_company_id, v_gpon_department_id, 'Hotel', 'HOTEL', 'Hotel or resort building', 14, true, NOW(), NOW(), false),
        -- Mixed Use
        (gen_random_uuid(), v_company_id, v_gpon_department_id, 'Mixed Development', 'MIXED', 'Mixed residential and commercial', 20, true, NOW(), NOW(), false),
        -- Others
        (gen_random_uuid(), v_company_id, v_gpon_department_id, 'Industrial', 'INDUSTRIAL', 'Industrial or warehouse building', 30, true, NOW(), NOW(), false),
        (gen_random_uuid(), v_company_id, v_gpon_department_id, 'Warehouse', 'WAREHOUSE', 'Storage or warehouse facility', 31, true, NOW(), NOW(), false),
        (gen_random_uuid(), v_company_id, v_gpon_department_id, 'Educational', 'EDUCATIONAL', 'School or educational institution', 32, true, NOW(), NOW(), false),
        (gen_random_uuid(), v_company_id, v_gpon_department_id, 'Government', 'GOVERNMENT', 'Government building', 33, true, NOW(), NOW(), false),
        (gen_random_uuid(), v_company_id, v_gpon_department_id, 'Other', 'OTHER', 'Other building type', 99, true, NOW(), NOW(), false)
    ON CONFLICT DO NOTHING;
    
    RAISE NOTICE 'Building Types seeded';
    
    -- ============================================
    -- Step 11: Seed Splitter Types
    -- ============================================
    INSERT INTO "SplitterTypes" (
        "Id", "CompanyId", "DepartmentId", "Name", "Code", "TotalPorts", 
        "StandbyPortNumber", "Description", "DisplayOrder", "IsActive", 
        "CreatedAt", "UpdatedAt", "IsDeleted"
    ) VALUES
        (gen_random_uuid(), v_company_id, v_gpon_department_id, '1:8', '1_8', 8, NULL, '1:8 Splitter (8 ports)', 1, true, NOW(), NOW(), false),
        (gen_random_uuid(), v_company_id, v_gpon_department_id, '1:12', '1_12', 12, NULL, '1:12 Splitter (12 ports)', 2, true, NOW(), NOW(), false),
        (gen_random_uuid(), v_company_id, v_gpon_department_id, '1:32', '1_32', 32, 32, '1:32 Splitter (32 ports, port 32 is standby)', 3, true, NOW(), NOW(), false)
    ON CONFLICT DO NOTHING;
    
    RAISE NOTICE 'Splitter Types seeded';
    
    -- ============================================
    -- Step 12: Seed Skills
    -- ============================================
    INSERT INTO "Skills" (
        "Id", "CompanyId", "Name", "Code", "Category", "Description", 
        "DisplayOrder", "IsActive", "CreatedAt", "UpdatedAt", "IsDeleted"
    ) VALUES
        -- Fiber Skills (9)
        (gen_random_uuid(), v_company_id, 'Fiber cable installation (indoor)', 'FIBER_CABLE_INDOOR', 'FiberSkills', 'Installation of fiber cables in indoor environments', 1, true, NOW(), NOW(), false),
        (gen_random_uuid(), v_company_id, 'Fiber cable installation (outdoor/aerial)', 'FIBER_CABLE_OUTDOOR', 'FiberSkills', 'Installation of fiber cables in outdoor/aerial environments', 2, true, NOW(), NOW(), false),
        (gen_random_uuid(), v_company_id, 'Fiber splicing (mechanical)', 'FIBER_SPLICE_MECHANICAL', 'FiberSkills', 'Mechanical fiber splicing techniques', 3, true, NOW(), NOW(), false),
        (gen_random_uuid(), v_company_id, 'Fiber splicing (fusion)', 'FIBER_SPLICE_FUSION', 'FiberSkills', 'Fusion fiber splicing techniques', 4, true, NOW(), NOW(), false),
        (gen_random_uuid(), v_company_id, 'Fiber connector termination (SC/LC)', 'FIBER_CONNECTOR_TERMINATION', 'FiberSkills', 'Termination of SC/LC fiber connectors', 5, true, NOW(), NOW(), false),
        (gen_random_uuid(), v_company_id, 'OTDR testing', 'OTDR_TESTING', 'FiberSkills', 'Optical Time Domain Reflectometer testing', 6, true, NOW(), NOW(), false),
        (gen_random_uuid(), v_company_id, 'Optical power meter usage', 'OPTICAL_POWER_METER', 'FiberSkills', 'Using optical power meters for signal measurement', 7, true, NOW(), NOW(), false),
        (gen_random_uuid(), v_company_id, 'Visual fault locator (VFL)', 'VFL_USAGE', 'FiberSkills', 'Using Visual Fault Locator for fiber troubleshooting', 8, true, NOW(), NOW(), false),
        (gen_random_uuid(), v_company_id, 'Drop cable installation', 'DROP_CABLE_INSTALL', 'FiberSkills', 'Installation of drop cables from distribution point to customer premises', 9, true, NOW(), NOW(), false),
        -- Network & Equipment (7)
        (gen_random_uuid(), v_company_id, 'ONT installation and configuration', 'ONT_INSTALL_CONFIG', 'NetworkEquipment', 'Installation and configuration of Optical Network Terminals', 10, true, NOW(), NOW(), false),
        (gen_random_uuid(), v_company_id, 'Router setup and configuration', 'ROUTER_SETUP', 'NetworkEquipment', 'Setting up and configuring routers', 11, true, NOW(), NOW(), false),
        (gen_random_uuid(), v_company_id, 'Wi-Fi optimization', 'WIFI_OPTIMIZATION', 'NetworkEquipment', 'Optimizing Wi-Fi networks for performance', 12, true, NOW(), NOW(), false),
        (gen_random_uuid(), v_company_id, 'IPTV setup', 'IPTV_SETUP', 'NetworkEquipment', 'Setting up IPTV services', 13, true, NOW(), NOW(), false),
        (gen_random_uuid(), v_company_id, 'Mesh network installation', 'MESH_NETWORK', 'NetworkEquipment', 'Installation of mesh Wi-Fi networks', 14, true, NOW(), NOW(), false),
        (gen_random_uuid(), v_company_id, 'Basic network troubleshooting', 'NETWORK_TROUBLESHOOTING', 'NetworkEquipment', 'Basic troubleshooting of network issues', 15, true, NOW(), NOW(), false),
        (gen_random_uuid(), v_company_id, 'Speed test and verification', 'SPEED_TEST', 'NetworkEquipment', 'Performing speed tests and verifying service quality', 16, true, NOW(), NOW(), false),
        -- Installation Methods (6)
        (gen_random_uuid(), v_company_id, 'Aerial installation (pole-to-building)', 'AERIAL_INSTALL', 'InstallationMethods', 'Aerial fiber installation from pole to building', 17, true, NOW(), NOW(), false),
        (gen_random_uuid(), v_company_id, 'Underground/conduit installation', 'UNDERGROUND_INSTALL', 'InstallationMethods', 'Underground and conduit-based fiber installation', 18, true, NOW(), NOW(), false),
        (gen_random_uuid(), v_company_id, 'Indoor cable routing', 'INDOOR_ROUTING', 'InstallationMethods', 'Routing fiber cables within buildings', 19, true, NOW(), NOW(), false),
        (gen_random_uuid(), v_company_id, 'Wall penetration and patching', 'WALL_PENETRATION', 'InstallationMethods', 'Penetrating walls and patching holes for cable routing', 20, true, NOW(), NOW(), false),
        (gen_random_uuid(), v_company_id, 'Cable management and labeling', 'CABLE_MANAGEMENT', 'InstallationMethods', 'Proper cable management and labeling practices', 21, true, NOW(), NOW(), false),
        (gen_random_uuid(), v_company_id, 'Weatherproofing', 'WEATHERPROOFING', 'InstallationMethods', 'Weatherproofing outdoor installations', 22, true, NOW(), NOW(), false),
        -- Safety & Compliance (6)
        (gen_random_uuid(), v_company_id, 'Working at heights certified', 'HEIGHTS_CERTIFIED', 'SafetyCompliance', 'Certification for working at heights', 23, true, NOW(), NOW(), false),
        (gen_random_uuid(), v_company_id, 'Electrical safety awareness', 'ELECTRICAL_SAFETY', 'SafetyCompliance', 'Awareness of electrical safety procedures', 24, true, NOW(), NOW(), false),
        (gen_random_uuid(), v_company_id, 'TNB clearance procedures', 'TNB_CLEARANCE', 'SafetyCompliance', 'Understanding TNB (Tenaga Nasional Berhad) clearance procedures', 25, true, NOW(), NOW(), false),
        (gen_random_uuid(), v_company_id, 'Confined space entry', 'CONFINED_SPACE', 'SafetyCompliance', 'Certification for confined space entry', 26, true, NOW(), NOW(), false),
        (gen_random_uuid(), v_company_id, 'PPE usage', 'PPE_USAGE', 'SafetyCompliance', 'Proper use of Personal Protective Equipment', 27, true, NOW(), NOW(), false),
        (gen_random_uuid(), v_company_id, 'First Aid certified', 'FIRST_AID', 'SafetyCompliance', 'First Aid certification', 28, true, NOW(), NOW(), false),
        -- Customer Service (5)
        (gen_random_uuid(), v_company_id, 'Customer communication', 'CUSTOMER_COMMUNICATION', 'CustomerService', 'Effective communication with customers', 29, true, NOW(), NOW(), false),
        (gen_random_uuid(), v_company_id, 'Service demonstration', 'SERVICE_DEMO', 'CustomerService', 'Demonstrating services to customers', 30, true, NOW(), NOW(), false),
        (gen_random_uuid(), v_company_id, 'Technical explanation to customers', 'TECH_EXPLANATION', 'CustomerService', 'Explaining technical concepts to non-technical customers', 31, true, NOW(), NOW(), false),
        (gen_random_uuid(), v_company_id, 'Professional conduct', 'PROFESSIONAL_CONDUCT', 'CustomerService', 'Maintaining professional conduct during installations', 32, true, NOW(), NOW(), false),
        (gen_random_uuid(), v_company_id, 'Site cleanliness', 'SITE_CLEANLINESS', 'CustomerService', 'Maintaining cleanliness at installation sites', 33, true, NOW(), NOW(), false)
    ON CONFLICT DO NOTHING;
    
    RAISE NOTICE 'Skills seeded (33 skills)';
    
    -- ============================================
    -- Step 13: Seed Parser Templates
    -- ============================================
    INSERT INTO "ParserTemplates" (
        "Id", "CompanyId", "Name", "Code", "PartnerPattern", "SubjectPattern", 
        "OrderTypeCode", "Priority", "IsActive", "AutoApprove", "Description", 
        "CreatedAt", "IsDeleted"
    ) VALUES
        (gen_random_uuid(), v_company_id, 'TIME Activation', 'TIME_ACTIVATION', '*@time.com.my', '*Activation*', 'ACTIVATION', 100, true, false, 'Parses TIME FTTH/HSBB activation work orders', NOW(), false),
        (gen_random_uuid(), v_company_id, 'TIME Modification (Indoor)', 'TIME_MOD_INDOOR', '*@time.com.my', '*Modification*Indoor*', 'MODIFICATION_INDOOR', 95, true, false, 'Parses TIME indoor modification work orders', NOW(), false),
        (gen_random_uuid(), v_company_id, 'TIME Modification (Outdoor)', 'TIME_MOD_OUTDOOR', '*@time.com.my', '*Modification*Outdoor*', 'MODIFICATION_OUTDOOR', 95, true, false, 'Parses TIME outdoor modification work orders', NOW(), false),
        (gen_random_uuid(), v_company_id, 'TIME Modification (General)', 'TIME_MODIFICATION', '*@time.com.my', '*Modification*', 'MODIFICATION', 90, true, false, 'Parses TIME general modification work orders', NOW(), false),
        (gen_random_uuid(), v_company_id, 'TIME Termination', 'TIME_TERMINATION', '*@time.com.my', '*Termination*', 'TERMINATION', 80, true, false, 'Parses TIME termination/cancellation work orders', NOW(), false),
        (gen_random_uuid(), v_company_id, 'TIME Relocation', 'TIME_RELOCATION', '*@time.com.my', '*Relocation*', 'RELOCATION', 85, true, false, 'Parses TIME relocation work orders', NOW(), false),
        (gen_random_uuid(), v_company_id, 'TIME Assurance', 'TIME_ASSURANCE', '*@time.com.my', '*Assurance*', 'ASSURANCE', 70, true, false, 'Parses TIME assurance/troubleshooting work orders', NOW(), false),
        (gen_random_uuid(), v_company_id, 'TIME General (Fallback)', 'TIME_GENERAL', '*@time.com.my', '*Work Order*', 'GENERAL', 10, true, false, 'Fallback template for TIME work orders that don''t match other patterns', NOW(), false),
        (gen_random_uuid(), v_company_id, 'Celcom HSBB', 'CELCOM_HSBB', '*celcom*', '*HSBB*', 'ACTIVATION', 100, true, false, 'Parses Celcom HSBB work orders via TIME', NOW(), false),
        (gen_random_uuid(), v_company_id, 'TIME Payment Advice', 'TIME_PAYMENT_ADVICE', '*@time.com.my', '*Payment Advice*|*Payment*', NULL, 11, true, false, 'Parses payment advice emails from TIME', NOW(), false),
        (gen_random_uuid(), v_company_id, 'TIME Reschedule Notification', 'TIME_RESCHEDULE', '*@time.com.my', '*Reschedule*|*Rescheduled*', NULL, 12, true, false, 'Parses reschedule notification emails from TIME', NOW(), false),
        (gen_random_uuid(), v_company_id, 'TIME Customer Uncontactable', 'TIME_CUSTOMER_UNCONTACTABLE', '*@time.com.my', '*Customer Uncontactable*|*Uncontactable*', NULL, 13, true, false, 'Parses customer uncontactable notification emails from TIME', NOW(), false),
        (gen_random_uuid(), v_company_id, 'TIME RFB Meeting Notification', 'TIME_RFB', '*@time.com.my', '*RFB MEETING*|*RFB Meeting*|*Request for Building*', NULL, 14, true, false, 'Parses RFB meeting notification emails from TIME. Extracts building information, meeting details, and BM contact information.', NOW(), false),
        (gen_random_uuid(), v_company_id, 'TIME Withdrawal Notification', 'TIME_WITHDRAWAL', '*@time.com.my', '*Withdraw*|*Withdrawn*|*Confirm Withdraw*', NULL, 15, true, false, 'Parses withdrawal notification emails from TIME. Extracts Service ID and updates order status to Cancelled.', NOW(), false)
    ON CONFLICT DO NOTHING;
    
    RAISE NOTICE 'Parser Templates seeded (14 templates)';
    
    -- ============================================
    -- Step 14: Seed Guard Condition Definitions
    -- ============================================
    INSERT INTO "guard_condition_definitions" (
        "Id", "CompanyId", "key", "Name", "Description", "EntityType", 
        "ValidatorType", "ValidatorConfigJson", "IsActive", "DisplayOrder", 
        "CreatedAt", "UpdatedAt", "IsDeleted"
    ) VALUES
        (gen_random_uuid(), v_company_id, 'photosRequired', 'Photos Required', 'Checks if photos are uploaded for the order', 'Order', 'PhotosRequiredValidator', '{"checkFlag": true, "checkFiles": true}', true, 1, NOW(), NOW(), false),
        (gen_random_uuid(), v_company_id, 'docketUploaded', 'Docket Uploaded', 'Checks if docket is uploaded for the order', 'Order', 'DocketUploadedValidator', '{"checkFlag": true, "checkDockets": true}', true, 2, NOW(), NOW(), false),
        (gen_random_uuid(), v_company_id, 'splitterAssigned', 'Splitter Assigned', 'Checks if splitter port is assigned to the order', 'Order', 'SplitterAssignedValidator', NULL, true, 3, NOW(), NOW(), false),
        (gen_random_uuid(), v_company_id, 'serialNumbersValidated', 'Serial Numbers Validated', 'Checks if serial numbers are validated for the order', 'Order', 'SerialsValidatedValidator', NULL, true, 4, NOW(), NOW(), false),
        (gen_random_uuid(), v_company_id, 'materialsSpecified', 'Materials Specified', 'Checks if materials are specified for the order', 'Order', 'MaterialsSpecifiedValidator', NULL, true, 5, NOW(), NOW(), false),
        (gen_random_uuid(), v_company_id, 'siaAssigned', 'SI Assigned', 'Checks if Service Installer (SI) is assigned to the order', 'Order', 'SiAssignedValidator', NULL, true, 6, NOW(), NOW(), false),
        (gen_random_uuid(), v_company_id, 'appointmentDateSet', 'Appointment Date Set', 'Checks if appointment date is set for the order', 'Order', 'AppointmentDateSetValidator', NULL, true, 7, NOW(), NOW(), false),
        (gen_random_uuid(), v_company_id, 'buildingSelected', 'Building Selected', 'Checks if building is selected for the order', 'Order', 'BuildingSelectedValidator', NULL, true, 8, NOW(), NOW(), false),
        (gen_random_uuid(), v_company_id, 'customerContactProvided', 'Customer Contact Provided', 'Checks if customer contact (phone or email) is provided for the order', 'Order', 'CustomerContactProvidedValidator', NULL, true, 9, NOW(), NOW(), false),
        (gen_random_uuid(), v_company_id, 'noBlockersActive', 'No Active Blockers', 'Checks if there are no active blockers for the order', 'Order', 'NoActiveBlockersValidator', NULL, true, 10, NOW(), NOW(), false)
    ON CONFLICT DO NOTHING;
    
    RAISE NOTICE 'Guard Condition Definitions seeded (10 conditions)';
    
    -- ============================================
    -- Step 15: Seed Side Effect Definitions
    -- ============================================
    INSERT INTO "side_effect_definitions" (
        "Id", "CompanyId", "key", "Name", "Description", "EntityType", 
        "ExecutorType", "ExecutorConfigJson", "IsActive", "DisplayOrder", 
        "CreatedAt", "UpdatedAt", "IsDeleted"
    ) VALUES
        (gen_random_uuid(), v_company_id, 'notify', 'Send Notification', 'Sends a notification to relevant users when workflow transition occurs', 'Order', 'NotifySideEffectExecutor', '{"template": "OrderStatusChange"}', true, 1, NOW(), NOW(), false),
        (gen_random_uuid(), v_company_id, 'createStockMovement', 'Create Stock Movement', 'Creates stock movement records when workflow transition occurs', 'Order', 'CreateStockMovementSideEffectExecutor', NULL, true, 2, NOW(), NOW(), false),
        (gen_random_uuid(), v_company_id, 'createOrderStatusLog', 'Create Order Status Log', 'Creates an order status log entry when workflow transition occurs', 'Order', 'CreateOrderStatusLogSideEffectExecutor', NULL, true, 3, NOW(), NOW(), false),
        (gen_random_uuid(), v_company_id, 'updateOrderFlags', 'Update Order Flags', 'Updates order flags (DocketUploaded, PhotosUploaded, etc.) when workflow transition occurs', 'Order', 'UpdateOrderFlagsSideEffectExecutor', NULL, true, 4, NOW(), NOW(), false),
        (gen_random_uuid(), v_company_id, 'triggerInvoiceEligibility', 'Trigger Invoice Eligibility', 'Checks and updates invoice eligibility flag when workflow transition occurs', 'Order', 'TriggerInvoiceEligibilitySideEffectExecutor', '{"requireDocket": true, "requirePhotos": true, "requireSerials": true}', true, 5, NOW(), NOW(), false)
    ON CONFLICT DO NOTHING;
    
    RAISE NOTICE 'Side Effect Definitions seeded (5 side effects)';
    
    -- ============================================
    -- Step 16: Seed Global Settings
    -- ============================================
    INSERT INTO "GlobalSettings" (
        "Id", "Key", "Value", "ValueType", "Description", "Module", 
        "CreatedAt", "UpdatedAt", "IsDeleted"
    ) VALUES
        -- SMS Settings
        (gen_random_uuid(), 'SMS_Enabled', 'false', 'Bool', 'Enable SMS notifications', 'Notifications', NOW(), NOW(), false),
        (gen_random_uuid(), 'SMS_Provider', 'None', 'String', 'SMS provider (Twilio, SMS_Gateway, None)', 'Notifications', NOW(), NOW(), false),
        (gen_random_uuid(), 'SMS_Twilio_AccountSid', '', 'String', 'Twilio Account SID (encrypted)', 'Notifications', NOW(), NOW(), false),
        (gen_random_uuid(), 'SMS_Twilio_AuthToken', '', 'String', 'Twilio Auth Token (encrypted)', 'Notifications', NOW(), NOW(), false),
        (gen_random_uuid(), 'SMS_Twilio_FromNumber', '', 'String', 'Twilio From Phone Number', 'Notifications', NOW(), NOW(), false),
        (gen_random_uuid(), 'SMS_AutoSendOnStatusChange', 'false', 'Bool', 'Automatically send SMS when order status changes', 'Notifications', NOW(), NOW(), false),
        (gen_random_uuid(), 'SMS_RetryAttempts', '3', 'Int', 'Number of retry attempts for failed SMS', 'Notifications', NOW(), NOW(), false),
        (gen_random_uuid(), 'SMS_RetryDelaySeconds', '5', 'Int', 'Delay between SMS retry attempts (seconds)', 'Notifications', NOW(), NOW(), false),
        -- WhatsApp Settings
        (gen_random_uuid(), 'WhatsApp_Enabled', 'false', 'Bool', 'Enable WhatsApp notifications', 'Notifications', NOW(), NOW(), false),
        (gen_random_uuid(), 'WhatsApp_Provider', 'None', 'String', 'WhatsApp provider (Twilio, None)', 'Notifications', NOW(), NOW(), false),
        (gen_random_uuid(), 'WhatsApp_Twilio_AccountSid', '', 'String', 'Twilio Account SID for WhatsApp (encrypted)', 'Notifications', NOW(), NOW(), false),
        (gen_random_uuid(), 'WhatsApp_Twilio_AuthToken', '', 'String', 'Twilio Auth Token for WhatsApp (encrypted)', 'Notifications', NOW(), NOW(), false),
        (gen_random_uuid(), 'WhatsApp_Twilio_FromNumber', '', 'String', 'Twilio WhatsApp From Number', 'Notifications', NOW(), NOW(), false),
        (gen_random_uuid(), 'WhatsApp_AutoSendOnStatusChange', 'false', 'Bool', 'Automatically send WhatsApp when order status changes', 'Notifications', NOW(), NOW(), false),
        (gen_random_uuid(), 'WhatsApp_RetryAttempts', '3', 'Int', 'Number of retry attempts for failed WhatsApp', 'Notifications', NOW(), NOW(), false),
        (gen_random_uuid(), 'WhatsApp_RetryDelaySeconds', '5', 'Int', 'Delay between WhatsApp retry attempts (seconds)', 'Notifications', NOW(), NOW(), false),
        -- MyInvois E-Invoice Settings
        (gen_random_uuid(), 'EInvoice_Enabled', 'false', 'Bool', 'Enable e-invoice submission (MyInvois)', 'Billing', NOW(), NOW(), false),
        (gen_random_uuid(), 'EInvoice_Provider', 'Null', 'String', 'E-invoice provider (MyInvois, Null)', 'Billing', NOW(), NOW(), false),
        (gen_random_uuid(), 'MyInvois_BaseUrl', 'https://api-sandbox.myinvois.hasil.gov.my', 'String', 'MyInvois API base URL', 'Billing', NOW(), NOW(), false),
        (gen_random_uuid(), 'MyInvois_ClientId', '', 'String', 'MyInvois Client ID (encrypted)', 'Billing', NOW(), NOW(), false),
        (gen_random_uuid(), 'MyInvois_ClientSecret', '', 'String', 'MyInvois Client Secret (encrypted)', 'Billing', NOW(), NOW(), false),
        (gen_random_uuid(), 'MyInvois_Enabled', 'false', 'Bool', 'Enable MyInvois integration', 'Billing', NOW(), NOW(), false),
        -- Template Mapping Settings
        (gen_random_uuid(), 'Notification_Assigned_SmsTemplateCode', 'ASSIGNED', 'String', 'SMS template code for Assigned status', 'Notifications', NOW(), NOW(), false),
        (gen_random_uuid(), 'Notification_Assigned_WhatsAppTemplateCode', 'ASSIGNED', 'String', 'WhatsApp template code for Assigned status', 'Notifications', NOW(), NOW(), false),
        (gen_random_uuid(), 'Notification_OnTheWay_SmsTemplateCode', 'OTW', 'String', 'SMS template code for OnTheWay status', 'Notifications', NOW(), NOW(), false),
        (gen_random_uuid(), 'Notification_OnTheWay_WhatsAppTemplateCode', 'OTW', 'String', 'WhatsApp template code for OnTheWay status', 'Notifications', NOW(), NOW(), false),
        (gen_random_uuid(), 'Notification_MetCustomer_SmsTemplateCode', 'MET_CUSTOMER', 'String', 'SMS template code for MetCustomer status', 'Notifications', NOW(), NOW(), false),
        (gen_random_uuid(), 'Notification_MetCustomer_WhatsAppTemplateCode', 'MET_CUSTOMER', 'String', 'WhatsApp template code for MetCustomer status', 'Notifications', NOW(), NOW(), false),
        (gen_random_uuid(), 'Notification_OrderCompleted_SmsTemplateCode', 'IN_PROGRESS', 'String', 'SMS template code for OrderCompleted status', 'Notifications', NOW(), NOW(), false),
        (gen_random_uuid(), 'Notification_OrderCompleted_WhatsAppTemplateCode', 'IN_PROGRESS', 'String', 'WhatsApp template code for OrderCompleted status', 'Notifications', NOW(), NOW(), false),
        (gen_random_uuid(), 'Notification_Completed_SmsTemplateCode', 'COMPLETED', 'String', 'SMS template code for Completed status', 'Notifications', NOW(), NOW(), false),
        (gen_random_uuid(), 'Notification_Completed_WhatsAppTemplateCode', 'COMPLETED', 'String', 'WhatsApp template code for Completed status', 'Notifications', NOW(), NOW(), false),
        (gen_random_uuid(), 'Notification_Cancelled_SmsTemplateCode', 'CANCELLED', 'String', 'SMS template code for Cancelled status', 'Notifications', NOW(), NOW(), false),
        (gen_random_uuid(), 'Notification_Cancelled_WhatsAppTemplateCode', 'CANCELLED', 'String', 'WhatsApp template code for Cancelled status', 'Notifications', NOW(), NOW(), false),
        (gen_random_uuid(), 'Notification_ReschedulePendingApproval_SmsTemplateCode', 'RESCHEDULED', 'String', 'SMS template code for ReschedulePendingApproval status', 'Notifications', NOW(), NOW(), false),
        (gen_random_uuid(), 'Notification_ReschedulePendingApproval_WhatsAppTemplateCode', 'RESCHEDULED', 'String', 'WhatsApp template code for ReschedulePendingApproval status', 'Notifications', NOW(), NOW(), false),
        (gen_random_uuid(), 'Notification_Blocker_SmsTemplateCode', 'BLOCKER', 'String', 'SMS template code for Blocker status', 'Notifications', NOW(), NOW(), false),
        (gen_random_uuid(), 'Notification_Blocker_WhatsAppTemplateCode', 'BLOCKER', 'String', 'WhatsApp template code for Blocker status', 'Notifications', NOW(), NOW(), false),
        -- Unified Messaging Routing Settings
        (gen_random_uuid(), 'Messaging_SendSmsFallback', 'true', 'Bool', 'Send SMS alongside WhatsApp for non-urgent messages (optional fallback)', 'Notifications', NOW(), NOW(), false),
        (gen_random_uuid(), 'Messaging_AutoDetectWhatsApp', 'true', 'Bool', 'Automatically detect if customer uses WhatsApp by attempting to send', 'Notifications', NOW(), NOW(), false),
        (gen_random_uuid(), 'Messaging_WhatsAppRetryOnFailure', 'true', 'Bool', 'Retry with SMS if WhatsApp fails', 'Notifications', NOW(), NOW(), false)
    ON CONFLICT DO NOTHING;
    
    RAISE NOTICE 'Global Settings seeded (~30+ settings)';
    
    -- ============================================
    -- Step 17: Seed Movement Types
    -- ============================================
    INSERT INTO "MovementTypes" (
        "Id", "CompanyId", "Code", "Name", "Description", "Direction", 
        "RequiresFromLocation", "RequiresToLocation", "RequiresOrderId", 
        "RequiresServiceInstallerId", "RequiresPartnerId", "AffectsStockBalance", 
        "StockImpact", "IsActive", "SortOrder", "CreatedAt", "UpdatedAt", "IsDeleted"
    ) VALUES
        -- Inbound movements
        (gen_random_uuid(), v_company_id, 'GRN', 'Goods Receipt Note', 'Receipt of materials from supplier', 'In', false, true, false, false, false, true, 'Positive', true, 1, NOW(), NOW(), false),
        (gen_random_uuid(), v_company_id, 'ReturnFromSI', 'Return from Service Installer', 'Materials returned from service installer to warehouse', 'In', false, true, false, true, false, true, 'Positive', true, 2, NOW(), NOW(), false),
        (gen_random_uuid(), v_company_id, 'ReturnFromCustomer', 'Return from Customer', 'Materials returned from customer site', 'In', false, true, true, false, false, true, 'Positive', true, 3, NOW(), NOW(), false),
        -- Outbound movements
        (gen_random_uuid(), v_company_id, 'IssueToSI', 'Issue to Service Installer', 'Materials issued to service installer for installation', 'Out', true, false, false, true, false, true, 'Negative', true, 4, NOW(), NOW(), false),
        (gen_random_uuid(), v_company_id, 'IssueToOrder', 'Issue to Order', 'Materials issued directly to order/customer site', 'Out', true, false, true, false, false, true, 'Negative', true, 5, NOW(), NOW(), false),
        (gen_random_uuid(), v_company_id, 'ReturnFaulty', 'Return Faulty', 'Faulty materials returned to warehouse/RMA', 'In', false, true, true, true, false, true, 'Positive', true, 6, NOW(), NOW(), false),
        -- Transfer movements
        (gen_random_uuid(), v_company_id, 'Transfer', 'Transfer', 'Transfer materials between locations', 'Transfer', true, true, false, false, false, true, 'Neutral', true, 7, NOW(), NOW(), false),
        (gen_random_uuid(), v_company_id, 'TransferToRMA', 'Transfer to RMA', 'Transfer faulty materials to RMA location', 'Transfer', true, true, false, false, false, true, 'Neutral', true, 8, NOW(), NOW(), false),
        -- Adjustment movements
        (gen_random_uuid(), v_company_id, 'Adjustment', 'Stock Adjustment', 'Stock count adjustment (increase or decrease)', 'Adjust', false, true, false, false, false, true, 'Positive', true, 9, NOW(), NOW(), false),
        (gen_random_uuid(), v_company_id, 'AdjustmentDown', 'Stock Adjustment (Decrease)', 'Stock count adjustment (decrease)', 'Adjust', true, false, false, false, false, true, 'Negative', true, 10, NOW(), NOW(), false),
        -- Write-off movements
        (gen_random_uuid(), v_company_id, 'WriteOff', 'Write Off', 'Materials written off (damaged, expired, etc.)', 'Out', true, false, false, false, false, true, 'Negative', true, 11, NOW(), NOW(), false)
    ON CONFLICT DO NOTHING;
    
    RAISE NOTICE 'Movement Types seeded (11 types)';
    
    -- ============================================
    -- Step 18: Seed Location Types
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
    ON CONFLICT DO NOTHING;
    
    RAISE NOTICE 'Location Types seeded (6 types)';
    
    -- ============================================
    -- Step 19: Seed Default Material Categories (if none exist)
    -- ============================================
    -- Only seed if no material categories exist
    IF NOT EXISTS (SELECT 1 FROM "MaterialCategories" WHERE "IsDeleted" = false) THEN
        INSERT INTO "MaterialCategories" (
            "Id", "CompanyId", "Name", "Description", "DisplayOrder", 
            "IsActive", "CreatedAt", "UpdatedAt", "IsDeleted"
        ) VALUES
            (gen_random_uuid(), v_company_id, 'ONU', 'Optical Network Units - Customer premises equipment', 1, true, NOW(), NOW(), false),
            (gen_random_uuid(), v_company_id, 'Fiber Cable', 'Fiber optic cables (indoor, outdoor, aerial)', 2, true, NOW(), NOW(), false),
            (gen_random_uuid(), v_company_id, 'Splitter', 'Optical splitters for fiber distribution', 3, true, NOW(), NOW(), false),
            (gen_random_uuid(), v_company_id, 'Accessories', 'Termination boxes, connectors, adapters', 4, true, NOW(), NOW(), false),
            (gen_random_uuid(), v_company_id, 'Distribution', 'Distribution units, cabinets, enclosures', 5, true, NOW(), NOW(), false),
            (gen_random_uuid(), v_company_id, 'Tools', 'Installation tools and equipment', 6, true, NOW(), NOW(), false),
            (gen_random_uuid(), v_company_id, 'Consumables', 'Consumable items (cable ties, labels, etc.)', 7, true, NOW(), NOW(), false),
            (gen_random_uuid(), v_company_id, 'Spare Parts', 'Spare parts and replacement components', 8, true, NOW(), NOW(), false)
        ON CONFLICT DO NOTHING;
        
        RAISE NOTICE 'Default Material Categories seeded (8 categories)';
    ELSE
        RAISE NOTICE 'Material Categories already exist, skipping default seed';
    END IF;
    
    RAISE NOTICE '========================================';
    RAISE NOTICE 'Seed data migration completed successfully!';
    RAISE NOTICE 'Company ID: %', v_company_id;
    RAISE NOTICE 'GPON Department ID: %', v_gpon_department_id;
    RAISE NOTICE '========================================';
    
EXCEPTION
    WHEN OTHERS THEN
        RAISE EXCEPTION 'Error seeding database: %', SQLERRM;
END $$;

