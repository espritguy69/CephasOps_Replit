using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CephasOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMaterialTaggingSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "MaterialCategoryId",
                table: "Materials",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MaterialAttributes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Value = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    DataType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "String"),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_MaterialAttributes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaterialAttributes_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MaterialTags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Color = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_MaterialTags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MaterialVerticals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_MaterialVerticals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MaterialMaterialTags",
                columns: table => new
                {
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialTagId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialMaterialTags", x => new { x.MaterialId, x.MaterialTagId });
                    table.ForeignKey(
                        name: "FK_MaterialMaterialTags_MaterialTags_MaterialTagId",
                        column: x => x.MaterialTagId,
                        principalTable: "MaterialTags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MaterialMaterialTags_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MaterialMaterialVerticals",
                columns: table => new
                {
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialVerticalId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialMaterialVerticals", x => new { x.MaterialId, x.MaterialVerticalId });
                    table.ForeignKey(
                        name: "FK_MaterialMaterialVerticals_MaterialVerticals_MaterialVertica~",
                        column: x => x.MaterialVerticalId,
                        principalTable: "MaterialVerticals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MaterialMaterialVerticals_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Materials_MaterialCategoryId",
                table: "Materials",
                column: "MaterialCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialAttributes_MaterialId",
                table: "MaterialAttributes",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialAttributes_MaterialId_Key",
                table: "MaterialAttributes",
                columns: new[] { "MaterialId", "Key" });

            migrationBuilder.CreateIndex(
                name: "IX_MaterialMaterialTags_MaterialTagId",
                table: "MaterialMaterialTags",
                column: "MaterialTagId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialMaterialVerticals_MaterialVerticalId",
                table: "MaterialMaterialVerticals",
                column: "MaterialVerticalId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialTags_CompanyId_IsActive",
                table: "MaterialTags",
                columns: new[] { "CompanyId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_MaterialTags_CompanyId_Name",
                table: "MaterialTags",
                columns: new[] { "CompanyId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MaterialVerticals_CompanyId_Code",
                table: "MaterialVerticals",
                columns: new[] { "CompanyId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MaterialVerticals_CompanyId_IsActive",
                table: "MaterialVerticals",
                columns: new[] { "CompanyId", "IsActive" });

            migrationBuilder.AddForeignKey(
                name: "FK_Materials_MaterialCategories_MaterialCategoryId",
                table: "Materials",
                column: "MaterialCategoryId",
                principalTable: "MaterialCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Materials_MaterialCategories_MaterialCategoryId",
                table: "Materials");

            migrationBuilder.DropTable(
                name: "MaterialAttributes");

            migrationBuilder.DropTable(
                name: "MaterialMaterialTags");

            migrationBuilder.DropTable(
                name: "MaterialMaterialVerticals");

            migrationBuilder.DropTable(
                name: "MaterialTags");

            migrationBuilder.DropTable(
                name: "MaterialVerticals");

            migrationBuilder.DropIndex(
                name: "IX_Materials_MaterialCategoryId",
                table: "Materials");

            migrationBuilder.DropColumn(
                name: "MaterialCategoryId",
                table: "Materials");
        }
    }
}
