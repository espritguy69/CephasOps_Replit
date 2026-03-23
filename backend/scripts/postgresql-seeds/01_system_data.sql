-- ============================================
-- System Data Seed Script
-- ============================================
-- Seeds: Companies, Roles, Users, UserRoles, DepartmentMemberships
-- Dependencies: None (foundation data)
-- ============================================

-- Enable pgcrypto extension for password hashing (if not already enabled)
CREATE EXTENSION IF NOT EXISTS pgcrypto;

-- ============================================
-- 1. Companies
-- ============================================
DO $$
DECLARE
    v_company_id UUID;
BEGIN
    -- Check if company already exists
    SELECT "Id" INTO v_company_id 
    FROM "Companies" 
    WHERE "ShortName" = 'Cephas' 
    LIMIT 1;
    
    IF v_company_id IS NULL THEN
        -- Create default company
        v_company_id := gen_random_uuid();
        
        INSERT INTO "Companies" (
            "Id", "LegalName", "ShortName", "Vertical", "IsActive", "CreatedAt"
        ) VALUES (
            v_company_id,
            'Cephas',
            'Cephas',
            'General',
            true,
            NOW()
        );
        
        RAISE NOTICE 'Created default company: Cephas (ID: %)', v_company_id;
    ELSE
        RAISE NOTICE 'Default company already exists: Cephas (ID: %)', v_company_id;
    END IF;
END $$;

-- ============================================
-- 2. Roles
-- ============================================
DO $$
DECLARE
    v_superadmin_role_id UUID;
    v_director_role_id UUID;
    v_hod_role_id UUID;
    v_supervisor_role_id UUID;
    v_finance_role_id UUID;
BEGIN
    -- SuperAdmin Role
    SELECT "Id" INTO v_superadmin_role_id 
    FROM "Roles" 
    WHERE "Name" = 'SuperAdmin' AND "Scope" = 'Global' 
    LIMIT 1;
    
    IF v_superadmin_role_id IS NULL THEN
        v_superadmin_role_id := gen_random_uuid();
        INSERT INTO "Roles" ("Id", "Name", "Scope") 
        VALUES (v_superadmin_role_id, 'SuperAdmin', 'Global')
        ON CONFLICT DO NOTHING;
        RAISE NOTICE 'Created SuperAdmin role';
    END IF;
    
    -- Director Role
    SELECT "Id" INTO v_director_role_id 
    FROM "Roles" 
    WHERE "Name" = 'Director' AND "Scope" = 'Global' 
    LIMIT 1;
    
    IF v_director_role_id IS NULL THEN
        v_director_role_id := gen_random_uuid();
        INSERT INTO "Roles" ("Id", "Name", "Scope") 
        VALUES (v_director_role_id, 'Director', 'Global')
        ON CONFLICT DO NOTHING;
        RAISE NOTICE 'Created Director role';
    END IF;
    
    -- HeadOfDepartment Role
    SELECT "Id" INTO v_hod_role_id 
    FROM "Roles" 
    WHERE "Name" = 'HeadOfDepartment' AND "Scope" = 'Global' 
    LIMIT 1;
    
    IF v_hod_role_id IS NULL THEN
        v_hod_role_id := gen_random_uuid();
        INSERT INTO "Roles" ("Id", "Name", "Scope") 
        VALUES (v_hod_role_id, 'HeadOfDepartment', 'Global')
        ON CONFLICT DO NOTHING;
        RAISE NOTICE 'Created HeadOfDepartment role';
    END IF;
    
    -- Supervisor Role
    SELECT "Id" INTO v_supervisor_role_id 
    FROM "Roles" 
    WHERE "Name" = 'Supervisor' AND "Scope" = 'Global' 
    LIMIT 1;
    
    IF v_supervisor_role_id IS NULL THEN
        v_supervisor_role_id := gen_random_uuid();
        INSERT INTO "Roles" ("Id", "Name", "Scope") 
        VALUES (v_supervisor_role_id, 'Supervisor', 'Global')
        ON CONFLICT DO NOTHING;
        RAISE NOTICE 'Created Supervisor role';
    END IF;
    
    -- FinanceManager Role (for Finance HOD)
    SELECT "Id" INTO v_finance_role_id 
    FROM "Roles" 
    WHERE "Name" = 'FinanceManager' AND "Scope" = 'Global' 
    LIMIT 1;
    
    IF v_finance_role_id IS NULL THEN
        v_finance_role_id := gen_random_uuid();
        INSERT INTO "Roles" ("Id", "Name", "Scope") 
        VALUES (v_finance_role_id, 'FinanceManager', 'Global')
        ON CONFLICT DO NOTHING;
        RAISE NOTICE 'Created FinanceManager role';
    END IF;
END $$;

-- ============================================
-- 3. Users
-- ============================================
DO $$
DECLARE
    v_admin_user_id UUID;
    v_finance_user_id UUID;
    v_superadmin_role_id UUID;
    v_finance_role_id UUID;
    v_gpon_department_id UUID;
    v_company_id UUID;
    -- Password hashes (SHA256 + salt "CephasOps_Salt_2024", Base64 encoded)
    v_admin_password_hash TEXT := 'DPoZR4yEm+hNKLt05409XYJPWGJC0KisAMQHVIOHp2Q=';
    v_finance_password_hash TEXT := 'M3YObIZ4+LOYNmkCSEIK8+kr64rQmW7x28HBNr3ZfoE=';
