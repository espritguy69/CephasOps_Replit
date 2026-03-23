using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CephasOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ExternalIdempotencyTenantScopeUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ExternalIdempotencyRecords_IdempotencyKey",
                table: "ExternalIdempotencyRecords");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastAccessedAtUtc",
                table: "Files",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StorageTier",
                table: "Files",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Hot");

            migrationBuilder.CreateTable(
                name: "TenantAnomalyEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Severity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Details = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ResolvedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantAnomalyEvents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_CompanyId_IsActive",
                table: "Users",
                columns: new[] { "CompanyId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CompanyId_CreatedAt",
                table: "Orders",
                columns: new[] { "CompanyId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_JobExecutions_CompanyId_CreatedAtUtc",
                table: "JobExecutions",
                columns: new[] { "CompanyId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Files_CompanyId_CreatedAt",
                table: "Files",
                columns: new[] { "CompanyId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Files_CompanyId_StorageTier",
                table: "Files",
                columns: new[] { "CompanyId", "StorageTier" });

            migrationBuilder.CreateIndex(
                name: "IX_ExternalIdempotencyRecords_ConnectorKey_CompanyId_Idempoten~",
                table: "ExternalIdempotencyRecords",
                columns: new[] { "ConnectorKey", "CompanyId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantAnomalyEvents_Severity_OccurredAtUtc",
                table: "TenantAnomalyEvents",
                columns: new[] { "Severity", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_TenantAnomalyEvents_TenantId",
                table: "TenantAnomalyEvents",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantAnomalyEvents_TenantId_OccurredAtUtc",
                table: "TenantAnomalyEvents",
                columns: new[] { "TenantId", "OccurredAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TenantAnomalyEvents");

            migrationBuilder.DropIndex(
                name: "IX_Users_CompanyId_IsActive",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Orders_CompanyId_CreatedAt",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_JobExecutions_CompanyId_CreatedAtUtc",
                table: "JobExecutions");

            migrationBuilder.DropIndex(
                name: "IX_Files_CompanyId_CreatedAt",
                table: "Files");

            migrationBuilder.DropIndex(
                name: "IX_Files_CompanyId_StorageTier",
                table: "Files");

            migrationBuilder.DropIndex(
                name: "IX_ExternalIdempotencyRecords_ConnectorKey_CompanyId_Idempoten~",
                table: "ExternalIdempotencyRecords");

            migrationBuilder.DropColumn(
                name: "LastAccessedAtUtc",
                table: "Files");

            migrationBuilder.DropColumn(
                name: "StorageTier",
                table: "Files");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalIdempotencyRecords_IdempotencyKey",
                table: "ExternalIdempotencyRecords",
                column: "IdempotencyKey",
                unique: true);
        }
    }
}
