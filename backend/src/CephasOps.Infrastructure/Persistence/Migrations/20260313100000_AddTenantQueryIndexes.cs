using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CephasOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantQueryIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Orders_CompanyId_CreatedAt",
                table: "Orders",
                columns: new[] { "CompanyId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_JobExecutions_CompanyId_CreatedAtUtc",
                table: "JobExecutions",
                columns: new[] { "CompanyId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Files_CompanyId_CreatedAt",
                table: "Files",
                columns: new[] { "CompanyId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_CompanyId_IsActive",
                table: "Users",
                columns: new[] { "CompanyId", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_Orders_CompanyId_CreatedAt", table: "Orders");
            migrationBuilder.DropIndex(name: "IX_JobExecutions_CompanyId_CreatedAtUtc", table: "JobExecutions");
            migrationBuilder.DropIndex(name: "IX_Files_CompanyId_CreatedAt", table: "Files");
            migrationBuilder.DropIndex(name: "IX_Users_CompanyId_IsActive", table: "Users");
        }
    }
}
