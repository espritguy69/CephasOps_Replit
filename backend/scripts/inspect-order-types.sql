SELECT "Id", "Code", "Name", "ParentOrderTypeId", "CompanyId"
FROM "OrderTypes"
WHERE "IsDeleted" = false
ORDER BY "ParentOrderTypeId" NULLS FIRST, "Code";
