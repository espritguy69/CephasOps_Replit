using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CephasOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDepartmentIdToSkills : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Skills_CompanyId_Category_IsActive",
                table: "Skills");

            migrationBuilder.DropIndex(
                name: "IX_Skills_CompanyId_Code",
                table: "Skills");

            migrationBuilder.AddColumn<Guid>(
                name: "DepartmentId",
                table: "Skills",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Skills_CompanyId_DepartmentId_Category_IsActive",
                table: "Skills",
                columns: new[] { "CompanyId", "DepartmentId", "Category", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Skills_CompanyId_DepartmentId_Code",
                table: "Skills",
                columns: new[] { "CompanyId", "DepartmentId", "Code" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_Skills_DepartmentId",
                table: "Skills",
                column: "DepartmentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Skills_CompanyId_DepartmentId_Category_IsActive",
                table: "Skills");

            migrationBuilder.DropIndex(
                name: "IX_Skills_CompanyId_DepartmentId_Code",
                table: "Skills");

            migrationBuilder.DropIndex(
                name: "IX_Skills_DepartmentId",
                table: "Skills");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "Skills");

            migrationBuilder.CreateIndex(
                name: "IX_Skills_CompanyId_Category_IsActive",
                table: "Skills",
                columns: new[] { "CompanyId", "Category", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Skills_CompanyId_Code",
                table: "Skills",
                columns: new[] { "CompanyId", "Code" },
                unique: true,
                filter: "\"IsDeleted\" = false");
        }
    }
}
