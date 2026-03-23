using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CephasOps.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Links legacy flat order types to their parents and ensures the four parent rows exist.
    /// Legacy codes: MODIFICATION_INDOOR, MODIFICATION_OUTDOOR, STANDARD, REPULL, UPGRADE, IAD, FIXED_IP.
    /// Parents: ACTIVATION, MODIFICATION, ASSURANCE, VALUE_ADDED_SERVICE.
    /// No existing orders break: we only set ParentOrderTypeId on OrderTypes; Order.OrderTypeId is unchanged.
    /// </summary>
    public partial class FixOrderTypesHierarchyData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var sql = @"
-- Ensure parent order types exist (one per distinct CompanyId from OrderTypes)
INSERT INTO ""OrderTypes"" (""Id"", ""CompanyId"", ""DepartmentId"", ""ParentOrderTypeId"", ""Name"", ""Code"", ""Description"", ""DisplayOrder"", ""IsActive"", ""CreatedAt"", ""UpdatedAt"", ""IsDeleted"")
SELECT gen_random_uuid(), d.""CompanyId"", d.""DepartmentId"", NULL, 'Activation', 'ACTIVATION', 'New installation + activation of service', 1, true, NOW(), NOW(), false
FROM (SELECT DISTINCT ON (COALESCE(""CompanyId""::text, 'x')) ""CompanyId"", ""DepartmentId"" FROM ""OrderTypes"" LIMIT 500) d
WHERE NOT EXISTS (SELECT 1 FROM ""OrderTypes"" p WHERE p.""Code"" = 'ACTIVATION' AND p.""ParentOrderTypeId"" IS NULL AND (p.""CompanyId"" IS NOT DISTINCT FROM d.""CompanyId""));

INSERT INTO ""OrderTypes"" (""Id"", ""CompanyId"", ""DepartmentId"", ""ParentOrderTypeId"", ""Name"", ""Code"", ""Description"", ""DisplayOrder"", ""IsActive"", ""CreatedAt"", ""UpdatedAt"", ""IsDeleted"")
SELECT gen_random_uuid(), d.""CompanyId"", d.""DepartmentId"", NULL, 'Modification', 'MODIFICATION', 'Modification of existing service', 2, true, NOW(), NOW(), false
FROM (SELECT DISTINCT ON (COALESCE(""CompanyId""::text, 'x')) ""CompanyId"", ""DepartmentId"" FROM ""OrderTypes"" LIMIT 500) d
WHERE NOT EXISTS (SELECT 1 FROM ""OrderTypes"" p WHERE p.""Code"" = 'MODIFICATION' AND p.""ParentOrderTypeId"" IS NULL AND (p.""CompanyId"" IS NOT DISTINCT FROM d.""CompanyId""));

INSERT INTO ""OrderTypes"" (""Id"", ""CompanyId"", ""DepartmentId"", ""ParentOrderTypeId"", ""Name"", ""Code"", ""Description"", ""DisplayOrder"", ""IsActive"", ""CreatedAt"", ""UpdatedAt"", ""IsDeleted"")
SELECT gen_random_uuid(), d.""CompanyId"", d.""DepartmentId"", NULL, 'Assurance', 'ASSURANCE', 'Fault repair and troubleshooting', 3, true, NOW(), NOW(), false
FROM (SELECT DISTINCT ON (COALESCE(""CompanyId""::text, 'x')) ""CompanyId"", ""DepartmentId"" FROM ""OrderTypes"" LIMIT 500) d
WHERE NOT EXISTS (SELECT 1 FROM ""OrderTypes"" p WHERE p.""Code"" = 'ASSURANCE' AND p.""ParentOrderTypeId"" IS NULL AND (p.""CompanyId"" IS NOT DISTINCT FROM d.""CompanyId""));

INSERT INTO ""OrderTypes"" (""Id"", ""CompanyId"", ""DepartmentId"", ""ParentOrderTypeId"", ""Name"", ""Code"", ""Description"", ""DisplayOrder"", ""IsActive"", ""CreatedAt"", ""UpdatedAt"", ""IsDeleted"")
SELECT gen_random_uuid(), d.""CompanyId"", d.""DepartmentId"", NULL, 'Value Added Service', 'VALUE_ADDED_SERVICE', 'Additional services beyond standard installation/repair', 4, true, NOW(), NOW(), false
FROM (SELECT DISTINCT ON (COALESCE(""CompanyId""::text, 'x')) ""CompanyId"", ""DepartmentId"" FROM ""OrderTypes"" LIMIT 500) d
WHERE NOT EXISTS (SELECT 1 FROM ""OrderTypes"" p WHERE p.""Code"" = 'VALUE_ADDED_SERVICE' AND p.""ParentOrderTypeId"" IS NULL AND (p.""CompanyId"" IS NOT DISTINCT FROM d.""CompanyId""));

