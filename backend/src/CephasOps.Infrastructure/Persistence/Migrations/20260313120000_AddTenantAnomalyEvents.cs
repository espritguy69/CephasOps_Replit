using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CephasOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantAnomalyEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                constraints: table => table.PrimaryKey("PK_TenantAnomalyEvents", x => x.Id));

            migrationBuilder.CreateIndex(name: "IX_TenantAnomalyEvents_TenantId", table: "TenantAnomalyEvents", column: "TenantId");
            migrationBuilder.CreateIndex(name: "IX_TenantAnomalyEvents_TenantId_OccurredAtUtc", table: "TenantAnomalyEvents", columns: new[] { "TenantId", "OccurredAtUtc" });
            migrationBuilder.CreateIndex(name: "IX_TenantAnomalyEvents_Severity_OccurredAtUtc", table: "TenantAnomalyEvents", columns: new[] { "Severity", "OccurredAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "TenantAnomalyEvents");
        }
    }
}
