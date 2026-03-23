-- ============================================
-- QUICK CHECK - Service Installers Current State
-- Copy and paste this into pgAdmin or psql
-- ============================================

-- 1. Total counts
SELECT 
    COUNT(*) as "Total SIs",
    COUNT(*) FILTER (WHERE "IsActive" = true) as "Active",
    COUNT(*) FILTER (WHERE "IsSubcontractor" = false) as "Employees",
    COUNT(*) FILTER (WHERE "IsSubcontractor" = true) as "Subcontractors"
FROM "ServiceInstallers";

-- 2. Find duplicates
SELECT 
    si."Name",
    si."EmployeeId",
    COUNT(*) as "Count",
    STRING_AGG(CASE WHEN si."IsSubcontractor" THEN 'Subcontractor' ELSE 'Employee' END, ', ') as "Types"
FROM "ServiceInstallers" si
WHERE si."IsActive" = true
GROUP BY si."Name", si."EmployeeId"
HAVING COUNT(*) > 1;

-- 3. List all active SIs
SELECT 
    ROW_NUMBER() OVER (ORDER BY si."Name") as "#",
    si."Name",
    si."EmployeeId",
    CASE WHEN si."IsSubcontractor" THEN 'Subcontractor' ELSE 'Employee' END as "Type",
    si."SiLevel" as "Level",
    d."Name" as "Department"
FROM "ServiceInstallers" si
LEFT JOIN "Departments" d ON si."DepartmentId" = d."Id"
WHERE si."IsActive" = true
ORDER BY si."Name";

