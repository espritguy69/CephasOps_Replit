using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CephasOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderTypeParentOrderTypeId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ParentOrderTypeId",
                table: "OrderTypes",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderTypes_ParentOrderTypeId",
                table: "OrderTypes",
                column: "ParentOrderTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderTypes_OrderTypes_ParentOrderTypeId",
                table: "OrderTypes",
                column: "ParentOrderTypeId",
                principalTable: "OrderTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderTypes_OrderTypes_ParentOrderTypeId",
                table: "OrderTypes");

            migrationBuilder.DropIndex(
                name: "IX_OrderTypes_ParentOrderTypeId",
                table: "OrderTypes");

            migrationBuilder.DropColumn(
                name: "ParentOrderTypeId",
                table: "OrderTypes");
        }
    }
}
