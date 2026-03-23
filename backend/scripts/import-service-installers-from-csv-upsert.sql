-- Import/Update Service Installers from CSV (UPSERT version)
-- This script updates existing records or inserts new ones
-- Run this AFTER applying the InstallerType migration

DO $$
DECLARE
    v_department_id UUID;
    v_company_id UUID;
    v_installer_id UUID;
    v_updated_count INT;
BEGIN
    -- Get GPON Department ID
    SELECT "Id" INTO v_department_id
    FROM "Departments"
    WHERE "Name" = 'GPON' AND "IsActive" = true
    LIMIT 1;

    -- Get Company ID (first active company, or use NULL for single-company mode)
    SELECT "Id" INTO v_company_id
    FROM "Companies"
    WHERE "IsActive" = true
    LIMIT 1;

    IF v_department_id IS NULL THEN
        RAISE EXCEPTION 'GPON Department not found. Please seed departments first.';
    END IF;

    RAISE NOTICE 'Using Department ID: %', v_department_id;
    RAISE NOTICE 'Using Company ID: %', COALESCE(v_company_id::text, 'NULL');

    -- Helper function to upsert a service installer
    -- This will update if exists (by Name + DepartmentId), or insert if new
    
    -- CHANDRASEKARAN VEERIAH
    UPDATE "ServiceInstallers"
    SET 
        "Phone" = '+60 16-491 1325',
        "Email" = NULL,
        "SiLevel" = 'Senior',
        "InstallerType" = 'InHouse',
        "IsSubcontractor" = false,
        "IsActive" = true,
        "UpdatedAt" = NOW()
    WHERE "Name" = 'CHANDRASEKARAN VEERIAH' AND "DepartmentId" = v_department_id;
    
    GET DIAGNOSTICS v_updated_count = ROW_COUNT;
    IF v_updated_count = 0 THEN
        INSERT INTO "ServiceInstallers" (
            "Id", "CompanyId", "DepartmentId", "Name", "Phone", "Email", 
            "SiLevel", "InstallerType", "IsSubcontractor", "IsActive", 
            "CreatedAt", "UpdatedAt"
        ) VALUES (
            gen_random_uuid(), v_company_id, v_department_id, 
            'CHANDRASEKARAN VEERIAH', '+60 16-491 1325', NULL,
            'Senior', 'InHouse', false, true,
            NOW(), NOW()
        );
    END IF;

    -- EDWIN DASS A/L YESU DAS
    UPDATE "ServiceInstallers"
    SET 
        "Phone" = '+60 11-1081 9064',
        "Email" = NULL,
        "SiLevel" = 'Junior',
        "InstallerType" = 'Subcontractor',
        "IsSubcontractor" = true,
        "IsActive" = true,
        "UpdatedAt" = NOW()
    WHERE "Name" = 'EDWIN DASS A/L YESU DAS' AND "DepartmentId" = v_department_id;
    
    GET DIAGNOSTICS v_updated_count = ROW_COUNT;
    IF v_updated_count = 0 THEN
        INSERT INTO "ServiceInstallers" (
            "Id", "CompanyId", "DepartmentId", "Name", "Phone", "Email", 
            "SiLevel", "InstallerType", "IsSubcontractor", "IsActive", 
            "CreatedAt", "UpdatedAt"
        ) VALUES (
            gen_random_uuid(), v_company_id, v_department_id, 
            'EDWIN DASS A/L YESU DAS', '+60 11-1081 9064', NULL,
            'Junior', 'Subcontractor', true, true,
            NOW(), NOW()
        );
    END IF;

    -- ISHAAN
    UPDATE "ServiceInstallers"
    SET 
        "Phone" = '+60125156965',
        "Email" = NULL,
        "SiLevel" = 'Junior',
        "InstallerType" = 'InHouse',
        "IsSubcontractor" = false,
        "IsActive" = true,
        "UpdatedAt" = NOW()
    WHERE "Name" = 'ISHAAN' AND "DepartmentId" = v_department_id;
    
    GET DIAGNOSTICS v_updated_count = ROW_COUNT;
    IF v_updated_count = 0 THEN
        INSERT INTO "ServiceInstallers" (
            "Id", "CompanyId", "DepartmentId", "Name", "Phone", "Email", 
            "SiLevel", "InstallerType", "IsSubcontractor", "IsActive", 
            "CreatedAt", "UpdatedAt"
        ) VALUES (
            gen_random_uuid(), v_company_id, v_department_id, 
            'ISHAAN', '+60125156965', NULL,
            'Junior', 'InHouse', false, true,
            NOW(), NOW()
        );
    END IF;

    -- K. MARIAPPAN A/L KUPPATHAN
    UPDATE "ServiceInstallers"
    SET 
        "Phone" = '+60 17-676 7625',
        "Email" = NULL,
        "SiLevel" = 'Senior',
        "InstallerType" = 'Subcontractor',
        "IsSubcontractor" = true,
        "IsActive" = true,
        "UpdatedAt" = NOW()
    WHERE "Name" = 'K. MARIAPPAN A/L KUPPATHAN' AND "DepartmentId" = v_department_id;
    
    GET DIAGNOSTICS v_updated_count = ROW_COUNT;
    IF v_updated_count = 0 THEN
        INSERT INTO "ServiceInstallers" (
            "Id", "CompanyId", "DepartmentId", "Name", "Phone", "Email", 
            "SiLevel", "InstallerType", "IsSubcontractor", "IsActive", 
            "CreatedAt", "UpdatedAt"
        ) VALUES (
            gen_random_uuid(), v_company_id, v_department_id, 
            'K. MARIAPPAN A/L KUPPATHAN', '+60 17-676 7625', NULL,
            'Senior', 'Subcontractor', true, true,
            NOW(), NOW()
        );
    END IF;

    -- KLAVINN RAJ A/L AROKKIASAMY
    UPDATE "ServiceInstallers"
    SET 
        "Phone" = '+60 12-291 6386',
        "Email" = NULL,
        "SiLevel" = 'Senior',
        "InstallerType" = 'Subcontractor',
        "IsSubcontractor" = true,
        "IsActive" = true,
        "UpdatedAt" = NOW()
    WHERE "Name" = 'KLAVINN RAJ A/L AROKKIASAMY' AND "DepartmentId" = v_department_id;
    
    GET DIAGNOSTICS v_updated_count = ROW_COUNT;
    IF v_updated_count = 0 THEN
        INSERT INTO "ServiceInstallers" (
            "Id", "CompanyId", "DepartmentId", "Name", "Phone", "Email", 
            "SiLevel", "InstallerType", "IsSubcontractor", "IsActive", 
            "CreatedAt", "UpdatedAt"
        ) VALUES (
            gen_random_uuid(), v_company_id, v_department_id, 
            'KLAVINN RAJ A/L AROKKIASAMY', '+60 12-291 6386', NULL,
            'Senior', 'Subcontractor', true, true,
            NOW(), NOW()
        );
    END IF;

    -- MOHAMMAD ALIYASMAAN
    UPDATE "ServiceInstallers"
    SET 
        "Phone" = '+60 17-494 7242',
        "Email" = NULL,
        "SiLevel" = 'Junior',
        "InstallerType" = 'InHouse',
        "IsSubcontractor" = false,
        "IsActive" = true,
        "UpdatedAt" = NOW()
    WHERE "Name" = 'MOHAMMAD ALIYASMAAN' AND "DepartmentId" = v_department_id;
    
    GET DIAGNOSTICS v_updated_count = ROW_COUNT;
    IF v_updated_count = 0 THEN
        INSERT INTO "ServiceInstallers" (
            "Id", "CompanyId", "DepartmentId", "Name", "Phone", "Email", 
            "SiLevel", "InstallerType", "IsSubcontractor", "IsActive", 
            "CreatedAt", "UpdatedAt"
        ) VALUES (
            gen_random_uuid(), v_company_id, v_department_id, 
            'MOHAMMAD ALIYASMAAN', '+60 17-494 7242', NULL,
            'Junior', 'InHouse', false, true,
            NOW(), NOW()
        );
    END IF;

    -- MOHD TAKYIN BIN CHE ALI
    UPDATE "ServiceInstallers"
    SET 
        "Phone" = '+60 13-515 5900',
        "Email" = NULL,
        "SiLevel" = 'Senior',
        "InstallerType" = 'Subcontractor',
        "IsSubcontractor" = true,
        "IsActive" = true,
        "UpdatedAt" = NOW()
    WHERE "Name" = 'MOHD TAKYIN BIN CHE ALI' AND "DepartmentId" = v_department_id;
    
    GET DIAGNOSTICS v_updated_count = ROW_COUNT;
    IF v_updated_count = 0 THEN
        INSERT INTO "ServiceInstallers" (
            "Id", "CompanyId", "DepartmentId", "Name", "Phone", "Email", 
            "SiLevel", "InstallerType", "IsSubcontractor", "IsActive", 
            "CreatedAt", "UpdatedAt"
        ) VALUES (
            gen_random_uuid(), v_company_id, v_department_id, 
            'MOHD TAKYIN BIN CHE ALI', '+60 13-515 5900', NULL,
            'Senior', 'Subcontractor', true, true,
            NOW(), NOW()
        );
    END IF;

    -- MUHAMAD QAIRUL HAIKAL BIN ABDULLAH
    UPDATE "ServiceInstallers"
    SET 
        "Phone" = '+60 11-1690 3721',
        "Email" = NULL,
        "SiLevel" = 'Junior',
        "InstallerType" = 'InHouse',
        "IsSubcontractor" = false,
        "IsActive" = true,
        "UpdatedAt" = NOW()
    WHERE "Name" = 'MUHAMAD QAIRUL HAIKAL BIN ABDULLAH' AND "DepartmentId" = v_department_id;
    
    GET DIAGNOSTICS v_updated_count = ROW_COUNT;
    IF v_updated_count = 0 THEN
        INSERT INTO "ServiceInstallers" (
            "Id", "CompanyId", "DepartmentId", "Name", "Phone", "Email", 
            "SiLevel", "InstallerType", "IsSubcontractor", "IsActive", 
            "CreatedAt", "UpdatedAt"
        ) VALUES (
            gen_random_uuid(), v_company_id, v_department_id, 
            'MUHAMAD QAIRUL HAIKAL BIN ABDULLAH', '+60 11-1690 3721', NULL,
            'Junior', 'InHouse', false, true,
            NOW(), NOW()
        );
    END IF;

    -- MUHAMMAD AMMAR BIN MOHD GHAZI
    UPDATE "ServiceInstallers"
    SET 
        "Phone" = '+60 11-7228 8644',
        "Email" = NULL,
        "SiLevel" = 'Junior',
        "InstallerType" = 'InHouse',
        "IsSubcontractor" = false,
        "IsActive" = true,
        "UpdatedAt" = NOW()
    WHERE "Name" = 'MUHAMMAD AMMAR BIN MOHD GHAZI' AND "DepartmentId" = v_department_id;
    
    GET DIAGNOSTICS v_updated_count = ROW_COUNT;
    IF v_updated_count = 0 THEN
        INSERT INTO "ServiceInstallers" (
            "Id", "CompanyId", "DepartmentId", "Name", "Phone", "Email", 
            "SiLevel", "InstallerType", "IsSubcontractor", "IsActive", 
            "CreatedAt", "UpdatedAt"
        ) VALUES (
            gen_random_uuid(), v_company_id, v_department_id, 
            'MUHAMMAD AMMAR BIN MOHD GHAZI', '+60 11-7228 8644', NULL,
            'Junior', 'InHouse', false, true,
            NOW(), NOW()
        );
    END IF;

    -- MUNIANDY A/L SOORINARAYANAN
    UPDATE "ServiceInstallers"
    SET 
        "Phone" = '+60 16-319 8867',
        "Email" = NULL,
        "SiLevel" = 'Senior',
        "InstallerType" = 'Subcontractor',
        "IsSubcontractor" = true,
        "IsActive" = true,
        "UpdatedAt" = NOW()
    WHERE "Name" = 'MUNIANDY A/L SOORINARAYANAN' AND "DepartmentId" = v_department_id;
    
    GET DIAGNOSTICS v_updated_count = ROW_COUNT;
    IF v_updated_count = 0 THEN
        INSERT INTO "ServiceInstallers" (
            "Id", "CompanyId", "DepartmentId", "Name", "Phone", "Email", 
            "SiLevel", "InstallerType", "IsSubcontractor", "IsActive", 
            "CreatedAt", "UpdatedAt"
        ) VALUES (
            gen_random_uuid(), v_company_id, v_department_id, 
            'MUNIANDY A/L SOORINARAYANAN', '+60 16-319 8867', NULL,
            'Senior', 'Subcontractor', true, true,
            NOW(), NOW()
        );
    END IF;

    -- NORAFIZ HAFIZUL BIN ABDULLAH
    UPDATE "ServiceInstallers"
    SET 
        "Phone" = '+60 17-943 7241',
        "Email" = NULL,
        "SiLevel" = 'Senior',
        "InstallerType" = 'InHouse',
        "IsSubcontractor" = false,
        "IsActive" = true,
        "UpdatedAt" = NOW()
    WHERE "Name" = 'NORAFIZ HAFIZUL BIN ABDULLAH' AND "DepartmentId" = v_department_id;
    
    GET DIAGNOSTICS v_updated_count = ROW_COUNT;
    IF v_updated_count = 0 THEN
        INSERT INTO "ServiceInstallers" (
            "Id", "CompanyId", "DepartmentId", "Name", "Phone", "Email", 
            "SiLevel", "InstallerType", "IsSubcontractor", "IsActive", 
            "CreatedAt", "UpdatedAt"
        ) VALUES (
            gen_random_uuid(), v_company_id, v_department_id, 
            'NORAFIZ HAFIZUL BIN ABDULLAH', '+60 17-943 7241', NULL,
            'Senior', 'InHouse', false, true,
            NOW(), NOW()
        );
    END IF;

    -- RAVEEN NAIR A/L K RAHMAN
    UPDATE "ServiceInstallers"
    SET 
        "Phone" = '+60 11-1081 8049',
        "Email" = NULL,
        "SiLevel" = 'Senior',
        "InstallerType" = 'Subcontractor',
        "IsSubcontractor" = true,
        "IsActive" = true,
        "UpdatedAt" = NOW()
    WHERE "Name" = 'RAVEEN NAIR A/L K RAHMAN' AND "DepartmentId" = v_department_id;
    
    GET DIAGNOSTICS v_updated_count = ROW_COUNT;
    IF v_updated_count = 0 THEN
        INSERT INTO "ServiceInstallers" (
            "Id", "CompanyId", "DepartmentId", "Name", "Phone", "Email", 
            "SiLevel", "InstallerType", "IsSubcontractor", "IsActive", 
            "CreatedAt", "UpdatedAt"
        ) VALUES (
            gen_random_uuid(), v_company_id, v_department_id, 
            'RAVEEN NAIR A/L K RAHMAN', '+60 11-1081 8049', NULL,
            'Senior', 'Subcontractor', true, true,
            NOW(), NOW()
        );
    END IF;

    -- SARAVANAN A/L I. CHINNIAH
    UPDATE "ServiceInstallers"
    SET 
        "Phone" = '+60 16-392 3026',
        "Email" = NULL,
        "SiLevel" = 'Senior',
        "InstallerType" = 'Subcontractor',
        "IsSubcontractor" = true,
        "IsActive" = true,
        "UpdatedAt" = NOW()
    WHERE "Name" = 'SARAVANAN A/L I. CHINNIAH' AND "DepartmentId" = v_department_id;
    
    GET DIAGNOSTICS v_updated_count = ROW_COUNT;
    IF v_updated_count = 0 THEN
        INSERT INTO "ServiceInstallers" (
            "Id", "CompanyId", "DepartmentId", "Name", "Phone", "Email", 
            "SiLevel", "InstallerType", "IsSubcontractor", "IsActive", 
            "CreatedAt", "UpdatedAt"
        ) VALUES (
            gen_random_uuid(), v_company_id, v_department_id, 
            'SARAVANAN A/L I. CHINNIAH', '+60 16-392 3026', NULL,
            'Senior', 'Subcontractor', true, true,
            NOW(), NOW()
        );
    END IF;

    -- SASIKUMAR A/L SEENIE
    UPDATE "ServiceInstallers"
    SET 
        "Phone" = '+60 17-677 4982',
        "Email" = NULL,
        "SiLevel" = 'Senior',
        "InstallerType" = 'Subcontractor',
        "IsSubcontractor" = true,
        "IsActive" = true,
        "UpdatedAt" = NOW()
    WHERE "Name" = 'SASIKUMAR A/L SEENIE' AND "DepartmentId" = v_department_id;
    
    GET DIAGNOSTICS v_updated_count = ROW_COUNT;
    IF v_updated_count = 0 THEN
        INSERT INTO "ServiceInstallers" (
            "Id", "CompanyId", "DepartmentId", "Name", "Phone", "Email", 
            "SiLevel", "InstallerType", "IsSubcontractor", "IsActive", 
            "CreatedAt", "UpdatedAt"
        ) VALUES (
            gen_random_uuid(), v_company_id, v_department_id, 
            'SASIKUMAR A/L SEENIE', '+60 17-677 4982', NULL,
            'Senior', 'Subcontractor', true, true,
            NOW(), NOW()
        );
    END IF;

    -- SATHISVARAN A/L S P GURUNATHAN
    UPDATE "ServiceInstallers"
    SET 
        "Phone" = '+60 10-273 6386',
        "Email" = NULL,
        "SiLevel" = 'Senior',
        "InstallerType" = 'Subcontractor',
        "IsSubcontractor" = true,
        "IsActive" = true,
        "UpdatedAt" = NOW()
    WHERE "Name" = 'SATHISVARAN A/L S P GURUNATHAN' AND "DepartmentId" = v_department_id;
    
    GET DIAGNOSTICS v_updated_count = ROW_COUNT;
    IF v_updated_count = 0 THEN
        INSERT INTO "ServiceInstallers" (
            "Id", "CompanyId", "DepartmentId", "Name", "Phone", "Email", 
            "SiLevel", "InstallerType", "IsSubcontractor", "IsActive", 
            "CreatedAt", "UpdatedAt"
        ) VALUES (
            gen_random_uuid(), v_company_id, v_department_id, 
            'SATHISVARAN A/L S P GURUNATHAN', '+60 10-273 6386', NULL,
            'Senior', 'Subcontractor', true, true,
            NOW(), NOW()
        );
    END IF;

    -- SHAMALAN A/L JOSEPH
    UPDATE "ServiceInstallers"
    SET 
        "Phone" = '+60 11-2840 3172',
        "Email" = NULL,
        "SiLevel" = 'Senior',
        "InstallerType" = 'InHouse',
        "IsSubcontractor" = false,
        "IsActive" = true,
        "UpdatedAt" = NOW()
    WHERE "Name" = 'SHAMALAN A/L JOSEPH' AND "DepartmentId" = v_department_id;
    
    GET DIAGNOSTICS v_updated_count = ROW_COUNT;
    IF v_updated_count = 0 THEN
        INSERT INTO "ServiceInstallers" (
            "Id", "CompanyId", "DepartmentId", "Name", "Phone", "Email", 
            "SiLevel", "InstallerType", "IsSubcontractor", "IsActive", 
            "CreatedAt", "UpdatedAt"
        ) VALUES (
            gen_random_uuid(), v_company_id, v_department_id, 
            'SHAMALAN A/L JOSEPH', '+60 11-2840 3172', NULL,
            'Senior', 'InHouse', false, true,
            NOW(), NOW()
        );
    END IF;

    -- SIVA A/L THANGIAH
    UPDATE "ServiceInstallers"
    SET 
        "Phone" = '+60 16-742 3600',
        "Email" = NULL,
        "SiLevel" = 'Senior',
        "InstallerType" = 'InHouse',
        "IsSubcontractor" = false,
        "IsActive" = false,
        "UpdatedAt" = NOW()
    WHERE "Name" = 'SIVA A/L THANGIAH' AND "DepartmentId" = v_department_id;
    
    GET DIAGNOSTICS v_updated_count = ROW_COUNT;
    IF v_updated_count = 0 THEN
        INSERT INTO "ServiceInstallers" (
            "Id", "CompanyId", "DepartmentId", "Name", "Phone", "Email", 
            "SiLevel", "InstallerType", "IsSubcontractor", "IsActive", 
            "CreatedAt", "UpdatedAt"
        ) VALUES (
            gen_random_uuid(), v_company_id, v_department_id, 
            'SIVA A/L THANGIAH', '+60 16-742 3600', NULL,
            'Senior', 'InHouse', false, false,
            NOW(), NOW()
        );
    END IF;

    -- SIVANESVARAAN A/L S YANESAGAR
    UPDATE "ServiceInstallers"
    SET 
        "Phone" = '+60 12-331 5104',
        "Email" = NULL,
        "SiLevel" = 'Senior',
        "InstallerType" = 'Subcontractor',
        "IsSubcontractor" = true,
        "IsActive" = true,
        "UpdatedAt" = NOW()
    WHERE "Name" = 'SIVANESVARAAN A/L S YANESAGAR' AND "DepartmentId" = v_department_id;
    
    GET DIAGNOSTICS v_updated_count = ROW_COUNT;
    IF v_updated_count = 0 THEN
        INSERT INTO "ServiceInstallers" (
            "Id", "CompanyId", "DepartmentId", "Name", "Phone", "Email", 
            "SiLevel", "InstallerType", "IsSubcontractor", "IsActive", 
            "CreatedAt", "UpdatedAt"
        ) VALUES (
            gen_random_uuid(), v_company_id, v_department_id, 
            'SIVANESVARAAN A/L S YANESAGAR', '+60 12-331 5104', NULL,
            'Senior', 'Subcontractor', true, true,
            NOW(), NOW()
        );
    END IF;

    -- SYLVESTER ELGIVA A/L SIMON
    UPDATE "ServiceInstallers"
    SET 
        "Phone" = '+60 17-233 9040',
        "Email" = NULL,
        "SiLevel" = 'Senior',
        "InstallerType" = 'InHouse',
        "IsSubcontractor" = false,
        "IsActive" = true,
        "UpdatedAt" = NOW()
    WHERE "Name" = 'SYLVESTER ELGIVA A/L SIMON' AND "DepartmentId" = v_department_id;
    
    GET DIAGNOSTICS v_updated_count = ROW_COUNT;
    IF v_updated_count = 0 THEN
        INSERT INTO "ServiceInstallers" (
            "Id", "CompanyId", "DepartmentId", "Name", "Phone", "Email", 
            "SiLevel", "InstallerType", "IsSubcontractor", "IsActive", 
            "CreatedAt", "UpdatedAt"
        ) VALUES (
            gen_random_uuid(), v_company_id, v_department_id, 
            'SYLVESTER ELGIVA A/L SIMON', '+60 17-233 9040', NULL,
            'Senior', 'InHouse', false, true,
            NOW(), NOW()
        );
    END IF;

    -- Test Service Installer
    UPDATE "ServiceInstallers"
    SET 
        "Phone" = '+60123456789',
        "Email" = 'si.test@cephas.com.my',
        "SiLevel" = 'Senior',
        "InstallerType" = 'InHouse',
        "IsSubcontractor" = false,
        "IsActive" = true,
        "UpdatedAt" = NOW()
    WHERE "Name" = 'Test Service Installer' AND "DepartmentId" = v_department_id;
    
    GET DIAGNOSTICS v_updated_count = ROW_COUNT;
    IF v_updated_count = 0 THEN
        INSERT INTO "ServiceInstallers" (
            "Id", "CompanyId", "DepartmentId", "Name", "Phone", "Email", 
            "SiLevel", "InstallerType", "IsSubcontractor", "IsActive", 
            "CreatedAt", "UpdatedAt"
        ) VALUES (
            gen_random_uuid(), v_company_id, v_department_id, 
            'Test Service Installer', '+60123456789', 'si.test@cephas.com.my',
            'Senior', 'InHouse', false, true,
            NOW(), NOW()
        );
    END IF;

    -- YELLESHUA JEEVAN A/L AROKKIASAMY
    UPDATE "ServiceInstallers"
    SET 
        "Phone" = '+60 16-453 2305',
        "Email" = NULL,
        "SiLevel" = 'Senior',
        "InstallerType" = 'Subcontractor',
        "IsSubcontractor" = true,
        "IsActive" = true,
        "UpdatedAt" = NOW()
    WHERE "Name" = 'YELLESHUA JEEVAN A/L AROKKIASAMY' AND "DepartmentId" = v_department_id;
    
    GET DIAGNOSTICS v_updated_count = ROW_COUNT;
    IF v_updated_count = 0 THEN
        INSERT INTO "ServiceInstallers" (
            "Id", "CompanyId", "DepartmentId", "Name", "Phone", "Email", 
            "SiLevel", "InstallerType", "IsSubcontractor", "IsActive", 
            "CreatedAt", "UpdatedAt"
        ) VALUES (
            gen_random_uuid(), v_company_id, v_department_id, 
            'YELLESHUA JEEVAN A/L AROKKIASAMY', '+60 16-453 2305', NULL,
            'Senior', 'Subcontractor', true, true,
            NOW(), NOW()
        );
    END IF;

    RAISE NOTICE 'Service Installers imported/updated successfully!';
END $$;

-- Verification Query
SELECT 
    "Name",
    "Phone",
    "Email",
    "SiLevel",
    "InstallerType",
    "IsSubcontractor",
    "IsActive",
    "CreatedAt",
    "UpdatedAt"
FROM "ServiceInstallers"
WHERE "DepartmentId" = (SELECT "Id" FROM "Departments" WHERE "Name" = 'GPON' LIMIT 1)
ORDER BY "Name";

