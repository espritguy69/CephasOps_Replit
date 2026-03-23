-- ============================================
-- CURRENT SERVICE INSTALLERS DATABASE STATE
-- Run this to see what's currently in your database
-- ============================================

-- Query 1: Count Summary
SELECT 
    '=== SERVICE INSTALLERS SUMMARY ===' as info;

SELECT 
    COUNT(*) as total_count,
    COUNT(*) FILTER (WHERE "IsActive" = true) as active_count,
    COUNT(*) FILTER (WHERE "IsActive" = false) as inactive_count,
    COUNT(*) FILTER (WHERE "IsSubcontractor" = true) as subcontractor_count,
    COUNT(*) FILTER (WHERE "IsSubcontractor" = false) as employee_count
FROM "ServiceInstallers";

-- Query 2: Find Current Duplicates (Name + EmployeeId)
SELECT 
    '=== DUPLICATES (Name + EmployeeId) ===' as info;

SELECT 
    si."Name",
    si."EmployeeId",
    COUNT(*) as duplicate_count,
    STRING_AGG(si."Id"::text, ', ' ORDER BY si."CreatedAt") as installer_ids,
    STRING_AGG(
        CASE WHEN si."IsSubcontractor" THEN 'Subcontractor' ELSE 'Employee' END, 
        ', ' ORDER BY si."CreatedAt"
    ) as types
FROM "ServiceInstallers" si
WHERE si."Name" IS NOT NULL AND si."EmployeeId" IS NOT NULL
GROUP BY si."Name", si."EmployeeId"
HAVING COUNT(*) > 1
ORDER BY COUNT(*) DESC, si."Name";

-- Query 3: Show ALL Active Service Installers
SELECT 
    '=== ALL ACTIVE SERVICE INSTALLERS ===' as info;

SELECT 
    ROW_NUMBER() OVER (ORDER BY si."Name") as "#",
    si."Id",
    si."Name",
    si."EmployeeId",
    CASE WHEN si."IsSubcontractor" THEN 'Subcontractor' ELSE 'Employee' END as "Type",
    si."SiLevel" as "Level",
    si."Phone",
    si."Email",
    d."Name" as "Department",
    si."CreatedAt",
    si."UpdatedAt"
FROM "ServiceInstallers" si
LEFT JOIN "Departments" d ON si."DepartmentId" = d."Id"
WHERE si."IsActive" = true
ORDER BY si."Name";

-- Query 4: Show Details of Duplicate Installers Only
SELECT 
    '=== DUPLICATE INSTALLER DETAILS ===' as info;

SELECT 
    si."Id",
    si."Name",
    si."EmployeeId",
    CASE WHEN si."IsSubcontractor" THEN 'Subcontractor' ELSE 'Employee' END as "Type",
    si."SiLevel",
    si."Phone",
    si."Email",
    d."Name" as "DepartmentName",
    si."IsActive",
    si."CreatedAt",
    si."UpdatedAt"
FROM "ServiceInstallers" si
LEFT JOIN "Departments" d ON si."DepartmentId" = d."Id"
WHERE (si."Name", si."EmployeeId") IN (
    SELECT "Name", "EmployeeId"
    FROM "ServiceInstallers"
    WHERE "Name" IS NOT NULL AND "EmployeeId" IS NOT NULL
    GROUP BY "Name", "EmployeeId"
    HAVING COUNT(*) > 1
)
ORDER BY si."Name", si."EmployeeId", si."CreatedAt";

-- Query 5: Check if Migration has been applied
SELECT 
    '=== MIGRATION STATUS ===' as info;

SELECT 
    indexname,
    indexdef
FROM pg_indexes
WHERE tablename = 'ServiceInstallers'
  AND (indexname LIKE '%Name%EmployeeId%' OR indexname LIKE '%ServiceInstallers%')
ORDER BY indexname;

-- Query 6: Check Orders assigned to Service Installers
SELECT 
    '=== ORDERS ASSIGNED TO SIs (Sample) ===' as info;

SELECT 
    o."Id" as order_id,
    o."OrderNumber",
    o."Status",
    si."Name" as installer_name,
    si."EmployeeId" as installer_employee_id,
    CASE WHEN si."IsSubcontractor" THEN 'Subcontractor' ELSE 'Employee' END as installer_type
FROM "Orders" o
INNER JOIN "ServiceInstallers" si ON o."AssignedSiId" = si."Id"
WHERE o."AssignedSiId" IS NOT NULL
ORDER BY si."Name"
LIMIT 20;

-- Query 7: Show installers by Department
SELECT 
    '=== SERVICE INSTALLERS BY DEPARTMENT ===' as info;

SELECT 
    COALESCE(d."Name", 'No Department') as "Department",
    COUNT(*) as "Count",
    COUNT(*) FILTER (WHERE si."IsSubcontractor" = false) as "Employees",
    COUNT(*) FILTER (WHERE si."IsSubcontractor" = true) as "Subcontractors"
FROM "ServiceInstallers" si
LEFT JOIN "Departments" d ON si."DepartmentId" = d."Id"
WHERE si."IsActive" = true
GROUP BY d."Name"
ORDER BY COUNT(*) DESC;

