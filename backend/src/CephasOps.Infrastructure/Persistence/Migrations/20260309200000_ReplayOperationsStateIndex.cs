using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CephasOps.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Adds a composite index on ReplayOperations (CompanyId, State, RequestedAtUtc) to support
    /// state-driven list/history queries (active, failed, pending filtering) and operational diagnostics.
    /// Performance-only; no replay or API behavior change.
    /// </summary>
    public partial class ReplayOperationsStateIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ReplayOperations_CompanyId_State_RequestedAtUtc",
                table: "ReplayOperations",
                columns: new[] { "CompanyId", "State", "RequestedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ReplayOperations_CompanyId_State_RequestedAtUtc",
                table: "ReplayOperations");
        }
    }
}
