-- ============================================
-- Scheduler and Assignment Monitoring Script
-- Use this to check order assignment status
-- ============================================

-- 1. Unassigned Orders (Ready for Assignment)
SELECT 
    '=== UNASSIGNED ORDERS ===' as info;

SELECT 
    o."Id",
    o."OrderNumber",
    o."ServiceId",
    o."Status",
    o."CustomerName",
    o."AppointmentDate",
    o."AppointmentWindowFrom",
    o."AppointmentWindowTo",
    p."Name" as "PartnerName",
    ot."Name" as "OrderTypeName"
FROM "Orders" o
LEFT JOIN "Partners" p ON o."PartnerId" = p."Id"
LEFT JOIN "OrderTypes" ot ON o."OrderTypeId" = ot."Id"
WHERE o."AssignedSiId" IS NULL
AND o."Status" IN ('Pending', 'Scheduled')
AND o."AppointmentDate" >= CURRENT_DATE
ORDER BY o."AppointmentDate", o."AppointmentWindowFrom"
LIMIT 20;

-- 2. Assigned Orders
SELECT 
    '=== ASSIGNED ORDERS ===' as info;

SELECT 
    o."Id",
    o."OrderNumber",
    o."ServiceId",
    o."Status",
    o."CustomerName",
    o."AppointmentDate",
    si."Name" as "InstallerName",
    si."EmployeeId" as "InstallerId"
FROM "Orders" o
INNER JOIN "ServiceInstallers" si ON o."AssignedSiId" = si."Id"
WHERE o."AssignedSiId" IS NOT NULL
ORDER BY o."AppointmentDate" DESC
LIMIT 20;

-- 3. Scheduled Slots
SELECT 
    '=== SCHEDULED SLOTS ===' as info;

SELECT 
    ss."Id",
    ss."Date",
    ss."WindowFrom",
    ss."WindowTo",
    ss."Status",
    ss."SequenceIndex",
    o."OrderNumber",
    o."ServiceId",
    si."Name" as "InstallerName"
FROM "ScheduledSlots" ss
LEFT JOIN "Orders" o ON ss."OrderId" = o."Id"
LEFT JOIN "ServiceInstallers" si ON ss."ServiceInstallerId" = si."Id"
WHERE ss."Date" >= CURRENT_DATE
ORDER BY ss."Date", ss."WindowFrom", ss."SequenceIndex"
LIMIT 20;

-- 4. Service Installer Availability
SELECT 
    '=== SERVICE INSTALLER AVAILABILITY ===' as info;

SELECT 
    si."Id",
    si."Name",
    si."EmployeeId",
    d."Name" as "DepartmentName",
    COUNT(DISTINCT ss."Id") as "ScheduledSlots",
    COUNT(DISTINCT CASE WHEN ss."Date" = CURRENT_DATE THEN ss."Id" END) as "TodaySlots"
FROM "ServiceInstallers" si
LEFT JOIN "Departments" d ON si."DepartmentId" = d."Id"
LEFT JOIN "ScheduledSlots" ss ON si."Id" = ss."ServiceInstallerId" 
    AND ss."Date" >= CURRENT_DATE
WHERE si."IsActive" = true
GROUP BY si."Id", si."Name", si."EmployeeId", d."Name"
ORDER BY si."Name"
LIMIT 20;

-- 5. Orders by Status
SELECT 
    '=== ORDERS BY STATUS ===' as info;

SELECT 
    o."Status",
    COUNT(*) as "Count",
    COUNT(*) FILTER (WHERE o."AssignedSiId" IS NOT NULL) as "Assigned",
    COUNT(*) FILTER (WHERE o."AssignedSiId" IS NULL) as "Unassigned"
FROM "Orders" o
WHERE o."AppointmentDate" >= CURRENT_DATE - INTERVAL '7 days'
GROUP BY o."Status"
ORDER BY "Count" DESC;

