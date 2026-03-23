using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CephasOps.Infrastructure.Persistence.Migrations;

/// <summary>
/// Adds OrderPayoutSnapshots table for immutable payout calculation audit trail.
/// </summary>
public partial class AddOrderPayoutSnapshot : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "OrderPayoutSnapshots",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                CompanyId = table.Column<Guid>(type: "uuid", nullable: true),
                InstallerId = table.Column<Guid>(type: "uuid", nullable: true),
                RateGroupId = table.Column<Guid>(type: "uuid", nullable: true),
                BaseWorkRateId = table.Column<Guid>(type: "uuid", nullable: true),
                ServiceProfileId = table.Column<Guid>(type: "uuid", nullable: true),
                CustomRateId = table.Column<Guid>(type: "uuid", nullable: true),
                LegacyRateId = table.Column<Guid>(type: "uuid", nullable: true),
                BaseAmount = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                ModifierTraceJson = table.Column<string>(type: "text", nullable: true),
                FinalPayout = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                ResolutionMatchLevel = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                PayoutPath = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                ResolutionTraceJson = table.Column<string>(type: "text", nullable: true),
                CalculatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_OrderPayoutSnapshots", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_OrderPayoutSnapshots_OrderId",
            table: "OrderPayoutSnapshots",
            column: "OrderId",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "OrderPayoutSnapshots");
    }
}
