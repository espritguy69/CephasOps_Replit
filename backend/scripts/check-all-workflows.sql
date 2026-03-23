-- Check all workflow definitions including deleted ones
SELECT 
    "Id", 
    "Name", 
    "EntityType", 
    "CompanyId", 
    "DepartmentId", 
    "IsActive", 
    "IsDeleted",
    "CreatedAt"
FROM "WorkflowDefinitions"
ORDER BY "CreatedAt" DESC;

-- Count by status
SELECT 
    COUNT(*) as total,
    COUNT(CASE WHEN "IsDeleted" = false THEN 1 END) as not_deleted,
    COUNT(CASE WHEN "IsDeleted" = true THEN 1 END) as deleted,
    COUNT(CASE WHEN "IsActive" = true AND "IsDeleted" = false THEN 1 END) as active_not_deleted
FROM "WorkflowDefinitions";

