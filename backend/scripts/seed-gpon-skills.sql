-- ============================================
-- GPON Skills Seed Script
-- ============================================
-- Seeds 10 essential GPON skills for service installers
-- Categories mapped to existing schema:
--   - Technical Installation -> InstallationMethods
--   - Testing & Measurement -> FiberSkills
--   - Network Equipment -> NetworkEquipment
--   - Customer Service -> CustomerService
--   - Documentation -> CustomerService (closest match)
--   - Safety & Compliance -> SafetyCompliance
-- ============================================

DO $$
DECLARE
    v_company_id UUID;
    v_skill_id UUID;
    v_skill_code TEXT;
    v_skill_name TEXT;
    v_skill_category TEXT;
    v_skill_description TEXT;
    v_display_order INT := 1;
BEGIN
    -- Get company ID (can be NULL for single-company mode)
    SELECT "Id" INTO v_company_id FROM "Companies" ORDER BY "CreatedAt" LIMIT 1;
    
    IF v_company_id IS NULL THEN
        RAISE NOTICE 'No company found. Skills will be created without CompanyId.';
    END IF;

    -- ============================================
    -- 1. Fiber Splicing (Technical Installation -> InstallationMethods)
    -- ============================================
    v_skill_code := 'FIBER_SPLICING';
    v_skill_name := 'Fiber Splicing';
    v_skill_category := 'InstallationMethods';
    v_skill_description := 'Fusion and mechanical fiber splicing techniques for connecting fiber optic cables';
    
    SELECT "Id" INTO v_skill_id 
    FROM "Skills" 
    WHERE "Code" = v_skill_code 
      AND ("CompanyId" = v_company_id OR ("CompanyId" IS NULL AND v_company_id IS NULL))
      AND "IsDeleted" = false
    LIMIT 1;
    
    IF v_skill_id IS NULL THEN
        v_skill_id := gen_random_uuid();
        INSERT INTO "Skills" (
            "Id", "CompanyId", "Name", "Code", "Category", "Description", 
            "IsActive", "IsDeleted", "DisplayOrder", "CreatedAt", "UpdatedAt"
        ) VALUES (
            v_skill_id,
            v_company_id,
            v_skill_name,
            v_skill_code,
            v_skill_category,
            v_skill_description,
            true,
            false,
            v_display_order,
            NOW(),
            NOW()
        );
        RAISE NOTICE 'Created skill: % (%)', v_skill_name, v_skill_category;
    ELSE
        RAISE NOTICE 'Skill already exists: % (%)', v_skill_name, v_skill_category;
    END IF;
    v_display_order := v_display_order + 1;

    -- ============================================
    -- 2. OTDR Testing (Testing & Measurement -> FiberSkills)
    -- ============================================
    v_skill_code := 'OTDR_TESTING';
    v_skill_name := 'OTDR Testing';
    v_skill_category := 'FiberSkills';
    v_skill_description := 'Optical Time Domain Reflectometer testing for fiber optic cable diagnostics and troubleshooting';
    
    SELECT "Id" INTO v_skill_id 
    FROM "Skills" 
    WHERE "Code" = v_skill_code 
      AND ("CompanyId" = v_company_id OR ("CompanyId" IS NULL AND v_company_id IS NULL))
      AND "IsDeleted" = false
    LIMIT 1;
    
    IF v_skill_id IS NULL THEN
        v_skill_id := gen_random_uuid();
        INSERT INTO "Skills" (
            "Id", "CompanyId", "Name", "Code", "Category", "Description", 
            "IsActive", "IsDeleted", "DisplayOrder", "CreatedAt", "UpdatedAt"
        ) VALUES (
            v_skill_id,
            v_company_id,
            v_skill_name,
            v_skill_code,
            v_skill_category,
            v_skill_description,
            true,
            false,
            v_display_order,
            NOW(),
            NOW()
        );
        RAISE NOTICE 'Created skill: % (%)', v_skill_name, v_skill_category;
    ELSE
        RAISE NOTICE 'Skill already exists: % (%)', v_skill_name, v_skill_category;
    END IF;
    v_display_order := v_display_order + 1;

    -- ============================================
    -- 3. ONT Installation (Network Equipment)
    -- ============================================
    v_skill_code := 'ONT_INSTALLATION';
    v_skill_name := 'ONT Installation';
    v_skill_category := 'NetworkEquipment';
    v_skill_description := 'Installation and configuration of Optical Network Terminals (ONT) for GPON services';
    
    SELECT "Id" INTO v_skill_id 
    FROM "Skills" 
    WHERE "Code" = v_skill_code 
      AND ("CompanyId" = v_company_id OR ("CompanyId" IS NULL AND v_company_id IS NULL))
      AND "IsDeleted" = false
    LIMIT 1;
    
    IF v_skill_id IS NULL THEN
        v_skill_id := gen_random_uuid();
        INSERT INTO "Skills" (
            "Id", "CompanyId", "Name", "Code", "Category", "Description", 
            "IsActive", "IsDeleted", "DisplayOrder", "CreatedAt", "UpdatedAt"
        ) VALUES (
            v_skill_id,
            v_company_id,
            v_skill_name,
            v_skill_code,
            v_skill_category,
            v_skill_description,
            true,
            false,
            v_display_order,
            NOW(),
            NOW()
        );
        RAISE NOTICE 'Created skill: % (%)', v_skill_name, v_skill_category;
    ELSE
        RAISE NOTICE 'Skill already exists: % (%)', v_skill_name, v_skill_category;
    END IF;
    v_display_order := v_display_order + 1;

    -- ============================================
    -- 4. Cable Installation (Technical Installation -> InstallationMethods)
    -- ============================================
    v_skill_code := 'CABLE_INSTALLATION';
    v_skill_name := 'Cable Installation';
    v_skill_category := 'InstallationMethods';
    v_skill_description := 'Installation of fiber optic cables in indoor, outdoor, and aerial environments';
    
    SELECT "Id" INTO v_skill_id 
    FROM "Skills" 
    WHERE "Code" = v_skill_code 
      AND ("CompanyId" = v_company_id OR ("CompanyId" IS NULL AND v_company_id IS NULL))
      AND "IsDeleted" = false
    LIMIT 1;
    
    IF v_skill_id IS NULL THEN
        v_skill_id := gen_random_uuid();
        INSERT INTO "Skills" (
            "Id", "CompanyId", "Name", "Code", "Category", "Description", 
            "IsActive", "IsDeleted", "DisplayOrder", "CreatedAt", "UpdatedAt"
        ) VALUES (
            v_skill_id,
            v_company_id,
            v_skill_name,
            v_skill_code,
            v_skill_category,
            v_skill_description,
            true,
            false,
            v_display_order,
            NOW(),
            NOW()
        );
        RAISE NOTICE 'Created skill: % (%)', v_skill_name, v_skill_category;
    ELSE
        RAISE NOTICE 'Skill already exists: % (%)', v_skill_name, v_skill_category;
    END IF;
    v_display_order := v_display_order + 1;

    -- ============================================
    -- 5. Troubleshooting (Testing & Measurement -> FiberSkills)
    -- ============================================
    v_skill_code := 'TROUBLESHOOTING';
    v_skill_name := 'Troubleshooting';
    v_skill_category := 'FiberSkills';
    v_skill_description := 'Diagnostic and troubleshooting skills for fiber optic network issues';
    
    SELECT "Id" INTO v_skill_id 
    FROM "Skills" 
    WHERE "Code" = v_skill_code 
      AND ("CompanyId" = v_company_id OR ("CompanyId" IS NULL AND v_company_id IS NULL))
      AND "IsDeleted" = false
    LIMIT 1;
    
    IF v_skill_id IS NULL THEN
        v_skill_id := gen_random_uuid();
        INSERT INTO "Skills" (
            "Id", "CompanyId", "Name", "Code", "Category", "Description", 
            "IsActive", "IsDeleted", "DisplayOrder", "CreatedAt", "UpdatedAt"
        ) VALUES (
            v_skill_id,
            v_company_id,
            v_skill_name,
            v_skill_code,
            v_skill_category,
            v_skill_description,
            true,
            false,
            v_display_order,
            NOW(),
            NOW()
        );
        RAISE NOTICE 'Created skill: % (%)', v_skill_name, v_skill_category;
    ELSE
        RAISE NOTICE 'Skill already exists: % (%)', v_skill_name, v_skill_category;
    END IF;
    v_display_order := v_display_order + 1;

    -- ============================================
    -- 6. Customer Service
    -- ============================================
    v_skill_code := 'CUSTOMER_SERVICE';
    v_skill_name := 'Customer Service';
    v_skill_category := 'CustomerService';
    v_skill_description := 'Professional customer service skills including communication, problem-solving, and service demonstration';
    
    SELECT "Id" INTO v_skill_id 
    FROM "Skills" 
    WHERE "Code" = v_skill_code 
      AND ("CompanyId" = v_company_id OR ("CompanyId" IS NULL AND v_company_id IS NULL))
      AND "IsDeleted" = false
    LIMIT 1;
    
    IF v_skill_id IS NULL THEN
        v_skill_id := gen_random_uuid();
        INSERT INTO "Skills" (
            "Id", "CompanyId", "Name", "Code", "Category", "Description", 
            "IsActive", "IsDeleted", "DisplayOrder", "CreatedAt", "UpdatedAt"
        ) VALUES (
            v_skill_id,
            v_company_id,
            v_skill_name,
            v_skill_code,
            v_skill_category,
            v_skill_description,
            true,
            false,
            v_display_order,
            NOW(),
            NOW()
        );
        RAISE NOTICE 'Created skill: % (%)', v_skill_name, v_skill_category;
    ELSE
        RAISE NOTICE 'Skill already exists: % (%)', v_skill_name, v_skill_category;
    END IF;
    v_display_order := v_display_order + 1;

    -- ============================================
    -- 7. Documentation (CustomerService category)
    -- ============================================
    v_skill_code := 'DOCUMENTATION';
    v_skill_name := 'Documentation';
    v_skill_category := 'CustomerService';
    v_skill_description := 'Proper documentation of installation work, including photos, forms, and service records';
    
    SELECT "Id" INTO v_skill_id 
    FROM "Skills" 
    WHERE "Code" = v_skill_code 
      AND ("CompanyId" = v_company_id OR ("CompanyId" IS NULL AND v_company_id IS NULL))
      AND "IsDeleted" = false
    LIMIT 1;
    
    IF v_skill_id IS NULL THEN
        v_skill_id := gen_random_uuid();
        INSERT INTO "Skills" (
            "Id", "CompanyId", "Name", "Code", "Category", "Description", 
            "IsActive", "IsDeleted", "DisplayOrder", "CreatedAt", "UpdatedAt"
        ) VALUES (
            v_skill_id,
            v_company_id,
            v_skill_name,
            v_skill_code,
            v_skill_category,
            v_skill_description,
            true,
            false,
            v_display_order,
            NOW(),
            NOW()
        );
        RAISE NOTICE 'Created skill: % (%)', v_skill_name, v_skill_category;
    ELSE
        RAISE NOTICE 'Skill already exists: % (%)', v_skill_name, v_skill_category;
    END IF;
    v_display_order := v_display_order + 1;

    -- ============================================
    -- 8. Safety Certified (Safety & Compliance)
    -- ============================================
    v_skill_code := 'SAFETY_CERTIFIED';
    v_skill_name := 'Safety Certified';
    v_skill_category := 'SafetyCompliance';
    v_skill_description := 'Certified in workplace safety procedures, including working at heights, electrical safety, and PPE usage';
    
    SELECT "Id" INTO v_skill_id 
    FROM "Skills" 
    WHERE "Code" = v_skill_code 
      AND ("CompanyId" = v_company_id OR ("CompanyId" IS NULL AND v_company_id IS NULL))
      AND "IsDeleted" = false
    LIMIT 1;
    
    IF v_skill_id IS NULL THEN
        v_skill_id := gen_random_uuid();
        INSERT INTO "Skills" (
            "Id", "CompanyId", "Name", "Code", "Category", "Description", 
            "IsActive", "IsDeleted", "DisplayOrder", "CreatedAt", "UpdatedAt"
        ) VALUES (
            v_skill_id,
            v_company_id,
            v_skill_name,
            v_skill_code,
            v_skill_category,
            v_skill_description,
            true,
            false,
            v_display_order,
            NOW(),
            NOW()
        );
        RAISE NOTICE 'Created skill: % (%)', v_skill_name, v_skill_category;
    ELSE
        RAISE NOTICE 'Skill already exists: % (%)', v_skill_name, v_skill_category;
    END IF;
    v_display_order := v_display_order + 1;

    -- ============================================
    -- 9. Pole Climbing (Safety & Compliance)
    -- ============================================
    v_skill_code := 'POLE_CLIMBING';
    v_skill_name := 'Pole Climbing';
    v_skill_category := 'SafetyCompliance';
    v_skill_description := 'Certified in pole climbing and working at heights for aerial fiber installation';
    
    SELECT "Id" INTO v_skill_id 
    FROM "Skills" 
    WHERE "Code" = v_skill_code 
      AND ("CompanyId" = v_company_id OR ("CompanyId" IS NULL AND v_company_id IS NULL))
      AND "IsDeleted" = false
    LIMIT 1;
    
    IF v_skill_id IS NULL THEN
        v_skill_id := gen_random_uuid();
        INSERT INTO "Skills" (
            "Id", "CompanyId", "Name", "Code", "Category", "Description", 
            "IsActive", "IsDeleted", "DisplayOrder", "CreatedAt", "UpdatedAt"
        ) VALUES (
            v_skill_id,
            v_company_id,
            v_skill_name,
            v_skill_code,
            v_skill_category,
            v_skill_description,
            true,
            false,
            v_display_order,
            NOW(),
            NOW()
        );
        RAISE NOTICE 'Created skill: % (%)', v_skill_name, v_skill_category;
    ELSE
        RAISE NOTICE 'Skill already exists: % (%)', v_skill_name, v_skill_category;
    END IF;
    v_display_order := v_display_order + 1;

    -- ============================================
    -- 10. Router Configuration (Network Equipment)
    -- ============================================
    v_skill_code := 'ROUTER_CONFIGURATION';
    v_skill_name := 'Router Configuration';
    v_skill_category := 'NetworkEquipment';
    v_skill_description := 'Configuration and setup of routers, including Wi-Fi optimization and network troubleshooting';
    
    SELECT "Id" INTO v_skill_id 
    FROM "Skills" 
    WHERE "Code" = v_skill_code 
      AND ("CompanyId" = v_company_id OR ("CompanyId" IS NULL AND v_company_id IS NULL))
      AND "IsDeleted" = false
    LIMIT 1;
    
    IF v_skill_id IS NULL THEN
        v_skill_id := gen_random_uuid();
        INSERT INTO "Skills" (
            "Id", "CompanyId", "Name", "Code", "Category", "Description", 
            "IsActive", "IsDeleted", "DisplayOrder", "CreatedAt", "UpdatedAt"
        ) VALUES (
            v_skill_id,
            v_company_id,
            v_skill_name,
            v_skill_code,
            v_skill_category,
            v_skill_description,
            true,
            false,
            v_display_order,
            NOW(),
            NOW()
        );
        RAISE NOTICE 'Created skill: % (%)', v_skill_name, v_skill_category;
    ELSE
        RAISE NOTICE 'Skill already exists: % (%)', v_skill_name, v_skill_category;
    END IF;

    RAISE NOTICE 'GPON Skills seeding completed successfully!';
END $$;

