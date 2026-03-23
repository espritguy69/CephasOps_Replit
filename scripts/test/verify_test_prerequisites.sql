-- ============================================
-- Test Prerequisites Verification Script
-- Run this to verify all required test data exists
-- ============================================

-- 1. Check Departments
SELECT 
    '=== DEPARTMENTS ===' as info;

SELECT 
    COUNT(*) as "Total",
    COUNT(*) FILTER (WHERE "IsActive" = true) as "Active",
    STRING_AGG("Name", ', ') as "DepartmentNames"
FROM "Departments";

-- 2. Check Partners
SELECT 
    '=== PARTNERS ===' as info;

SELECT 
    COUNT(*) as "Total",
    COUNT(*) FILTER (WHERE "IsActive" = true) as "Active",
    STRING_AGG("Name", ', ') as "PartnerNames"
FROM "Partners";

-- 3. Check OrderTypes
SELECT 
    '=== ORDER TYPES ===' as info;

SELECT 
    COUNT(*) as "Total",
    COUNT(*) FILTER (WHERE "IsActive" = true) as "Active",
    STRING_AGG("Name", ', ') as "OrderTypeNames"
FROM "OrderTypes";

-- 4. Check Buildings
SELECT 
    '=== BUILDINGS ===' as info;

SELECT 
    COUNT(*) as "Total",
    COUNT(*) FILTER (WHERE "IsActive" = true) as "Active"
FROM "Buildings";

-- Show sample buildings
SELECT 
    "Id",
    "Name",
    "Code",
    "City",
    "State",
    "Postcode",
    "IsActive"
FROM "Buildings"
WHERE "IsActive" = true
LIMIT 5;

-- 5. Check ServiceInstallers
SELECT 
    '=== SERVICE INSTALLERS ===' as info;

SELECT 
    COUNT(*) as "Total",
    COUNT(*) FILTER (WHERE "IsActive" = true) as "Active"
FROM "ServiceInstallers";

-- Show sample installers
SELECT 
    si."Id",
    si."Name",
    si."EmployeeId",
    si."Phone",
    si."Email",
    d."Name" as "DepartmentName",
    si."IsActive"
FROM "ServiceInstallers" si
LEFT JOIN "Departments" d ON si."DepartmentId" = d."Id"
WHERE si."IsActive" = true
LIMIT 5;

-- 6. Check ParserTemplates
SELECT 
    '=== PARSER TEMPLATES ===' as info;

SELECT 
    COUNT(*) as "Total",
    COUNT(*) FILTER (WHERE "IsActive" = true) as "Active",
    STRING_AGG("Name", ', ') as "TemplateNames"
FROM "ParserTemplates";

-- Show sample templates
SELECT 
    "Id",
    "Name",
    "FromPattern",
    "SubjectPattern",
    "OrderTypeCode",
    "IsActive"
FROM "ParserTemplates"
WHERE "IsActive" = true
LIMIT 5;

-- 7. Check EmailAccounts
SELECT 
    '=== EMAIL ACCOUNTS ===' as info;

SELECT 
    "Id",
    "Name",
    "Provider",
    "Host",
    "Username",
    "IsActive",
    "PollIntervalSec",
    "LastPolledAt",
    "DefaultDepartmentId",
    "DefaultParserTemplateId"
FROM "EmailAccounts"
WHERE "IsActive" = true;

-- 8. Summary - Prerequisites Status
SELECT 
    '=== PREREQUISITES STATUS ===' as info;

SELECT 
    (SELECT COUNT(*) FROM "Departments" WHERE "IsActive" = true) >= 1 as "HasDepartment",
    (SELECT COUNT(*) FROM "Partners" WHERE "IsActive" = true) >= 1 as "HasPartner",
    (SELECT COUNT(*) FROM "OrderTypes" WHERE "IsActive" = true) >= 1 as "HasOrderType",
    (SELECT COUNT(*) FROM "Buildings" WHERE "IsActive" = true) >= 1 as "HasBuilding",
    (SELECT COUNT(*) FROM "ServiceInstallers" WHERE "IsActive" = true) >= 1 as "HasServiceInstaller",
    (SELECT COUNT(*) FROM "ParserTemplates" WHERE "IsActive" = true) >= 1 as "HasParserTemplate",
    (SELECT COUNT(*) FROM "EmailAccounts" WHERE "IsActive" = true) >= 1 as "HasEmailAccount";

