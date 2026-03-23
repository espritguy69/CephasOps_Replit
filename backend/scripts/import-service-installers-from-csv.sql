-- Import Service Installers from CSV
-- This script imports data from service-installers-template.csv
-- Run this AFTER applying the InstallerType migration

-- Step 1: Get the GPON Department ID and Company ID
DO $$
DECLARE
    v_department_id UUID;
    v_company_id UUID;
    v_installer_id UUID;
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

    -- Step 2: Clear existing Service Installers (optional - comment out if you want to keep existing)
    -- DELETE FROM "ServiceInstallers" WHERE "DepartmentId" = v_department_id;

    -- Step 3: Insert Service Installers
    -- CHANDRASEKARAN VEERIAH
    v_installer_id := gen_random_uuid();
    INSERT INTO "ServiceInstallers" (
        "Id", "CompanyId", "DepartmentId", "Name", "Phone", "Email", 
        "SiLevel", "InstallerType", "IsSubcontractor", "IsActive", 
        "CreatedAt", "UpdatedAt"
    ) VALUES (
        v_installer_id, v_company_id, v_department_id, 
        'CHANDRASEKARAN VEERIAH', '+60 16-491 1325', NULL,
        'Senior', 'InHouse', false, true,
        NOW(), NOW()
    ) ON CONFLICT DO NOTHING;

    -- EDWIN DASS A/L YESU DAS
    v_installer_id := gen_random_uuid();
    INSERT INTO "ServiceInstallers" (
        "Id", "CompanyId", "DepartmentId", "Name", "Phone", "Email", 
        "SiLevel", "InstallerType", "IsSubcontractor", "IsActive", 
        "CreatedAt", "UpdatedAt"
    ) VALUES (
        v_installer_id, v_company_id, v_department_id, 
        'EDWIN DASS A/L YESU DAS', '+60 11-1081 9064', NULL,
        'Junior', 'Subcontractor', true, true,
        NOW(), NOW()
    ) ON CONFLICT DO NOTHING;

    -- ISHAAN
    v_installer_id := gen_random_uuid();
    INSERT INTO "ServiceInstallers" (
        "Id", "CompanyId", "DepartmentId", "Name", "Phone", "Email", 
        "SiLevel", "InstallerType", "IsSubcontractor", "IsActive", 
        "CreatedAt", "UpdatedAt"
    ) VALUES (
        v_installer_id, v_company_id, v_department_id, 
        'ISHAAN', '+60125156965', NULL,
        'Junior', 'InHouse', false, true,
        NOW(), NOW()
    ) ON CONFLICT DO NOTHING;

    -- K. MARIAPPAN A/L KUPPATHAN
    v_installer_id := gen_random_uuid();
    INSERT INTO "ServiceInstallers" (
        "Id", "CompanyId", "DepartmentId", "Name", "Phone", "Email", 
        "SiLevel", "InstallerType", "IsSubcontractor", "IsActive", 
        "CreatedAt", "UpdatedAt"
    ) VALUES (
        v_installer_id, v_company_id, v_department_id, 
        'K. MARIAPPAN A/L KUPPATHAN', '+60 17-676 7625', NULL,
        'Senior', 'Subcontractor', true, true,
        NOW(), NOW()
    ) ON CONFLICT DO NOTHING;

    -- KLAVINN RAJ A/L AROKKIASAMY
    v_installer_id := gen_random_uuid();
    INSERT INTO "ServiceInstallers" (
        "Id", "CompanyId", "DepartmentId", "Name", "Phone", "Email", 
        "SiLevel", "InstallerType", "IsSubcontractor", "IsActive", 
        "CreatedAt", "UpdatedAt"
    ) VALUES (
        v_installer_id, v_company_id, v_department_id, 
        'KLAVINN RAJ A/L AROKKIASAMY', '+60 12-291 6386', NULL,
        'Senior', 'Subcontractor', true, true,
        NOW(), NOW()
    ) ON CONFLICT DO NOTHING;

    -- MOHAMMAD ALIYASMAAN
    v_installer_id := gen_random_uuid();
    INSERT INTO "ServiceInstallers" (
        "Id", "CompanyId", "DepartmentId", "Name", "Phone", "Email", 
        "SiLevel", "InstallerType", "IsSubcontractor", "IsActive", 
        "CreatedAt", "UpdatedAt"
    ) VALUES (
        v_installer_id, v_company_id, v_department_id, 
        'MOHAMMAD ALIYASMAAN', '+60 17-494 7242', NULL,
        'Junior', 'InHouse', false, true,
        NOW(), NOW()
    ) ON CONFLICT DO NOTHING;

    -- MOHD TAKYIN BIN CHE ALI
    v_installer_id := gen_random_uuid();
    INSERT INTO "ServiceInstallers" (
        "Id", "CompanyId", "DepartmentId", "Name", "Phone", "Email", 
        "SiLevel", "InstallerType", "IsSubcontractor", "IsActive", 
        "CreatedAt", "UpdatedAt"
    ) VALUES (
        v_installer_id, v_company_id, v_department_id, 
        'MOHD TAKYIN BIN CHE ALI', '+60 13-515 5900', NULL,
        'Senior', 'Subcontractor', true, true,
        NOW(), NOW()
    ) ON CONFLICT DO NOTHING;

    -- MUHAMAD QAIRUL HAIKAL BIN ABDULLAH
    v_installer_id := gen_random_uuid();
    INSERT INTO "ServiceInstallers" (
        "Id", "CompanyId", "DepartmentId", "Name", "Phone", "Email", 
        "SiLevel", "InstallerType", "IsSubcontractor", "IsActive", 
        "CreatedAt", "UpdatedAt"
    ) VALUES (
        v_installer_id, v_company_id, v_department_id, 
        'MUHAMAD QAIRUL HAIKAL BIN ABDULLAH', '+60 11-1690 3721', NULL,
        'Junior', 'InHouse', false, true,
        NOW(), NOW()
    ) ON CONFLICT DO NOTHING;

    -- MUHAMMAD AMMAR BIN MOHD GHAZI
    v_installer_id := gen_random_uuid();
    INSERT INTO "ServiceInstallers" (
        "Id", "CompanyId", "DepartmentId", "Name", "Phone", "Email", 
        "SiLevel", "InstallerType", "IsSubcontractor", "IsActive", 
        "CreatedAt", "UpdatedAt"
    ) VALUES (
        v_installer_id, v_company_id, v_department_id, 
        'MUHAMMAD AMMAR BIN MOHD GHAZI', '+60 11-7228 8644', NULL,
        'Junior', 'InHouse', false, true,
        NOW(), NOW()
    ) ON CONFLICT DO NOTHING;

    -- MUNIANDY A/L SOORINARAYANAN
    v_installer_id := gen_random_uuid();
    INSERT INTO "ServiceInstallers" (
        "Id", "CompanyId", "DepartmentId", "Name", "Phone", "Email", 
        "SiLevel", "InstallerType", "IsSubcontractor", "IsActive", 
        "CreatedAt", "UpdatedAt"
    ) VALUES (
        v_installer_id, v_company_id, v_department_id, 
        'MUNIANDY A/L SOORINARAYANAN', '+60 16-319 8867', NULL,
        'Senior', 'Subcontractor', true, true,
        NOW(), NOW()
    ) ON CONFLICT DO NOTHING;

    -- NORAFIZ HAFIZUL BIN ABDULLAH
    v_installer_id := gen_random_uuid();
    INSERT INTO "ServiceInstallers" (
        "Id", "CompanyId", "DepartmentId", "Name", "Phone", "Email", 
        "SiLevel", "InstallerType", "IsSubcontractor", "IsActive", 
        "CreatedAt", "UpdatedAt"
    ) VALUES (
        v_installer_id, v_company_id, v_department_id, 
        'NORAFIZ HAFIZUL BIN ABDULLAH', '+60 17-943 7241', NULL,
        'Senior', 'InHouse', false, true,
        NOW(), NOW()
    ) ON CONFLICT DO NOTHING;

    -- RAVEEN NAIR A/L K RAHMAN
    v_installer_id := gen_random_uuid();
    INSERT INTO "ServiceInstallers" (
        "Id", "CompanyId", "DepartmentId", "Name", "Phone", "Email", 
        "SiLevel", "InstallerType", "IsSubcontractor", "IsActive", 
        "CreatedAt", "UpdatedAt"
    ) VALUES (
        v_installer_id, v_company_id, v_department_id, 
        'RAVEEN NAIR A/L K RAHMAN', '+60 11-1081 8049', NULL,
        'Senior', 'Subcontractor', true, true,
        NOW(), NOW()
    ) ON CONFLICT DO NOTHING;

    -- SARAVANAN A/L I. CHINNIAH
    v_installer_id := gen_random_uuid();
    INSERT INTO "ServiceInstallers" (
        "Id", "CompanyId", "DepartmentId", "Name", "Phone", "Email", 
        "SiLevel", "InstallerType", "IsSubcontractor", "IsActive", 
        "CreatedAt", "UpdatedAt"
    ) VALUES (
        v_installer_id, v_company_id, v_department_id, 
        'SARAVANAN A/L I. CHINNIAH', '+60 16-392 3026', NULL,
        'Senior', 'Subcontractor', true, true,
        NOW(), NOW()
    ) ON CONFLICT DO NOTHING;

    -- SASIKUMAR A/L SEENIE
    v_installer_id := gen_random_uuid();
    INSERT INTO "ServiceInstallers" (
        "Id", "CompanyId", "DepartmentId", "Name", "Phone", "Email", 
        "SiLevel", "InstallerType", "IsSubcontractor", "IsActive", 
        "CreatedAt", "UpdatedAt"
    ) VALUES (
        v_installer_id, v_company_id, v_department_id, 
        'SASIKUMAR A/L SEENIE', '+60 17-677 4982', NULL,
        'Senior', 'Subcontractor', true, true,
        NOW(), NOW()
    ) ON CONFLICT DO NOTHING;

    -- SATHISVARAN A/L S P GURUNATHAN
    v_installer_id := gen_random_uuid();
    INSERT INTO "ServiceInstallers" (
        "Id", "CompanyId", "DepartmentId", "Name", "Phone", "Email", 
        "SiLevel", "InstallerType", "IsSubcontractor", "IsActive", 
        "CreatedAt", "UpdatedAt"
    ) VALUES (
        v_installer_id, v_company_id, v_department_id, 
        'SATHISVARAN A/L S P GURUNATHAN', '+60 10-273 6386', NULL,
        'Senior', 'Subcontractor', true, true,
        NOW(), NOW()
    ) ON CONFLICT DO NOTHING;

    -- SHAMALAN A/L JOSEPH
    v_installer_id := gen_random_uuid();
    INSERT INTO "ServiceInstallers" (
        "Id", "CompanyId", "DepartmentId", "Name", "Phone", "Email", 
        "SiLevel", "InstallerType", "IsSubcontractor", "IsActive", 
        "CreatedAt", "UpdatedAt"
    ) VALUES (
        v_installer_id, v_company_id, v_department_id, 
        'SHAMALAN A/L JOSEPH', '+60 11-2840 3172', NULL,
        'Senior', 'InHouse', false, true,
        NOW(), NOW()
    ) ON CONFLICT DO NOTHING;

    -- SIVA A/L THANGIAH
    v_installer_id := gen_random_uuid();
    INSERT INTO "ServiceInstallers" (
        "Id", "CompanyId", "DepartmentId", "Name", "Phone", "Email", 
        "SiLevel", "InstallerType", "IsSubcontractor", "IsActive", 
        "CreatedAt", "UpdatedAt"
    ) VALUES (
        v_installer_id, v_company_id, v_department_id, 
        'SIVA A/L THANGIAH', '+60 16-742 3600', NULL,
        'Senior', 'InHouse', false, false,
        NOW(), NOW()
    ) ON CONFLICT DO NOTHING;

    -- SIVANESVARAAN A/L S YANESAGAR
    v_installer_id := gen_random_uuid();
    INSERT INTO "ServiceInstallers" (
        "Id", "CompanyId", "DepartmentId", "Name", "Phone", "Email", 
        "SiLevel", "InstallerType", "IsSubcontractor", "IsActive", 
        "CreatedAt", "UpdatedAt"
    ) VALUES (
        v_installer_id, v_company_id, v_department_id, 
        'SIVANESVARAAN A/L S YANESAGAR', '+60 12-331 5104', NULL,
        'Senior', 'Subcontractor', true, true,
        NOW(), NOW()
    ) ON CONFLICT DO NOTHING;

    -- SYLVESTER ELGIVA A/L SIMON
    v_installer_id := gen_random_uuid();
    INSERT INTO "ServiceInstallers" (
        "Id", "CompanyId", "DepartmentId", "Name", "Phone", "Email", 
        "SiLevel", "InstallerType", "IsSubcontractor", "IsActive", 
        "CreatedAt", "UpdatedAt"
    ) VALUES (
        v_installer_id, v_company_id, v_department_id, 
        'SYLVESTER ELGIVA A/L SIMON', '+60 17-233 9040', NULL,
        'Senior', 'InHouse', false, true,
        NOW(), NOW()
    ) ON CONFLICT DO NOTHING;

    -- Test Service Installer
    v_installer_id := gen_random_uuid();
    INSERT INTO "ServiceInstallers" (
        "Id", "CompanyId", "DepartmentId", "Name", "Phone", "Email", 
        "SiLevel", "InstallerType", "IsSubcontractor", "IsActive", 
        "CreatedAt", "UpdatedAt"
    ) VALUES (
        v_installer_id, v_company_id, v_department_id, 
        'Test Service Installer', '+60123456789', 'si.test@cephas.com.my',
        'Senior', 'InHouse', false, true,
        NOW(), NOW()
    ) ON CONFLICT DO NOTHING;

    -- YELLESHUA JEEVAN A/L AROKKIASAMY
    v_installer_id := gen_random_uuid();
    INSERT INTO "ServiceInstallers" (
        "Id", "CompanyId", "DepartmentId", "Name", "Phone", "Email", 
        "SiLevel", "InstallerType", "IsSubcontractor", "IsActive", 
        "CreatedAt", "UpdatedAt"
    ) VALUES (
        v_installer_id, v_company_id, v_department_id, 
        'YELLESHUA JEEVAN A/L AROKKIASAMY', '+60 16-453 2305', NULL,
        'Senior', 'Subcontractor', true, true,
        NOW(), NOW()
    ) ON CONFLICT DO NOTHING;

    RAISE NOTICE 'Service Installers imported successfully!';
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
    "CreatedAt"
FROM "ServiceInstallers"
WHERE "DepartmentId" = (SELECT "Id" FROM "Departments" WHERE "Name" = 'GPON' LIMIT 1)
ORDER BY "Name";

