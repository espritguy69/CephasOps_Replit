using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CephasOps.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// 1) Deduplicates parent order types: for each (CompanyId, Code) where ParentOrderTypeId IS NULL,
    ///    keeps one canonical row (prefer with children, then lowest DisplayOrder, then oldest CreatedAt),
    ///    reassigns all FKs from duplicate parents to canonical, then soft-deletes duplicates.
    /// 2) Adds unique partial indexes so duplicates cannot recur.
    /// </summary>
    public partial class DedupeOrderTypeParentsAndAddUniqueIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var cleanupSql = @"
CREATE TEMP TABLE IF NOT EXISTS _order_type_dup_map (duplicate_id uuid, canonical_id uuid);

TRUNCATE _order_type_dup_map;

INSERT INTO _order_type_dup_map (duplicate_id, canonical_id)
SELECT p.""Id"", c.canonical_id
FROM ""OrderTypes"" p
INNER JOIN (
  SELECT ""Id"" AS canonical_id, ""CompanyId"", UPPER(TRIM(""Code"")) AS code_key
  FROM (
    SELECT ""Id"", ""CompanyId"", ""Code"",
      ROW_NUMBER() OVER (
        PARTITION BY ""CompanyId"", UPPER(TRIM(""Code""))
        ORDER BY (SELECT COUNT(*) FROM ""OrderTypes"" c WHERE c.""ParentOrderTypeId"" = ""OrderTypes"".""Id"" AND (c.""IsDeleted"" IS NOT DISTINCT FROM false)) DESC,
                 ""DisplayOrder"" ASC,
                 ""CreatedAt"" ASC
      ) AS rn
    FROM ""OrderTypes""
    WHERE ""ParentOrderTypeId"" IS NULL AND (""IsDeleted"" IS NOT DISTINCT FROM false)
  ) ranked
  WHERE rn = 1
) c ON (p.""CompanyId"" IS NOT DISTINCT FROM c.""CompanyId"") AND UPPER(TRIM(p.""Code"")) = c.code_key
  AND p.""ParentOrderTypeId"" IS NULL AND (p.""IsDeleted"" IS NOT DISTINCT FROM false) AND p.""Id"" <> c.canonical_id;

UPDATE ""OrderTypes"" AS child SET ""ParentOrderTypeId"" = d.canonical_id, ""UpdatedAt"" = NOW()
FROM _order_type_dup_map d WHERE child.""ParentOrderTypeId"" = d.duplicate_id;

UPDATE ""Orders"" o SET ""OrderTypeId"" = d.canonical_id FROM _order_type_dup_map d WHERE o.""OrderTypeId"" = d.duplicate_id;
UPDATE ""JobEarningRecords"" j SET ""OrderTypeId"" = d.canonical_id FROM _order_type_dup_map d WHERE j.""OrderTypeId"" = d.duplicate_id;
UPDATE ""BuildingDefaultMaterials"" b SET ""OrderTypeId"" = d.canonical_id FROM _order_type_dup_map d WHERE b.""OrderTypeId"" = d.duplicate_id;
UPDATE ""BillingRatecards"" b SET ""OrderTypeId"" = d.canonical_id FROM _order_type_dup_map d WHERE b.""OrderTypeId"" = d.duplicate_id;
UPDATE ""GponPartnerJobRates"" g SET ""OrderTypeId"" = d.canonical_id FROM _order_type_dup_map d WHERE g.""OrderTypeId"" = d.duplicate_id;
UPDATE ""GponSiJobRates"" g SET ""OrderTypeId"" = d.canonical_id FROM _order_type_dup_map d WHERE g.""OrderTypeId"" = d.duplicate_id;
UPDATE ""GponSiCustomRates"" g SET ""OrderTypeId"" = d.canonical_id FROM _order_type_dup_map d WHERE g.""OrderTypeId"" = d.duplicate_id;
UPDATE ""ParserTemplates"" pt SET ""OrderTypeId"" = d.canonical_id FROM _order_type_dup_map d WHERE pt.""OrderTypeId"" = d.duplicate_id;

UPDATE ""OrderTypes"" ot SET ""IsDeleted"" = true, ""DeletedAt"" = NOW(), ""UpdatedAt"" = NOW()
FROM _order_type_dup_map d WHERE ot.""Id"" = d.duplicate_id;
";
            migrationBuilder.Sql(cleanupSql);

            // Drop old non-unique index if it exists (EF may have created IX_OrderTypes_CompanyId_Code)
            migrationBuilder.Sql(@"
DO $$
BEGIN
  IF EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'IX_OrderTypes_CompanyId_Code') THEN
    DROP INDEX ""IX_OrderTypes_CompanyId_Code"";
  END IF;
