using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CephasOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiTenantArchitecture : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "Users",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "Companies",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SubscriptionId",
                table: "Companies",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "BackgroundJobs",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_CompanyId",
                table: "Users",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_Code",
                table: "Companies",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_BackgroundJobs_CompanyId",
                table: "BackgroundJobs",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_BackgroundJobs_CompanyId_State_CreatedAt",
                table: "BackgroundJobs",
                columns: new[] { "CompanyId", "State", "CreatedAt" });

            // Data migration: default tenant, set Company Code, backfill CompanyId
            migrationBuilder.Sql(@"
DO $$
DECLARE
    v_tenant_id UUID;
    v_company_id UUID;
    r RECORD;
BEGIN
    -- Create default tenant (Cephas) if not exists
    SELECT ""Id"" INTO v_tenant_id FROM ""Tenants"" WHERE ""Slug"" = 'cephas' LIMIT 1;
    IF v_tenant_id IS NULL THEN
        INSERT INTO ""Tenants"" (""Id"", ""Name"", ""Slug"", ""IsActive"", ""CreatedAtUtc"", ""UpdatedAtUtc"")
        VALUES (gen_random_uuid(), 'Cephas', 'cephas', true, NOW(), NOW())
        RETURNING ""Id"" INTO v_tenant_id;
    END IF;

    -- Set default company: first company gets Code CEPHAS and TenantId
    UPDATE ""Companies""
    SET ""Code"" = 'CEPHAS', ""TenantId"" = v_tenant_id
    WHERE ""Id"" = (SELECT ""Id"" FROM ""Companies"" ORDER BY ""CreatedAt"" ASC LIMIT 1);

    SELECT ""Id"" INTO v_company_id FROM ""Companies"" WHERE ""Code"" = 'CEPHAS' LIMIT 1;
    IF v_company_id IS NULL THEN
        RETURN;
    END IF;

    -- Backfill company_id where NULL (all tables that have the column)
    FOR r IN
        SELECT table_name FROM information_schema.columns
        WHERE table_schema = 'public' AND column_name = 'company_id'
    LOOP
        EXECUTE format('UPDATE %I SET company_id = $1 WHERE company_id IS NULL', r.table_name) USING v_company_id;
    END LOOP;

    -- Users and BackgroundJobs use ""CompanyId"" (PascalCase) if not configured with ColumnName
    UPDATE ""Users"" SET ""CompanyId"" = v_company_id WHERE ""CompanyId"" IS NULL;
    UPDATE ""BackgroundJobs"" SET ""CompanyId"" = v_company_id WHERE ""CompanyId"" IS NULL;
END $$;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_CompanyId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Companies_Code",
                table: "Companies");

            migrationBuilder.DropIndex(
                name: "IX_BackgroundJobs_CompanyId",
                table: "BackgroundJobs");

            migrationBuilder.DropIndex(
                name: "IX_BackgroundJobs_CompanyId_State_CreatedAt",
                table: "BackgroundJobs");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "SubscriptionId",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "BackgroundJobs");
        }
    }
}
