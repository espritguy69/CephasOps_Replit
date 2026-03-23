using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CephasOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStockByLocationSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StockByLocationSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    LocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    PeriodStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PeriodEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SnapshotType = table.Column<string>(type: "text", nullable: false),
                    QuantityOnHand = table.Column<decimal>(type: "numeric", nullable: false),
                    QuantityReserved = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockByLocationSnapshots", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StockByLocationSnapshots_CompanyId_DepartmentId_Period_Type",
                table: "StockByLocationSnapshots",
                columns: new[] { "CompanyId", "DepartmentId", "PeriodStart", "SnapshotType" });

            migrationBuilder.CreateIndex(
                name: "IX_StockByLocationSnapshots_CompanyId_MaterialId_LocationId_PeriodStart_Type",
                table: "StockByLocationSnapshots",
                columns: new[] { "CompanyId", "MaterialId", "LocationId", "PeriodStart", "SnapshotType" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StockByLocationSnapshots");
        }
    }
}
