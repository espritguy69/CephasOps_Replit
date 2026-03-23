using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CephasOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderFinancialAlerts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderTypes_OrderTypes_ParentOrderTypeId",
                table: "OrderTypes");

            // Partners.Code already added by AddCodeToPartners migration

            // Only alter ParserReplayRuns if table exists (e.g. missing when migration history is ahead of schema)
            migrationBuilder.Sql(@"
DO $$
BEGIN
  IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'ParserReplayRuns') THEN
    ALTER TABLE ""ParserReplayRuns"" ALTER COLUMN ""OldConfidence"" TYPE numeric;
    ALTER TABLE ""ParserReplayRuns"" ALTER COLUMN ""NewConfidence"" TYPE numeric;
  END IF;
END $$;");

            migrationBuilder.CreateTable(
                name: "OrderFinancialAlerts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    AlertCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Severity = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Message = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    RevenueAmount = table.Column<decimal>(type: "numeric", nullable: true),
                    PayoutAmount = table.Column<decimal>(type: "numeric", nullable: true),
                    ProfitAmount = table.Column<decimal>(type: "numeric", nullable: true),
                    MarginPercent = table.Column<decimal>(type: "numeric", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderFinancialAlerts", x => x.Id);
                });

            migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS ""IX_Orders_PartnerId"" ON ""Orders"" (""PartnerId"");");

            migrationBuilder.CreateIndex(
                name: "IX_OrderFinancialAlerts_CompanyId",
                table: "OrderFinancialAlerts",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderFinancialAlerts_CreatedAt",
                table: "OrderFinancialAlerts",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_OrderFinancialAlerts_IsActive",
                table: "OrderFinancialAlerts",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_OrderFinancialAlerts_OrderId_AlertCode",
                table: "OrderFinancialAlerts",
                columns: new[] { "OrderId", "AlertCode" });

            migrationBuilder.Sql(@"
DO $$
BEGIN
  IF NOT EXISTS (
    SELECT 1 FROM pg_constraint WHERE conname = 'FK_Orders_Partners_PartnerId'
  ) THEN
    ALTER TABLE ""Orders"" ADD CONSTRAINT ""FK_Orders_Partners_PartnerId""
      FOREIGN KEY (""PartnerId"") REFERENCES ""Partners"" (""Id"") ON DELETE RESTRICT;
  END IF;
END $$;");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderTypes_OrderTypes_ParentOrderTypeId",
                table: "OrderTypes",
                column: "ParentOrderTypeId",
                principalTable: "OrderTypes",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Partners_PartnerId",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderTypes_OrderTypes_ParentOrderTypeId",
                table: "OrderTypes");

            migrationBuilder.DropTable(
                name: "OrderFinancialAlerts");

            migrationBuilder.DropIndex(
                name: "IX_Orders_PartnerId",
                table: "Orders");

            // Do not drop Partners.Code (owned by AddCodeToPartners)

            migrationBuilder.Sql(@"
DO $$
BEGIN
  IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'ParserReplayRuns') THEN
    ALTER TABLE ""ParserReplayRuns"" ALTER COLUMN ""OldConfidence"" TYPE numeric(18,4);
    ALTER TABLE ""ParserReplayRuns"" ALTER COLUMN ""NewConfidence"" TYPE numeric(18,4);
  END IF;
END $$;");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderTypes_OrderTypes_ParentOrderTypeId",
                table: "OrderTypes",
                column: "ParentOrderTypeId",
                principalTable: "OrderTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
