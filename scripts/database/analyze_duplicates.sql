-- Check for exact duplicates (same person, different IsSubcontractor flag)
SELECT 
    si."Name",
    si."EmployeeId",
    COUNT(*) as total_records,
    COUNT(CASE WHEN si."IsSubcontractor" = false THEN 1 END) as employee_count,
    COUNT(CASE WHEN si."IsSubcontractor" = true THEN 1 END) as subcontractor_count
FROM "ServiceInstallers" si
WHERE si."IsActive" = true
GROUP BY si."Name", si."EmployeeId"
HAVING COUNT(*) > 1
ORDER BY si."Name";

-- Show the actual duplicate records with all details
SELECT 
    si."Id",
    si."Name",
    si."EmployeeId",
    si."IsSubcontractor",
    si."SiLevel",
    si."Phone",
    si."DepartmentId",
    d."Name" as DepartmentName,
    si."CreatedAt"
FROM "ServiceInstallers" si
LEFT JOIN "Departments" d ON si."DepartmentId" = d."Id"
WHERE si."IsActive" = true
  AND (si."Name", si."EmployeeId") IN (
    SELECT "Name", "EmployeeId"
    FROM "ServiceInstallers"
    WHERE "IsActive" = true
    GROUP BY "Name", "EmployeeId"
    HAVING COUNT(*) > 1
  )
ORDER BY si."Name", si."IsSubcontractor";

