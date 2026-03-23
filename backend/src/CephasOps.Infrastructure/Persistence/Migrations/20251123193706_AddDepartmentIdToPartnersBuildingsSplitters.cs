using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CephasOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDepartmentIdToPartnersBuildingsSplitters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Block",
                table: "Splitters",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DepartmentId",
                table: "Splitters",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Floor",
                table: "Splitters",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DepartmentId",
                table: "Partners",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DepartmentId",
                table: "Buildings",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Splitters_CompanyId_DepartmentId",
                table: "Splitters",
                columns: new[] { "CompanyId", "DepartmentId" });

            migrationBuilder.CreateIndex(
                name: "IX_Splitters_DepartmentId",
                table: "Splitters",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Partners_CompanyId_DepartmentId",
                table: "Partners",
                columns: new[] { "CompanyId", "DepartmentId" });

            migrationBuilder.CreateIndex(
                name: "IX_Partners_DepartmentId",
                table: "Partners",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Buildings_CompanyId_DepartmentId",
                table: "Buildings",
                columns: new[] { "CompanyId", "DepartmentId" });

            migrationBuilder.CreateIndex(
                name: "IX_Buildings_DepartmentId",
                table: "Buildings",
                column: "DepartmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Buildings_Departments_DepartmentId",
                table: "Buildings",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Partners_Departments_DepartmentId",
                table: "Partners",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Splitters_Departments_DepartmentId",
                table: "Splitters",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Buildings_Departments_DepartmentId",
                table: "Buildings");

            migrationBuilder.DropForeignKey(
                name: "FK_Partners_Departments_DepartmentId",
                table: "Partners");

            migrationBuilder.DropForeignKey(
                name: "FK_Splitters_Departments_DepartmentId",
                table: "Splitters");

            migrationBuilder.DropIndex(
                name: "IX_Splitters_CompanyId_DepartmentId",
                table: "Splitters");

            migrationBuilder.DropIndex(
                name: "IX_Splitters_DepartmentId",
                table: "Splitters");

            migrationBuilder.DropIndex(
                name: "IX_Partners_CompanyId_DepartmentId",
                table: "Partners");

            migrationBuilder.DropIndex(
                name: "IX_Partners_DepartmentId",
                table: "Partners");

            migrationBuilder.DropIndex(
                name: "IX_Buildings_CompanyId_DepartmentId",
                table: "Buildings");

            migrationBuilder.DropIndex(
                name: "IX_Buildings_DepartmentId",
                table: "Buildings");

            migrationBuilder.DropColumn(
                name: "Block",
                table: "Splitters");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "Splitters");

            migrationBuilder.DropColumn(
                name: "Floor",
                table: "Splitters");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "Partners");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "Buildings");
        }
    }
}
