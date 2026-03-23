using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CephasOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMaterialUsageAndRMAFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_OrderMaterialUsage_MaterialId",
                table: "OrderMaterialUsage",
                column: "MaterialId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderMaterialUsage_Materials_MaterialId",
                table: "OrderMaterialUsage",
                column: "MaterialId",
                principalTable: "Materials",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderMaterialUsage_SerialisedItems_SerialisedItemId",
                table: "OrderMaterialUsage",
                column: "SerialisedItemId",
                principalTable: "SerialisedItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderMaterialUsage_Materials_MaterialId",
                table: "OrderMaterialUsage");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderMaterialUsage_SerialisedItems_SerialisedItemId",
                table: "OrderMaterialUsage");

            migrationBuilder.DropIndex(
                name: "IX_OrderMaterialUsage_MaterialId",
                table: "OrderMaterialUsage");
        }
    }
}
