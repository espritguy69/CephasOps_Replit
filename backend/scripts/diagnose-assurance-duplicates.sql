-- Run this first to inspect all top-level ASSURANCE order types and their reference counts.
-- Use the results to confirm which row is the duplicate and which is canonical before running the repair script.

SELECT
  ot."Id",
  ot."CompanyId",
  ot."Name",
  ot."Code",
  ot."ParentOrderTypeId",
  ot."DisplayOrder",
  ot."IsActive",
  ot."CreatedAt",
  ot."IsDeleted",
  (SELECT COUNT(*) FROM "OrderTypes" c WHERE c."ParentOrderTypeId" = ot."Id" AND (c."IsDeleted" IS NOT DISTINCT FROM false)) AS child_subtypes,
  (SELECT COUNT(*) FROM "Orders" o WHERE o."OrderTypeId" = ot."Id") AS orders_refs,
  (SELECT COUNT(*) FROM "JobEarningRecords" j WHERE j."OrderTypeId" = ot."Id") AS job_earning_refs,
  (SELECT COUNT(*) FROM "BuildingDefaultMaterials" b WHERE b."OrderTypeId" = ot."Id") AS building_default_material_refs,
  (SELECT COUNT(*) FROM "BillingRatecards" b WHERE b."OrderTypeId" = ot."Id") AS billing_ratecard_refs,
  (SELECT COUNT(*) FROM "GponPartnerJobRates" g WHERE g."OrderTypeId" = ot."Id") AS gpon_partner_job_rate_refs,
  (SELECT COUNT(*) FROM "GponSiJobRates" g WHERE g."OrderTypeId" = ot."Id") AS gpon_si_job_rate_refs,
  (SELECT COUNT(*) FROM "GponSiCustomRates" g WHERE g."OrderTypeId" = ot."Id") AS gpon_si_custom_rate_refs,
  (SELECT COUNT(*) FROM "ParserTemplates" pt WHERE pt."OrderTypeId" = ot."Id") AS parser_template_refs
FROM "OrderTypes" ot
WHERE ot."Code" = 'ASSURANCE'
  AND ot."ParentOrderTypeId" IS NULL
  AND (ot."IsDeleted" IS NOT DISTINCT FROM false)
ORDER BY ot."CompanyId", ot."CreatedAt";
