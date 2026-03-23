using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CephasOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixBuildingRelationshipsQueryFilter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // BuildingDefaultMaterials table doesn't exist yet, so skip these operations
            // The configuration is set for when the table is created in the future
            
            // migrationBuilder.AlterColumn<string>(
            //     name: "Notes",
            //     table: "BuildingDefaultMaterials",
            //     type: "character varying(1000)",
            //     maxLength: 1000,
            //     nullable: true,
            //     oldClrType: typeof(string),
            //     oldType: "text",
            //     oldNullable: true);

            // migrationBuilder.AlterColumn<decimal>(
            //     name: "DefaultQuantity",
            //     table: "BuildingDefaultMaterials",
            //     type: "numeric(18,2)",
            //     precision: 18,
            //     scale: 2,
            //     nullable: false,
            //     oldClrType: typeof(decimal),
            //     oldType: "numeric");

            // migrationBuilder.CreateIndex(
            //     name: "IX_BuildingDefaultMaterials_BuildingId_IsActive",
            //     table: "BuildingDefaultMaterials",
            //     columns: new[] { "BuildingId", "IsActive" });

            // migrationBuilder.CreateIndex(
            //     name: "IX_BuildingDefaultMaterials_BuildingId_MaterialId",
            //     table: "BuildingDefaultMaterials",
            //     columns: new[] { "BuildingId", "MaterialId" });

            // migrationBuilder.CreateIndex(
            //     name: "IX_BuildingDefaultMaterials_BuildingId_OrderTypeId",
            //     table: "BuildingDefaultMaterials",
            //     columns: new[] { "BuildingId", "OrderTypeId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // BuildingDefaultMaterials table doesn't exist yet, so skip these operations
            // migrationBuilder.DropIndex(
            //     name: "IX_BuildingDefaultMaterials_BuildingId_IsActive",
            //     table: "BuildingDefaultMaterials");

            // migrationBuilder.DropIndex(
            //     name: "IX_BuildingDefaultMaterials_BuildingId_MaterialId",
            //     table: "BuildingDefaultMaterials");

            // migrationBuilder.DropIndex(
            //     name: "IX_BuildingDefaultMaterials_BuildingId_OrderTypeId",
            //     table: "BuildingDefaultMaterials");

            // migrationBuilder.AlterColumn<string>(
            //     name: "Notes",
            //     table: "BuildingDefaultMaterials",
            //     type: "text",
            //     nullable: true,
            //     oldClrType: typeof(string),
            //     oldType: "character varying(1000)",
            //     oldMaxLength: 1000,
            //     oldNullable: true);

            // migrationBuilder.AlterColumn<decimal>(
            //     name: "DefaultQuantity",
            //     table: "BuildingDefaultMaterials",
            //     type: "numeric",
            //     nullable: false,
            //     oldClrType: typeof(decimal),
            //     oldType: "numeric(18,2)",
            //     oldPrecision: 18,
            //     oldScale: 2);
        }
    }
}
