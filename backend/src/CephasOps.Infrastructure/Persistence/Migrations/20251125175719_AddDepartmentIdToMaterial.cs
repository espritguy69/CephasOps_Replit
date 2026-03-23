using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CephasOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDepartmentIdToMaterial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DepartmentId",
                table: "Materials",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Materials_DepartmentId",
                table: "Materials",
                column: "DepartmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Materials_Departments_DepartmentId",
                table: "Materials",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Materials_Departments_DepartmentId",
                table: "Materials");

            migrationBuilder.DropIndex(
                name: "IX_Materials_DepartmentId",
                table: "Materials");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "Materials");
        }
    }
}

