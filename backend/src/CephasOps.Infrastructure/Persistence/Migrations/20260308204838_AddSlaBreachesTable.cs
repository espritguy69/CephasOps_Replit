using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CephasOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSlaBreachesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SlaBreaches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: true),
                    RuleId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TargetId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DetectedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DurationSeconds = table.Column<double>(type: "double precision", nullable: false),
                    Severity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AcknowledgedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResolvedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResolvedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlaBreaches", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SlaBreaches_CompanyId",
                table: "SlaBreaches",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_SlaBreaches_CompanyId_Severity",
                table: "SlaBreaches",
                columns: new[] { "CompanyId", "Severity" });

            migrationBuilder.CreateIndex(
                name: "IX_SlaBreaches_CompanyId_Status_DetectedAtUtc",
                table: "SlaBreaches",
                columns: new[] { "CompanyId", "Status", "DetectedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SlaBreaches_CorrelationId",
                table: "SlaBreaches",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_SlaBreaches_RuleId",
                table: "SlaBreaches",
                column: "RuleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SlaBreaches");
        }
    }
}
