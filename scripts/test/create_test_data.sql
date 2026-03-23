-- ============================================
-- Test Data Creation Script
-- Creates minimum required test data if missing
-- Run this AFTER verify_test_prerequisites.sql
-- ============================================

-- Note: This script uses INSERT ... ON CONFLICT DO NOTHING
-- to avoid errors if data already exists

-- 1. Create Department (GPON) if missing
INSERT INTO "Departments" ("Id", "CompanyId", "Name", "Code", "Description", "IsActive", "CreatedAt", "UpdatedAt")
SELECT 
    gen_random_uuid(),
    NULL, -- Single company mode
    'GPON',
    'GPON',
    'GPON Department for testing',
    true,
    NOW(),
    NOW()
WHERE NOT EXISTS (
    SELECT 1 FROM "Departments" WHERE "Code" = 'GPON' AND "IsActive" = true
)
RETURNING "Id", "Name";

-- 2. Create Partner (TIME FTTH) if missing
INSERT INTO "Partners" ("Id", "CompanyId", "Name", "Code", "PartnerGroupId", "IsActive", "CreatedAt", "UpdatedAt")
SELECT 
    gen_random_uuid(),
    NULL, -- Single company mode
    'TIME FTTH',
    'TIMEFTTH',
    (SELECT "Id" FROM "PartnerGroups" WHERE "Code" = 'TIME' LIMIT 1),
    true,
    NOW(),
    NOW()
WHERE NOT EXISTS (
    SELECT 1 FROM "Partners" WHERE "Code" = 'TIMEFTTH' AND "IsActive" = true
)
RETURNING "Id", "Name";

-- 3. Create OrderType (Activation) if missing
INSERT INTO "OrderTypes" ("Id", "CompanyId", "Name", "Code", "Description", "IsActive", "CreatedAt", "UpdatedAt")
SELECT 
    gen_random_uuid(),
    NULL, -- Single company mode
    'Activation',
    'ACTIVATION',
    'GPON Activation Order',
    true,
    NOW(),
    NOW()
WHERE NOT EXISTS (
    SELECT 1 FROM "OrderTypes" WHERE "Code" = 'ACTIVATION' AND "IsActive" = true
)
RETURNING "Id", "Name";

-- 4. Create Building if missing
INSERT INTO "Buildings" ("Id", "CompanyId", "Name", "Code", "AddressLine1", "City", "State", "Postcode", "IsActive", "CreatedAt", "UpdatedAt")
SELECT 
    gen_random_uuid(),
    NULL, -- Single company mode
    'Test Building',
    'TEST001',
    '123 Test Street',
    'Kuala Lumpur',
    'Wilayah Persekutuan',
    '50000',
    true,
    NOW(),
    NOW()
WHERE NOT EXISTS (
    SELECT 1 FROM "Buildings" WHERE "Code" = 'TEST001' AND "IsActive" = true
)
RETURNING "Id", "Name", "Code";

-- 5. Create ServiceInstaller if missing
INSERT INTO "ServiceInstallers" (
    "Id", "CompanyId", "Name", "EmployeeId", "Phone", "Email", 
    "DepartmentId", "SiLevel", "IsSubcontractor", "IsActive", 
    "CreatedAt", "UpdatedAt"
)
SELECT 
    gen_random_uuid(),
    NULL, -- Single company mode
    'Test Installer',
    'SI001',
    '0123456789',
    'test.installer@cephas.com',
    (SELECT "Id" FROM "Departments" WHERE "Code" = 'GPON' AND "IsActive" = true LIMIT 1),
    'Level1',
    false,
    true,
    NOW(),
    NOW()
WHERE NOT EXISTS (
    SELECT 1 FROM "ServiceInstallers" WHERE "EmployeeId" = 'SI001' AND "IsActive" = true
)
RETURNING "Id", "Name", "EmployeeId";

-- 6. Create ParserTemplate if missing
INSERT INTO "ParserTemplates" (
    "Id", "CompanyId", "Name", "FromPattern", "SubjectPattern", 
    "OrderTypeCode", "PartnerId", "DepartmentId", "IsActive", 
    "CreatedAt", "UpdatedAt"
)
SELECT 
    gen_random_uuid(),
    NULL, -- Single company mode
    'TIME FTTH Activation Template',
    'noreply@time.com.my',
    'FTTH|Activation',
    'ACTIVATION',
    (SELECT "Id" FROM "Partners" WHERE "Code" = 'TIMEFTTH' AND "IsActive" = true LIMIT 1),
    (SELECT "Id" FROM "Departments" WHERE "Code" = 'GPON' AND "IsActive" = true LIMIT 1),
    true,
    NOW(),
    NOW()
WHERE NOT EXISTS (
    SELECT 1 FROM "ParserTemplates" 
    WHERE "FromPattern" = 'noreply@time.com.my' 
    AND "IsActive" = true
)
RETURNING "Id", "Name", "FromPattern";

-- 7. Summary
SELECT 
    '=== TEST DATA CREATED ===' as info;

SELECT 
    (SELECT COUNT(*) FROM "Departments" WHERE "Code" = 'GPON' AND "IsActive" = true) as "GPON_Department",
    (SELECT COUNT(*) FROM "Partners" WHERE "Code" = 'TIMEFTTH' AND "IsActive" = true) as "TIME_Partner",
    (SELECT COUNT(*) FROM "OrderTypes" WHERE "Code" = 'ACTIVATION' AND "IsActive" = true) as "Activation_OrderType",
    (SELECT COUNT(*) FROM "Buildings" WHERE "Code" = 'TEST001' AND "IsActive" = true) as "Test_Building",
    (SELECT COUNT(*) FROM "ServiceInstallers" WHERE "EmployeeId" = 'SI001' AND "IsActive" = true) as "Test_Installer",
    (SELECT COUNT(*) FROM "ParserTemplates" WHERE "FromPattern" = 'noreply@time.com.my' AND "IsActive" = true) as "TIME_Template";