BEGIN
    -- Get company ID
    SELECT "Id" INTO v_company_id FROM "Companies" WHERE "ShortName" = 'Cephas' LIMIT 1;
    
    -- Get role IDs
    SELECT "Id" INTO v_superadmin_role_id FROM "Roles" WHERE "Name" = 'SuperAdmin' AND "Scope" = 'Global' LIMIT 1;
    SELECT "Id" INTO v_finance_role_id FROM "Roles" WHERE "Name" = 'FinanceManager' AND "Scope" = 'Global' LIMIT 1;
    
    -- Get GPON department ID (may not exist yet, will be created in 03_master_data.sql)
    SELECT "Id" INTO v_gpon_department_id FROM "Departments" WHERE "Code" = 'GPON' LIMIT 1;
    
    -- Admin User (simon@cephas.com.my)
    SELECT "Id" INTO v_admin_user_id 
    FROM "Users" 
    WHERE "Email" = 'simon@cephas.com.my' 
    LIMIT 1;
    
    IF v_admin_user_id IS NULL THEN
        v_admin_user_id := gen_random_uuid();
        INSERT INTO "Users" (
            "Id", "Name", "Email", "PasswordHash", "IsActive", "CreatedAt"
        ) VALUES (
            v_admin_user_id,
            'Simon',
            'simon@cephas.com.my',
            v_admin_password_hash,
            true,
            NOW()
        );
        RAISE NOTICE 'Created admin user: simon@cephas.com.my';
    ELSE
        -- Update password hash if it doesn't match
        UPDATE "Users" 
        SET "PasswordHash" = v_admin_password_hash, "IsActive" = true
        WHERE "Id" = v_admin_user_id AND "PasswordHash" != v_admin_password_hash;
        RAISE NOTICE 'Admin user already exists: simon@cephas.com.my';
    END IF;
    
    -- Assign SuperAdmin role to admin user
    IF NOT EXISTS (
        SELECT 1 FROM "UserRoles" 
        WHERE "UserId" = v_admin_user_id AND "RoleId" = v_superadmin_role_id
    ) THEN
        INSERT INTO "UserRoles" ("Id", "UserId", "CompanyId", "RoleId", "CreatedAt")
        VALUES (gen_random_uuid(), v_admin_user_id, NULL, v_superadmin_role_id, NOW())
        ON CONFLICT DO NOTHING;
        RAISE NOTICE 'Assigned SuperAdmin role to admin user';
    END IF;
    
    -- Finance HOD User (finance@cephas.com.my)
    SELECT "Id" INTO v_finance_user_id 
    FROM "Users" 
    WHERE "Email" = 'finance@cephas.com.my' 
    LIMIT 1;
    
    IF v_finance_user_id IS NULL THEN
        v_finance_user_id := gen_random_uuid();
        INSERT INTO "Users" (
            "Id", "Name", "Email", "PasswordHash", "IsActive", "CreatedAt"
        ) VALUES (
            v_finance_user_id,
            'Samyu Kavitha',
            'finance@cephas.com.my',
            v_finance_password_hash,
            true,
            NOW()
        );
        RAISE NOTICE 'Created Finance HOD user: finance@cephas.com.my';
    ELSE
        RAISE NOTICE 'Finance HOD user already exists: finance@cephas.com.my';
    END IF;
    
    -- Assign FinanceManager role to Finance HOD user
    IF v_finance_role_id IS NOT NULL AND NOT EXISTS (
        SELECT 1 FROM "UserRoles" 
        WHERE "UserId" = v_finance_user_id AND "RoleId" = v_finance_role_id
    ) THEN
        INSERT INTO "UserRoles" ("Id", "UserId", "CompanyId", "RoleId", "CreatedAt")
        VALUES (gen_random_uuid(), v_finance_user_id, v_company_id, v_finance_role_id, NOW())
        ON CONFLICT DO NOTHING;
        RAISE NOTICE 'Assigned FinanceManager role to Finance HOD user';
    END IF;
    
    -- Link Finance HOD to GPON department (if department exists)
    IF v_gpon_department_id IS NOT NULL AND NOT EXISTS (
        SELECT 1 FROM "DepartmentMemberships" 
        WHERE "UserId" = v_finance_user_id AND "DepartmentId" = v_gpon_department_id
    ) THEN
        INSERT INTO "DepartmentMemberships" (
            "Id", "CompanyId", "DepartmentId", "UserId", "Role", "IsDefault", "CreatedAt", "UpdatedAt"
        ) VALUES (
            gen_random_uuid(),
            v_company_id,
            v_gpon_department_id,
            v_finance_user_id,
            'HOD',
            true,
            NOW(),
            NOW()
        )
        ON CONFLICT DO NOTHING;
        RAISE NOTICE 'Linked Finance HOD to GPON department';
    END IF;
END $$;

-- ============================================
-- Verification
-- ============================================
DO $$
DECLARE
    v_companies_count INT;
    v_roles_count INT;
    v_users_count INT;
    v_user_roles_count INT;
BEGIN
    SELECT COUNT(*) INTO v_companies_count FROM "Companies";
    SELECT COUNT(*) INTO v_roles_count FROM "Roles";
    SELECT COUNT(*) INTO v_users_count FROM "Users";
    SELECT COUNT(*) INTO v_user_roles_count FROM "UserRoles";
    
    RAISE NOTICE '========================================';
    RAISE NOTICE 'System Data Seeding Complete';
    RAISE NOTICE '========================================';
    RAISE NOTICE 'Companies: %', v_companies_count;
    RAISE NOTICE 'Roles: %', v_roles_count;
    RAISE NOTICE 'Users: %', v_users_count;
    RAISE NOTICE 'UserRoles: %', v_user_roles_count;
    RAISE NOTICE '========================================';
END $$;

