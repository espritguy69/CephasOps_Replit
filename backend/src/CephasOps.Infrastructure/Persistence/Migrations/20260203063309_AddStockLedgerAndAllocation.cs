using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CephasOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStockLedgerAndAllocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StockAllocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    SerialisedItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    LocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    LedgerEntryIdReserved = table.Column<Guid>(type: "uuid", nullable: true),
                    LedgerEntryIdIssued = table.Column<Guid>(type: "uuid", nullable: true),
                    LedgerEntryIdReturned = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockAllocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockAllocations_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StockAllocations_SerialisedItems_SerialisedItemId",
                        column: x => x.SerialisedItemId,
                        principalTable: "SerialisedItems",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StockAllocations_StockLocations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "StockLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StockLedgerEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EntryType = table.Column<int>(type: "integer", nullable: false),
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    LocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    FromLocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    ToLocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    SerialisedItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    AllocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReferenceType = table.Column<string>(type: "text", nullable: true),
                    ReferenceId = table.Column<string>(type: "text", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Remarks = table.Column<string>(type: "text", nullable: true),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockLedgerEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockLedgerEntries_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StockLedgerEntries_SerialisedItems_SerialisedItemId",
                        column: x => x.SerialisedItemId,
                        principalTable: "SerialisedItems",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StockLedgerEntries_StockAllocations_AllocationId",
                        column: x => x.AllocationId,
                        principalTable: "StockAllocations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StockLedgerEntries_StockLocations_FromLocationId",
                        column: x => x.FromLocationId,
                        principalTable: "StockLocations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StockLedgerEntries_StockLocations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "StockLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StockLedgerEntries_StockLocations_ToLocationId",
                        column: x => x.ToLocationId,
                        principalTable: "StockLocations",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_StockAllocations_LedgerEntryIdIssued",
                table: "StockAllocations",
                column: "LedgerEntryIdIssued");

            migrationBuilder.CreateIndex(
                name: "IX_StockAllocations_LedgerEntryIdReserved",
                table: "StockAllocations",
                column: "LedgerEntryIdReserved");

            migrationBuilder.CreateIndex(
                name: "IX_StockAllocations_LedgerEntryIdReturned",
                table: "StockAllocations",
                column: "LedgerEntryIdReturned");

            migrationBuilder.CreateIndex(
                name: "IX_StockAllocations_LocationId",
                table: "StockAllocations",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_StockAllocations_MaterialId",
                table: "StockAllocations",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_StockAllocations_SerialisedItemId",
                table: "StockAllocations",
                column: "SerialisedItemId");

            migrationBuilder.CreateIndex(
                name: "IX_StockLedgerEntries_AllocationId",
                table: "StockLedgerEntries",
                column: "AllocationId");

            migrationBuilder.CreateIndex(
                name: "IX_StockLedgerEntries_FromLocationId",
                table: "StockLedgerEntries",
                column: "FromLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_StockLedgerEntries_LocationId",
                table: "StockLedgerEntries",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_StockLedgerEntries_MaterialId",
                table: "StockLedgerEntries",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_StockLedgerEntries_SerialisedItemId",
                table: "StockLedgerEntries",
                column: "SerialisedItemId");

            migrationBuilder.CreateIndex(
                name: "IX_StockLedgerEntries_ToLocationId",
                table: "StockLedgerEntries",
                column: "ToLocationId");

            migrationBuilder.AddForeignKey(
                name: "FK_StockAllocations_StockLedgerEntries_LedgerEntryIdIssued",
                table: "StockAllocations",
                column: "LedgerEntryIdIssued",
                principalTable: "StockLedgerEntries",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StockAllocations_StockLedgerEntries_LedgerEntryIdReserved",
                table: "StockAllocations",
                column: "LedgerEntryIdReserved",
                principalTable: "StockLedgerEntries",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StockAllocations_StockLedgerEntries_LedgerEntryIdReturned",
                table: "StockAllocations",
                column: "LedgerEntryIdReturned",
                principalTable: "StockLedgerEntries",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StockAllocations_StockLedgerEntries_LedgerEntryIdIssued",
                table: "StockAllocations");

            migrationBuilder.DropForeignKey(
                name: "FK_StockAllocations_StockLedgerEntries_LedgerEntryIdReserved",
                table: "StockAllocations");

            migrationBuilder.DropForeignKey(
                name: "FK_StockAllocations_StockLedgerEntries_LedgerEntryIdReturned",
                table: "StockAllocations");

            migrationBuilder.DropTable(
                name: "StockLedgerEntries");

            migrationBuilder.DropTable(
                name: "StockAllocations");
        }
    }
}
