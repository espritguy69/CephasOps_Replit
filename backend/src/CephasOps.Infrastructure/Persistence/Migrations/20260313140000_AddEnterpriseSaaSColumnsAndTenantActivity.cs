using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CephasOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEnterpriseSaaSColumnsAndTenantActivity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "HealthScore",
                table: "TenantMetricsDaily",
                type: "integer",
                nullable: true);
            migrationBuilder.AddColumn<string>(
                name: "HealthStatus",
                table: "TenantMetricsDaily",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);
            migrationBuilder.AddColumn<int>(
                name: "RateLimitExceededCount",
                table: "TenantMetricsDaily",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "TenantActivityEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EntityType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: true),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    TimestampUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MetadataJson = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table => table.PrimaryKey("PK_TenantActivityEvents", x => x.Id));

            migrationBuilder.CreateIndex(
                name: "IX_TenantActivityEvents_TenantId_TimestampUtc",
                table: "TenantActivityEvents",
                columns: new[] { "TenantId", "TimestampUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "TenantActivityEvents");
            migrationBuilder.DropColumn(name: "HealthScore", table: "TenantMetricsDaily");
            migrationBuilder.DropColumn(name: "HealthStatus", table: "TenantMetricsDaily");
            migrationBuilder.DropColumn(name: "RateLimitExceededCount", table: "TenantMetricsDaily");
        }
    }
}
