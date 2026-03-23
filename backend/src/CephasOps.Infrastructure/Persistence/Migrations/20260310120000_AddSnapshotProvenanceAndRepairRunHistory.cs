using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CephasOps.Infrastructure.Persistence.Migrations;

/// <summary>
/// Adds snapshot provenance (NormalFlow / RepairJob / Unknown etc.) and PayoutSnapshotRepairRuns table for repair run history.
/// Existing OrderPayoutSnapshots get Provenance = 'Unknown'.
/// </summary>
public partial class AddSnapshotProvenanceAndRepairRunHistory : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Provenance",
            table: "OrderPayoutSnapshots",
            type: "character varying(32)",
            maxLength: 32,
            nullable: false,
            defaultValue: "Unknown");

        migrationBuilder.CreateTable(
            name: "PayoutSnapshotRepairRuns",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                TotalProcessed = table.Column<int>(type: "integer", nullable: false),
                CreatedCount = table.Column<int>(type: "integer", nullable: false),
                SkippedCount = table.Column<int>(type: "integer", nullable: false),
                ErrorCount = table.Column<int>(type: "integer", nullable: false),
                ErrorOrderIdsJson = table.Column<string>(type: "text", nullable: true),
                TriggerSource = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PayoutSnapshotRepairRuns", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_PayoutSnapshotRepairRuns_StartedAt",
            table: "PayoutSnapshotRepairRuns",
            column: "StartedAt",
            descending: new[] { true });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "PayoutSnapshotRepairRuns");
        migrationBuilder.DropColumn(name: "Provenance", table: "OrderPayoutSnapshots");
    }
}
