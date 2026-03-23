-- SQL Script to create a test Service Installer account for testing the SI App UI
-- Run this script in your PostgreSQL database

-- Test SI Account Details
DO $$
DECLARE
    v_user_id UUID;
    v_si_id UUID;
    v_installer_role_id UUID;
    v_department_id UUID;
    v_test_si_email TEXT := 'si.test@cephas.com.my';
    v_test_si_password TEXT := 'TestSI123!';
    v_test_si_name TEXT := 'Test Service Installer';
    v_test_si_phone TEXT := '+60123456789';
    v_test_si_employee_id TEXT := 'SI-TEST-001';
    v_test_si_level TEXT := 'Senior';
    v_password_hash TEXT;
BEGIN
    -- Calculate password hash (SHA256 with salt "CephasOps_Salt_2024")
    -- In PostgreSQL, we'll use pgcrypto extension
    -- Hash = SHA256(password + salt) encoded as Base64
    -- For simplicity, we'll use a pre-calculated hash
    -- You can calculate this using: SHA256("TestSI123!" + "CephasOps_Salt_2024") -> Base64
    -- Or use the PowerShell script which calculates it correctly
    v_password_hash := encode(digest(v_test_si_password || 'CephasOps_Salt_2024', 'sha256'), 'base64');
    
    RAISE NOTICE 'Password hash: %', v_password_hash;
    
    -- Get or create Installer role
    SELECT "Id" INTO v_installer_role_id
    FROM "Roles"
    WHERE "Name" = 'Installer' AND "Scope" = 'Global'
    LIMIT 1;
    
    IF v_installer_role_id IS NULL THEN
        v_installer_role_id := gen_random_uuid();
        INSERT INTO "Roles" ("Id", "Name", "Scope")
        VALUES (v_installer_role_id, 'Installer', 'Global');
        RAISE NOTICE 'Created Installer role: %', v_installer_role_id;
    END IF;
    
    -- Get first active department (or create a default one)
    SELECT "Id" INTO v_department_id
    FROM "Departments"
    WHERE "IsActive" = true
    LIMIT 1;
    
    IF v_department_id IS NULL THEN
        v_department_id := gen_random_uuid();
        INSERT INTO "Departments" ("Id", "CompanyId", "Name", "Code", "IsActive", "CreatedAt")
        VALUES (v_department_id, NULL, 'GPON', 'GPON', true, CURRENT_TIMESTAMP);
        RAISE NOTICE 'Created default department: %', v_department_id;
    END IF;
    
    -- Check if user exists
    SELECT "Id" INTO v_user_id
    FROM "Users"
    WHERE "Email" = v_test_si_email;
    
    IF v_user_id IS NULL THEN
        -- Create new user
        v_user_id := gen_random_uuid();
        INSERT INTO "Users" ("Id", "Name", "Email", "Phone", "PasswordHash", "IsActive", "CreatedAt")
        VALUES (
            v_user_id,
            v_test_si_name,
            v_test_si_email,
            v_test_si_phone,
            v_password_hash,
            true,
            CURRENT_TIMESTAMP
        );
        RAISE NOTICE 'Created user: %', v_user_id;
        
        -- Assign Installer role
        INSERT INTO "UserRoles" ("Id", "UserId", "CompanyId", "RoleId", "CreatedAt")
        VALUES (gen_random_uuid(), v_user_id, NULL, v_installer_role_id, CURRENT_TIMESTAMP)
        ON CONFLICT DO NOTHING;
        RAISE NOTICE 'Assigned Installer role to user';
    ELSE
        -- Update existing user
        UPDATE "Users"
        SET 
            "Name" = v_test_si_name,
            "Phone" = v_test_si_phone,
            "PasswordHash" = v_password_hash,
            "IsActive" = true,
            "UpdatedAt" = CURRENT_TIMESTAMP
        WHERE "Id" = v_user_id;
        RAISE NOTICE 'Updated existing user: %', v_user_id;
        
        -- Ensure Installer role is assigned
        INSERT INTO "UserRoles" ("Id", "UserId", "CompanyId", "RoleId", "CreatedAt")
        VALUES (gen_random_uuid(), v_user_id, NULL, v_installer_role_id, CURRENT_TIMESTAMP)
        ON CONFLICT DO NOTHING;
    END IF;
    
    -- Check if service installer profile exists
    SELECT "Id" INTO v_si_id
    FROM "ServiceInstallers"
    WHERE "UserId" = v_user_id;
    
    IF v_si_id IS NULL THEN
        -- Create service installer profile
        v_si_id := gen_random_uuid();
        INSERT INTO "ServiceInstallers" (
            "Id", "CompanyId", "DepartmentId", "UserId", "Name", 
            "EmployeeId", "Phone", "Email", "SiLevel", 
            "IsSubcontractor", "IsActive", "CreatedAt", "UpdatedAt"
        )
        VALUES (
            v_si_id,
            NULL,
            v_department_id,
            v_user_id,
            v_test_si_name,
            v_test_si_employee_id,
            v_test_si_phone,
            v_test_si_email,
            v_test_si_level,
            true,
            true,
            CURRENT_TIMESTAMP,
            CURRENT_TIMESTAMP
        );
        RAISE NOTICE 'Created service installer profile: %', v_si_id;
    ELSE
        -- Update existing service installer profile
        UPDATE "ServiceInstallers"
        SET 
            "Name" = v_test_si_name,
            "EmployeeId" = v_test_si_employee_id,
            "Phone" = v_test_si_phone,
            "Email" = v_test_si_email,
            "SiLevel" = v_test_si_level,
            "IsSubcontractor" = true,
            "IsActive" = true,
            "UpdatedAt" = CURRENT_TIMESTAMP
        WHERE "Id" = v_si_id;
        RAISE NOTICE 'Updated existing service installer profile: %', v_si_id;
    END IF;
    
    RAISE NOTICE '';
    RAISE NOTICE '========================================';
    RAISE NOTICE '✅ Test SI account created/updated successfully!';
    RAISE NOTICE '========================================';
    RAISE NOTICE 'Email: %', v_test_si_email;
    RAISE NOTICE 'Password: %', v_test_si_password;
    RAISE NOTICE 'User ID: %', v_user_id;
    RAISE NOTICE 'Service Installer ID: %', v_si_id;
    RAISE NOTICE '========================================';
END $$;

