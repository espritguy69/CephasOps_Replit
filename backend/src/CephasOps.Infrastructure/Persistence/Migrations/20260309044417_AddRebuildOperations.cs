using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CephasOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRebuildOperations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RebuildOperations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RebuildTargetId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    RequestedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ScopeCompanyId = table.Column<Guid>(type: "uuid", nullable: true),
                    FromOccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ToOccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DryRun = table.Column<bool>(type: "boolean", nullable: false),
                    State = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DurationMs = table.Column<long>(type: "bigint", nullable: true),
                    RowsDeleted = table.Column<int>(type: "integer", nullable: false),
                    RowsInserted = table.Column<int>(type: "integer", nullable: false),
                    RowsUpdated = table.Column<int>(type: "integer", nullable: false),
                    SourceRecordCount = table.Column<int>(type: "integer", nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RebuildOperations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RebuildOperations_RebuildTargetId",
                table: "RebuildOperations",
                column: "RebuildTargetId");

            migrationBuilder.CreateIndex(
                name: "IX_RebuildOperations_RequestedAtUtc",
                table: "RebuildOperations",
                column: "RequestedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_RebuildOperations_ScopeCompanyId_RequestedAtUtc",
                table: "RebuildOperations",
                columns: new[] { "ScopeCompanyId", "RequestedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RebuildOperations");
        }
    }
}
