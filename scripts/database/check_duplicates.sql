-- ============================================
-- Check for duplicate service installers
-- Based on Name + EmployeeId (our duplicate logic)
-- ============================================

-- Query 1: Find duplicates by Name + EmployeeId
SELECT 
    si."Name",
    si."EmployeeId",
    CASE WHEN si."IsSubcontractor" THEN 'Subcontractor' ELSE 'Employee' END as "Type",
    si."SiLevel",
    COUNT(*) as duplicate_count,
    STRING_AGG(si."Id"::text, ', ') as installer_ids
FROM "ServiceInstallers" si
WHERE si."IsActive" = true
GROUP BY si."Name", si."EmployeeId", si."IsSubcontractor", si."SiLevel"
HAVING COUNT(*) > 1
ORDER BY COUNT(*) DESC, si."Name";

-- Query 2: Show all details of installers with duplicate Name + EmployeeId
SELECT 
    si."Id",
    si."Name",
    si."EmployeeId",
    CASE WHEN si."IsSubcontractor" THEN 'Subcontractor' ELSE 'Employee' END as "Type",
    si."SiLevel",
    si."Phone",
    si."Email",
    si."DepartmentId",
    d."Name" as DepartmentName,
    si."IsActive",
    si."CreatedAt",
    si."UpdatedAt"
FROM "ServiceInstallers" si
LEFT JOIN "Departments" d ON si."DepartmentId" = d."Id"
WHERE si."Name" IN (
    SELECT "Name"
    FROM "ServiceInstallers"
    WHERE "IsActive" = true
    GROUP BY "Name", "EmployeeId"
    HAVING COUNT(*) > 1
)
ORDER BY si."Name", si."EmployeeId", si."CreatedAt";

-- Query 3: Count total installers
SELECT 
    COUNT(*) as total_count,
    COUNT(*) FILTER (WHERE "IsActive" = true) as active_count,
    COUNT(*) FILTER (WHERE "IsActive" = false) as inactive_count,
    COUNT(*) FILTER (WHERE "IsSubcontractor" = true) as subcontractor_count,
    COUNT(*) FILTER (WHERE "IsSubcontractor" = false) as employee_count
FROM "ServiceInstallers";

-- Query 4: Check if unique index exists
SELECT 
    indexname,
    indexdef
FROM pg_indexes
WHERE tablename = 'ServiceInstallers'
  AND indexname LIKE '%Name%EmployeeId%';

-- Query 5: Check table constraints
SELECT 
    conname as constraint_name,
    contype as constraint_type,
    pg_get_constraintdef(oid) as definition
FROM pg_constraint
WHERE conrelid = '"ServiceInstallers"'::regclass;

-- Query 6: Orders that reference service installers (to verify FK relationships)
SELECT 
    o."Id" as order_id,
    o."OrderNumber",
    o."Status",
    o."AssignedSiId",
    si."Name" as installer_name,
    si."EmployeeId" as installer_employee_id
FROM "Orders" o
INNER JOIN "ServiceInstallers" si ON o."AssignedSiId" = si."Id"
WHERE o."AssignedSiId" IS NOT NULL
LIMIT 10;
