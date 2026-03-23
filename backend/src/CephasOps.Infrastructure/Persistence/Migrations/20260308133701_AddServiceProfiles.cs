using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CephasOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ServiceProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_ServiceProfiles", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceProfiles_CompanyId_Code",
                table: "ServiceProfiles",
                columns: new[] { "CompanyId", "Code" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceProfiles_CompanyId_IsActive",
                table: "ServiceProfiles",
                columns: new[] { "CompanyId", "IsActive" },
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateTable(
                name: "OrderCategoryServiceProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderCategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceProfileId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_OrderCategoryServiceProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderCategoryServiceProfiles_OrderCategories_OrderCategoryId",
                        column: x => x.OrderCategoryId,
                        principalTable: "OrderCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderCategoryServiceProfiles_ServiceProfiles_ServiceProfileId",
                        column: x => x.ServiceProfileId,
                        principalTable: "ServiceProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderCategoryServiceProfiles_CompanyId_OrderCategoryId",
                table: "OrderCategoryServiceProfiles",
                columns: new[] { "CompanyId", "OrderCategoryId" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_OrderCategoryServiceProfiles_OrderCategoryId",
                table: "OrderCategoryServiceProfiles",
                column: "OrderCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderCategoryServiceProfiles_ServiceProfileId",
                table: "OrderCategoryServiceProfiles",
                column: "ServiceProfileId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "OrderCategoryServiceProfiles");
            migrationBuilder.DropTable(name: "ServiceProfiles");
        }
    }
}
