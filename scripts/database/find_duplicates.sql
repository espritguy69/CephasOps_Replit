-- Find duplicate service installers by Name
SELECT 
    si."Name",
    si."EmployeeId",
    si."IsSubcontractor",
    si."SiLevel",
    COUNT(*) as duplicate_count
FROM "ServiceInstallers" si
WHERE si."IsActive" = true
GROUP BY si."Name", si."EmployeeId", si."IsSubcontractor", si."SiLevel"
HAVING COUNT(*) > 1
ORDER BY COUNT(*) DESC, si."Name";

-- Show detailed duplicate records
SELECT 
    si."Id",
    si."Name",
    si."EmployeeId",
    si."IsSubcontractor",
    si."SiLevel",
    si."Phone",
    si."Email",
    si."DepartmentId",
    d."Name" as DepartmentName,
    si."CreatedAt",
    si."UpdatedAt"
FROM "ServiceInstallers" si
LEFT JOIN "Departments" d ON si."DepartmentId" = d."Id"
WHERE si."IsActive" = true
  AND si."Name" IN (
    SELECT "Name"
    FROM "ServiceInstallers"
    WHERE "IsActive" = true
    GROUP BY "Name"
    HAVING COUNT(*) > 1
  )
ORDER BY si."Name", si."IsSubcontractor", si."CreatedAt";

-- Check if there are unique constraints
SELECT 
    indexname,
    indexdef
FROM pg_indexes
WHERE tablename = 'ServiceInstallers'
  AND indexdef LIKE '%UNIQUE%';

