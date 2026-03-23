using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CephasOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderDepartment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DepartmentId",
                table: "Orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_DepartmentId",
                table: "Orders",
                column: "DepartmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Departments_DepartmentId",
                table: "Orders",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(
                @"UPDATE ""Orders"" o
                  SET ""DepartmentId"" = b.""DepartmentId""
                  FROM ""Buildings"" b
                  WHERE o.""BuildingId"" = b.""Id"" AND b.""DepartmentId"" IS NOT NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Departments_DepartmentId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_DepartmentId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "Orders");
        }
    }
}
