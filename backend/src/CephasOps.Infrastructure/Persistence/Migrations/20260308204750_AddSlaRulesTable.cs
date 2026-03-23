using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CephasOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSlaRulesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ReplayOperations and ReplayOperationEvents are created by AddReplayOperations migration

            migrationBuilder.CreateTable(
                name: "SlaRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RuleType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TargetType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TargetName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    MaxDurationSeconds = table.Column<int>(type: "integer", nullable: false),
                    WarningThresholdSeconds = table.Column<int>(type: "integer", nullable: true),
                    EscalationThresholdSeconds = table.Column<int>(type: "integer", nullable: true),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
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
                    table.PrimaryKey("PK_SlaRules", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SlaRules_CompanyId",
                table: "SlaRules",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_SlaRules_CompanyId_Enabled_RuleType",
                table: "SlaRules",
                columns: new[] { "CompanyId", "Enabled", "RuleType" });

            migrationBuilder.CreateIndex(
                name: "IX_SlaRules_CompanyId_TargetType_TargetName",
                table: "SlaRules",
                columns: new[] { "CompanyId", "TargetType", "TargetName" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SlaRules");
        }
    }
}
