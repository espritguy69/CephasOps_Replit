using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CephasOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMigrationAuditTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MigrationAudit",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Environment = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    MigrationId = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    AppliedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AppliedBy = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    MethodUsed = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    VerificationStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SmokeTestStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MigrationAudit", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MigrationAudit_Environment_MigrationId",
                table: "MigrationAudit",
                columns: new[] { "Environment", "MigrationId" });

            migrationBuilder.CreateIndex(
                name: "IX_MigrationAudit_AppliedAtUtc",
                table: "MigrationAudit",
                column: "AppliedAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "MigrationAudit");
        }
    }
}
