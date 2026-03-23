using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CephasOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPayoutAnomalyAlerts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PayoutAnomalyAlerts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AnomalyFingerprintId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Channel = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    SentAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    RecipientId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayoutAnomalyAlerts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PayoutAnomalyAlerts_AnomalyFingerprintId",
                table: "PayoutAnomalyAlerts",
                column: "AnomalyFingerprintId");

            migrationBuilder.CreateIndex(
                name: "IX_PayoutAnomalyAlerts_AnomalyFingerprintId_Channel",
                table: "PayoutAnomalyAlerts",
                columns: new[] { "AnomalyFingerprintId", "Channel" });

            migrationBuilder.CreateIndex(
                name: "IX_PayoutAnomalyAlerts_SentAtUtc",
                table: "PayoutAnomalyAlerts",
                column: "SentAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PayoutAnomalyAlerts");
        }
    }
}
