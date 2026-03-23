using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CephasOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEventLedgerEntries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LedgerEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceEventId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReplayOperationId = table.Column<Guid>(type: "uuid", nullable: true),
                    LedgerFamily = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Category = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: true),
                    EntityType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: true),
                    EventType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RecordedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PayloadSnapshot = table.Column<string>(type: "text", nullable: true),
                    CorrelationId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    TriggeredByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    OrderingStrategyId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LedgerEntries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LedgerEntries_CompanyId_LedgerFamily_OccurredAtUtc",
                table: "LedgerEntries",
                columns: new[] { "CompanyId", "LedgerFamily", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_LedgerEntries_EntityType_EntityId_LedgerFamily",
                table: "LedgerEntries",
                columns: new[] { "EntityType", "EntityId", "LedgerFamily" });

            migrationBuilder.CreateIndex(
                name: "IX_LedgerEntries_RecordedAtUtc",
                table: "LedgerEntries",
                column: "RecordedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_LedgerEntries_ReplayOperationId",
                table: "LedgerEntries",
                column: "ReplayOperationId",
                filter: "\"ReplayOperationId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LedgerEntries_ReplayOperationId_LedgerFamily",
                table: "LedgerEntries",
                columns: new[] { "ReplayOperationId", "LedgerFamily" },
                unique: true,
                filter: "\"ReplayOperationId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LedgerEntries_SourceEventId",
                table: "LedgerEntries",
                column: "SourceEventId",
                filter: "\"SourceEventId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LedgerEntries_SourceEventId_LedgerFamily",
                table: "LedgerEntries",
                columns: new[] { "SourceEventId", "LedgerFamily" },
                unique: true,
                filter: "\"SourceEventId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LedgerEntries");
        }
    }
}