END $$;
");

            // Unique partial index: one parent per (CompanyId, Code)
            migrationBuilder.Sql(@"
CREATE UNIQUE INDEX IF NOT EXISTS ""IX_OrderTypes_CompanyId_Code_Parents""
ON ""OrderTypes"" (""CompanyId"", ""Code"")
WHERE ""ParentOrderTypeId"" IS NULL AND (""IsDeleted"" IS NOT DISTINCT FROM false);
");

            // Deduplicate subtypes (one per CompanyId, ParentOrderTypeId, Code) so the subtype unique index can be created
            migrationBuilder.Sql(@"
CREATE TEMP TABLE IF NOT EXISTS _subtype_dup_map (duplicate_id uuid, canonical_id uuid);
TRUNCATE _subtype_dup_map;
INSERT INTO _subtype_dup_map (duplicate_id, canonical_id)
SELECT s.""Id"", c.canonical_id
FROM ""OrderTypes"" s
INNER JOIN (
  SELECT ""Id"" AS canonical_id, ""CompanyId"", ""ParentOrderTypeId"", UPPER(TRIM(""Code"")) AS code_key
  FROM (
    SELECT ""Id"", ""CompanyId"", ""ParentOrderTypeId"", ""Code"",
      ROW_NUMBER() OVER (
        PARTITION BY ""CompanyId"", ""ParentOrderTypeId"", UPPER(TRIM(""Code""))
        ORDER BY ""DisplayOrder"" ASC, ""CreatedAt"" ASC
      ) AS rn
    FROM ""OrderTypes""
    WHERE ""ParentOrderTypeId"" IS NOT NULL AND (""IsDeleted"" IS NOT DISTINCT FROM false)
  ) ranked
  WHERE rn = 1
) c ON (s.""CompanyId"" IS NOT DISTINCT FROM c.""CompanyId"") AND s.""ParentOrderTypeId"" = c.""ParentOrderTypeId"" AND UPPER(TRIM(s.""Code"")) = c.code_key
  AND (""IsDeleted"" IS NOT DISTINCT FROM false) AND s.""Id"" <> c.canonical_id;

UPDATE ""Orders"" o SET ""OrderTypeId"" = d.canonical_id FROM _subtype_dup_map d WHERE o.""OrderTypeId"" = d.duplicate_id;
UPDATE ""JobEarningRecords"" j SET ""OrderTypeId"" = d.canonical_id FROM _subtype_dup_map d WHERE j.""OrderTypeId"" = d.duplicate_id;
UPDATE ""BuildingDefaultMaterials"" b SET ""OrderTypeId"" = d.canonical_id FROM _subtype_dup_map d WHERE b.""OrderTypeId"" = d.duplicate_id;
UPDATE ""BillingRatecards"" b SET ""OrderTypeId"" = d.canonical_id FROM _subtype_dup_map d WHERE b.""OrderTypeId"" = d.duplicate_id;
UPDATE ""GponPartnerJobRates"" g SET ""OrderTypeId"" = d.canonical_id FROM _subtype_dup_map d WHERE g.""OrderTypeId"" = d.duplicate_id;
UPDATE ""GponSiJobRates"" g SET ""OrderTypeId"" = d.canonical_id FROM _subtype_dup_map d WHERE g.""OrderTypeId"" = d.duplicate_id;
UPDATE ""GponSiCustomRates"" g SET ""OrderTypeId"" = d.canonical_id FROM _subtype_dup_map d WHERE g.""OrderTypeId"" = d.duplicate_id;
UPDATE ""ParserTemplates"" pt SET ""OrderTypeId"" = d.canonical_id FROM _subtype_dup_map d WHERE pt.""OrderTypeId"" = d.duplicate_id;
UPDATE ""OrderTypes"" ot SET ""IsDeleted"" = true, ""DeletedAt"" = NOW(), ""UpdatedAt"" = NOW()
FROM _subtype_dup_map d WHERE ot.""Id"" = d.duplicate_id;
");

            // Unique partial index: one subtype per (CompanyId, ParentOrderTypeId, Code)
            migrationBuilder.Sql(@"
CREATE UNIQUE INDEX IF NOT EXISTS ""IX_OrderTypes_CompanyId_ParentId_Code_Subtypes""
ON ""OrderTypes"" (""CompanyId"", ""ParentOrderTypeId"", ""Code"")
WHERE ""ParentOrderTypeId"" IS NOT NULL AND (""IsDeleted"" IS NOT DISTINCT FROM false);
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OrderTypes_CompanyId_Code_Parents",
                table: "OrderTypes");

            migrationBuilder.DropIndex(
                name: "IX_OrderTypes_CompanyId_ParentId_Code_Subtypes",
                table: "OrderTypes");

            // Restore non-unique index for (CompanyId, Code) if needed
            migrationBuilder.CreateIndex(
                name: "IX_OrderTypes_CompanyId_Code",
                table: "OrderTypes",
                columns: new[] { "CompanyId", "Code" });
        }
    }
}
