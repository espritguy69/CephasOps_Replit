using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CephasOps.Infrastructure.Persistence.Migrations;

/// <summary>
/// Adds PayoutAnomalyReviews table for payout anomaly governance (acknowledge, assign, resolve, comments). Operational metadata only.
/// </summary>
public partial class AddPayoutAnomalyReview : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "PayoutAnomalyReviews",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                AnomalyFingerprintId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                AnomalyType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                OrderId = table.Column<Guid>(type: "uuid", nullable: true),
                InstallerId = table.Column<Guid>(type: "uuid", nullable: true),
                PayoutSnapshotId = table.Column<Guid>(type: "uuid", nullable: true),
                Severity = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                DetectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                AssignedToUserId = table.Column<Guid>(type: "uuid", nullable: true),
                NotesJson = table.Column<string>(type: "text", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PayoutAnomalyReviews", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_PayoutAnomalyReviews_AnomalyFingerprintId",
            table: "PayoutAnomalyReviews",
            column: "AnomalyFingerprintId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_PayoutAnomalyReviews_Status",
            table: "PayoutAnomalyReviews",
            column: "Status");

        migrationBuilder.CreateIndex(
            name: "IX_PayoutAnomalyReviews_DetectedAt",
            table: "PayoutAnomalyReviews",
            column: "DetectedAt");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "PayoutAnomalyReviews");
    }
}
