using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CephasOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBackgroundJobWorkerOwnership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ParsedMaterialAliases: idempotent and conditional. This migration was generated when the
            // snapshot included ParsedMaterialAliases (from a later migration 20260311120000_AddParsedMaterialAlias
            // which has no Designer and is not in the discovered chain). In DBs where that table was never
            // applied, or indexes differ, we only run these changes if the table exists.
            migrationBuilder.Sql(@"
DO $$
BEGIN
  IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'ParsedMaterialAliases') THEN
    DROP INDEX IF EXISTS ""IX_ParsedMaterialAliases_CompanyId_NormalizedAliasText"";
    DROP INDEX IF EXISTS ""IX_ParsedMaterialAliases_MaterialId"";
    ALTER TABLE ""ParsedMaterialAliases"" ALTER COLUMN ""Source"" TYPE text;
    ALTER TABLE ""ParsedMaterialAliases"" ALTER COLUMN ""RowVersion"" DROP DEFAULT;
    ALTER TABLE ""ParsedMaterialAliases"" ALTER COLUMN ""NormalizedAliasText"" TYPE text;
    ALTER TABLE ""ParsedMaterialAliases"" ALTER COLUMN ""IsActive"" DROP DEFAULT;
    ALTER TABLE ""ParsedMaterialAliases"" ALTER COLUMN ""AliasText"" TYPE text;
  END IF;
END $$;");

            migrationBuilder.AddColumn<DateTime>(
                name: "ClaimedAtUtc",
                table: "BackgroundJobs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "WorkerId",
                table: "BackgroundJobs",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BackgroundJobs_State_WorkerId",
                table: "BackgroundJobs",
                columns: new[] { "State", "WorkerId" });

            migrationBuilder.CreateIndex(
                name: "IX_BackgroundJobs_WorkerId",
                table: "BackgroundJobs",
                column: "WorkerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BackgroundJobs_State_WorkerId",
                table: "BackgroundJobs");

            migrationBuilder.DropIndex(
                name: "IX_BackgroundJobs_WorkerId",
                table: "BackgroundJobs");

            migrationBuilder.DropColumn(
                name: "ClaimedAtUtc",
                table: "BackgroundJobs");

            migrationBuilder.DropColumn(
                name: "WorkerId",
                table: "BackgroundJobs");

            // Reverse ParsedMaterialAliases changes only if table exists (idempotent Down).
            migrationBuilder.Sql(@"
DO $$
BEGIN
  IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'ParsedMaterialAliases') THEN
    ALTER TABLE ""ParsedMaterialAliases"" ALTER COLUMN ""Source"" TYPE character varying(50);
    ALTER TABLE ""ParsedMaterialAliases"" ALTER COLUMN ""RowVersion"" SET DEFAULT gen_random_bytes(8);
    ALTER TABLE ""ParsedMaterialAliases"" ALTER COLUMN ""NormalizedAliasText"" TYPE character varying(500);
    ALTER TABLE ""ParsedMaterialAliases"" ALTER COLUMN ""IsActive"" SET DEFAULT true;
    ALTER TABLE ""ParsedMaterialAliases"" ALTER COLUMN ""AliasText"" TYPE character varying(500);
    CREATE INDEX IF NOT EXISTS ""IX_ParsedMaterialAliases_CompanyId_NormalizedAliasText"" ON ""ParsedMaterialAliases"" (""CompanyId"", ""NormalizedAliasText"");
    CREATE INDEX IF NOT EXISTS ""IX_ParsedMaterialAliases_MaterialId"" ON ""ParsedMaterialAliases"" (""MaterialId"");
  END IF;
END $$;");
        }
    }
}
