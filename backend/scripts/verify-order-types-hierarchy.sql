SELECT "Code", "Name", "ParentOrderTypeId" IS NOT NULL AS has_parent
FROM "OrderTypes"
WHERE "IsDeleted" = false
ORDER BY "ParentOrderTypeId" NULLS FIRST, "DisplayOrder", "Code";