-- Link legacy MODIFICATION_INDOOR and MODIFICATION_OUTDOOR to MODIFICATION
UPDATE ""OrderTypes"" AS child
SET ""ParentOrderTypeId"" = parent.""Id"", ""UpdatedAt"" = NOW()
FROM ""OrderTypes"" AS parent
WHERE parent.""Code"" = 'MODIFICATION' AND parent.""ParentOrderTypeId"" IS NULL
  AND (parent.""CompanyId"" IS NOT DISTINCT FROM child.""CompanyId"")
  AND child.""Code"" IN ('MODIFICATION_INDOOR', 'MODIFICATION_OUTDOOR') AND child.""ParentOrderTypeId"" IS NULL;

-- Link legacy STANDARD and REPULL to ASSURANCE
UPDATE ""OrderTypes"" AS child
SET ""ParentOrderTypeId"" = parent.""Id"", ""UpdatedAt"" = NOW()
FROM ""OrderTypes"" AS parent
WHERE parent.""Code"" = 'ASSURANCE' AND parent.""ParentOrderTypeId"" IS NULL
  AND (parent.""CompanyId"" IS NOT DISTINCT FROM child.""CompanyId"")
  AND child.""Code"" IN ('STANDARD', 'REPULL') AND child.""ParentOrderTypeId"" IS NULL;

-- Link legacy UPGRADE, IAD, FIXED_IP to VALUE_ADDED_SERVICE
UPDATE ""OrderTypes"" AS child
SET ""ParentOrderTypeId"" = parent.""Id"", ""UpdatedAt"" = NOW()
FROM ""OrderTypes"" AS parent
WHERE parent.""Code"" = 'VALUE_ADDED_SERVICE' AND parent.""ParentOrderTypeId"" IS NULL
  AND (parent.""CompanyId"" IS NOT DISTINCT FROM child.""CompanyId"")
  AND child.""Code"" IN ('UPGRADE', 'IAD', 'FIXED_IP') AND child.""ParentOrderTypeId"" IS NULL;

-- Normalize display names for legacy subtypes so settings show: Indoor, Outdoor, Standard, Repull, Upgrade, IAD, Fixed IP
UPDATE ""OrderTypes"" SET ""Name"" = 'Indoor', ""UpdatedAt"" = NOW() WHERE ""Code"" = 'MODIFICATION_INDOOR';
UPDATE ""OrderTypes"" SET ""Name"" = 'Outdoor', ""UpdatedAt"" = NOW() WHERE ""Code"" = 'MODIFICATION_OUTDOOR';
UPDATE ""OrderTypes"" SET ""Name"" = 'Standard', ""UpdatedAt"" = NOW() WHERE ""Code"" = 'STANDARD';
UPDATE ""OrderTypes"" SET ""Name"" = 'Repull', ""UpdatedAt"" = NOW() WHERE ""Code"" = 'REPULL';
UPDATE ""OrderTypes"" SET ""Name"" = 'Upgrade', ""UpdatedAt"" = NOW() WHERE ""Code"" = 'UPGRADE';
UPDATE ""OrderTypes"" SET ""Name"" = 'IAD', ""UpdatedAt"" = NOW() WHERE ""Code"" = 'IAD';
UPDATE ""OrderTypes"" SET ""Name"" = 'Fixed IP', ""UpdatedAt"" = NOW() WHERE ""Code"" = 'FIXED_IP';
";
            migrationBuilder.Sql(sql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Unlink: set ParentOrderTypeId back to NULL for the legacy codes we linked
            var sql = @"
UPDATE ""OrderTypes""
SET ""ParentOrderTypeId"" = NULL, ""UpdatedAt"" = NOW()
WHERE ""Code"" IN (
  'MODIFICATION_INDOOR', 'MODIFICATION_OUTDOOR',
  'STANDARD', 'REPULL',
  'UPGRADE', 'IAD', 'FIXED_IP'
) AND ""ParentOrderTypeId"" IS NOT NULL;
";
            migrationBuilder.Sql(sql);
        }
    }
}
