using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CephasOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReplayExecutionLock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReplayExecutionLock",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReplayOperationId = table.Column<Guid>(type: "uuid", nullable: false),
                    AcquiredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReleasedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReplayExecutionLock", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReplayExecutionLock_CompanyId",
                table: "ReplayExecutionLock",
                column: "CompanyId",
                unique: true,
                filter: "\"ReleasedAtUtc\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ReplayExecutionLock_ReleasedAtUtc",
                table: "ReplayExecutionLock",
                column: "ReleasedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ReplayExecutionLock_ReplayOperationId",
                table: "ReplayExecutionLock",
                column: "ReplayOperationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReplayExecutionLock");
        }
    }
}
