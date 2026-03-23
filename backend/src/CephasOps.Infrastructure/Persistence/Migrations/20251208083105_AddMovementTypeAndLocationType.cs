using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CephasOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMovementTypeAndLocationType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "MovementTypeId",
                table: "StockMovements",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LocationTypeId",
                table: "StockLocations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "WarehouseId",
                table: "StockLocations",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LocationTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    RequiresServiceInstallerId = table.Column<bool>(type: "boolean", nullable: false),
                    RequiresBuildingId = table.Column<bool>(type: "boolean", nullable: false),
                    RequiresWarehouseId = table.Column<bool>(type: "boolean", nullable: false),
                    AutoCreate = table.Column<bool>(type: "boolean", nullable: false),
                    AutoCreateTrigger = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_LocationTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MovementTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Direction = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    RequiresFromLocation = table.Column<bool>(type: "boolean", nullable: false),
                    RequiresToLocation = table.Column<bool>(type: "boolean", nullable: false),
                    RequiresOrderId = table.Column<bool>(type: "boolean", nullable: false),
                    RequiresServiceInstallerId = table.Column<bool>(type: "boolean", nullable: false),
                    RequiresPartnerId = table.Column<bool>(type: "boolean", nullable: false),
                    AffectsStockBalance = table.Column<bool>(type: "boolean", nullable: false),
                    StockImpact = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_MovementTypes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_MovementTypeId",
                table: "StockMovements",
                column: "MovementTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_StockLocations_CompanyId_LocationTypeId",
                table: "StockLocations",
                columns: new[] { "CompanyId", "LocationTypeId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockLocations_LocationTypeId",
                table: "StockLocations",
                column: "LocationTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_StockLocations_WarehouseId",
                table: "StockLocations",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_LocationTypes_CompanyId_Code",
                table: "LocationTypes",
                columns: new[] { "CompanyId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LocationTypes_CompanyId_IsActive",
                table: "LocationTypes",
                columns: new[] { "CompanyId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_MovementTypes_CompanyId_Code",
                table: "MovementTypes",
                columns: new[] { "CompanyId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MovementTypes_CompanyId_IsActive",
                table: "MovementTypes",
                columns: new[] { "CompanyId", "IsActive" });

            migrationBuilder.AddForeignKey(
                name: "FK_StockLocations_LocationTypes_LocationTypeId",
                table: "StockLocations",
                column: "LocationTypeId",
                principalTable: "LocationTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_StockMovements_MovementTypes_MovementTypeId",
                table: "StockMovements",
                column: "MovementTypeId",
                principalTable: "MovementTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StockLocations_LocationTypes_LocationTypeId",
                table: "StockLocations");

            migrationBuilder.DropForeignKey(
                name: "FK_StockMovements_MovementTypes_MovementTypeId",
                table: "StockMovements");

            migrationBuilder.DropTable(
                name: "LocationTypes");

            migrationBuilder.DropTable(
                name: "MovementTypes");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_MovementTypeId",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_StockLocations_CompanyId_LocationTypeId",
                table: "StockLocations");

            migrationBuilder.DropIndex(
                name: "IX_StockLocations_LocationTypeId",
                table: "StockLocations");

            migrationBuilder.DropIndex(
                name: "IX_StockLocations_WarehouseId",
                table: "StockLocations");

            migrationBuilder.DropColumn(
                name: "MovementTypeId",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "LocationTypeId",
                table: "StockLocations");

            migrationBuilder.DropColumn(
                name: "WarehouseId",
                table: "StockLocations");
        }
    }
}
