using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CephasOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLedgerReadPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StockLedgerEntries_SerialisedItemId",
                table: "StockLedgerEntries");

            migrationBuilder.DropIndex(
                name: "IX_StockAllocations_MaterialId",
                table: "StockAllocations");

            migrationBuilder.CreateIndex(
                name: "IX_StockLedgerEntries_CompanyId_IsDeleted_CreatedAt",
                table: "StockLedgerEntries",
                columns: new[] { "CompanyId", "IsDeleted", "CreatedAt" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "IX_StockLedgerEntries_CompanyId_IsDeleted_MaterialId_LocationId",
                table: "StockLedgerEntries",
                columns: new[] { "CompanyId", "IsDeleted", "MaterialId", "LocationId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockLedgerEntries_CompanyId_IsDeleted_OrderId",
                table: "StockLedgerEntries",
                columns: new[] { "CompanyId", "IsDeleted", "OrderId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockLedgerEntries_SerialisedItemId_CreatedAt",
                table: "StockLedgerEntries",
                columns: new[] { "SerialisedItemId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_StockAllocations_CompanyId_IsDeleted_Status",
                table: "StockAllocations",
                columns: new[] { "CompanyId", "IsDeleted", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_StockAllocations_MaterialId_LocationId_Status",
                table: "StockAllocations",
                columns: new[] { "MaterialId", "LocationId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StockLedgerEntries_CompanyId_IsDeleted_CreatedAt",
                table: "StockLedgerEntries");

            migrationBuilder.DropIndex(
                name: "IX_StockLedgerEntries_CompanyId_IsDeleted_MaterialId_LocationId",
                table: "StockLedgerEntries");

            migrationBuilder.DropIndex(
                name: "IX_StockLedgerEntries_CompanyId_IsDeleted_OrderId",
                table: "StockLedgerEntries");

            migrationBuilder.DropIndex(
                name: "IX_StockLedgerEntries_SerialisedItemId_CreatedAt",
                table: "StockLedgerEntries");

            migrationBuilder.DropIndex(
                name: "IX_StockAllocations_CompanyId_IsDeleted_Status",
                table: "StockAllocations");

            migrationBuilder.DropIndex(
                name: "IX_StockAllocations_MaterialId_LocationId_Status",
                table: "StockAllocations");

            migrationBuilder.CreateIndex(
                name: "IX_StockLedgerEntries_SerialisedItemId",
                table: "StockLedgerEntries",
                column: "SerialisedItemId");

            migrationBuilder.CreateIndex(
                name: "IX_StockAllocations_MaterialId",
                table: "StockAllocations",
                column: "MaterialId");
        }
    }
}
