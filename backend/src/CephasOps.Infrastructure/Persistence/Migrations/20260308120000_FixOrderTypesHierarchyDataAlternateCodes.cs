using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CephasOps.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Links legacy order type rows that use alternate codes (e.g. INDOOR instead of MODIFICATION_INDOOR,
    /// ASSURANCE-REPULL, FIXEDIP) to the correct parent. Case-insensitive. Only updates rows where
    /// ParentOrderTypeId IS NULL. Does not change Order IDs; existing orders continue to reference the same OrderTypeId.
    /// </summary>
    public partial class FixOrderTypesHierarchyDataAlternateCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var sql = @"
-- Link INDOOR / OUTDOOR (and variants) to MODIFICATION (case-insensitive)
UPDATE ""OrderTypes"" AS child
SET ""ParentOrderTypeId"" = parent.""Id"", ""UpdatedAt"" = NOW()
FROM ""OrderTypes"" AS parent
WHERE parent.""Code"" = 'MODIFICATION' AND parent.""ParentOrderTypeId"" IS NULL
  AND (parent.""CompanyId"" IS NOT DISTINCT FROM child.""CompanyId"")
  AND child.""ParentOrderTypeId"" IS NULL
  AND UPPER(TRIM(child.""Code"")) IN ('INDOOR', 'OUTDOOR', 'MODIFICATION_INDOOR', 'MODIFICATION_OUTDOOR', 'MODIFICATION INDOOR', 'MODIFICATION OUTDOOR');

-- Link ASSURANCE-REPULL and REPULL to ASSURANCE (case-insensitive)
UPDATE ""OrderTypes"" AS child
SET ""ParentOrderTypeId"" = parent.""Id"", ""UpdatedAt"" = NOW()
FROM ""OrderTypes"" AS parent
WHERE parent.""Code"" = 'ASSURANCE' AND parent.""ParentOrderTypeId"" IS NULL
  AND (parent.""CompanyId"" IS NOT DISTINCT FROM child.""CompanyId"")
  AND child.""ParentOrderTypeId"" IS NULL
  AND UPPER(TRIM(child.""Code"")) IN ('ASSURANCE-REPULL', 'REPULL', 'STANDARD');

-- Link FIXEDIP (no underscore) and FIXED_IP to VALUE_ADDED_SERVICE (case-insensitive)
UPDATE ""OrderTypes"" AS child
SET ""ParentOrderTypeId"" = parent.""Id"", ""UpdatedAt"" = NOW()
FROM ""OrderTypes"" AS parent
WHERE parent.""Code"" = 'VALUE_ADDED_SERVICE' AND parent.""ParentOrderTypeId"" IS NULL
  AND (parent.""CompanyId"" IS NOT DISTINCT FROM child.""CompanyId"")
  AND child.""ParentOrderTypeId"" IS NULL
  AND UPPER(TRIM(child.""Code"")) IN ('FIXEDIP', 'FIXED_IP', 'UPGRADE', 'IAD');

-- Normalize names for alternate-code rows so UI shows consistent labels
UPDATE ""OrderTypes"" SET ""Name"" = 'Indoor', ""UpdatedAt"" = NOW() WHERE UPPER(TRIM(""Code"")) IN ('INDOOR', 'MODIFICATION_INDOOR', 'MODIFICATION INDOOR');
UPDATE ""OrderTypes"" SET ""Name"" = 'Outdoor', ""UpdatedAt"" = NOW() WHERE UPPER(TRIM(""Code"")) IN ('OUTDOOR', 'MODIFICATION_OUTDOOR', 'MODIFICATION OUTDOOR');
UPDATE ""OrderTypes"" SET ""Name"" = 'Repull', ""UpdatedAt"" = NOW() WHERE UPPER(TRIM(""Code"")) IN ('ASSURANCE-REPULL', 'REPULL');
UPDATE ""OrderTypes"" SET ""Name"" = 'Standard', ""UpdatedAt"" = NOW() WHERE UPPER(TRIM(""Code"")) = 'STANDARD';
UPDATE ""OrderTypes"" SET ""Name"" = 'Fixed IP', ""UpdatedAt"" = NOW() WHERE UPPER(TRIM(""Code"")) IN ('FIXEDIP', 'FIXED_IP');
";
            migrationBuilder.Sql(sql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Unlink only the alternate codes we linked here (leave original FixOrderTypesHierarchyData linked)
            var sql = @"
UPDATE ""OrderTypes""
SET ""ParentOrderTypeId"" = NULL, ""UpdatedAt"" = NOW()
WHERE ""ParentOrderTypeId"" IS NOT NULL
  AND UPPER(TRIM(""Code"")) IN (
    'INDOOR', 'OUTDOOR', 'MODIFICATION INDOOR', 'MODIFICATION OUTDOOR',
    'ASSURANCE-REPULL', 'FIXEDIP'
  );
";
            migrationBuilder.Sql(sql);
        }
    }
}
