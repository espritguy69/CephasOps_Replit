using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CephasOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRateGroupAndOrderTypeSubtypeRateGroup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RateGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RateGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OrderTypeSubtypeRateGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderSubtypeId = table.Column<Guid>(type: "uuid", nullable: true),
                    RateGroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderTypeSubtypeRateGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderTypeSubtypeRateGroups_OrderTypes_OrderTypeId",
                        column: x => x.OrderTypeId,
                        principalTable: "OrderTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderTypeSubtypeRateGroups_OrderTypes_OrderSubtypeId",
                        column: x => x.OrderSubtypeId,
                        principalTable: "OrderTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderTypeSubtypeRateGroups_RateGroups_RateGroupId",
                        column: x => x.RateGroupId,
                        principalTable: "RateGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderTypeSubtypeRateGroups_OrderSubtypeId",
                table: "OrderTypeSubtypeRateGroups",
                column: "OrderSubtypeId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderTypeSubtypeRateGroups_RateGroupId",
                table: "OrderTypeSubtypeRateGroups",
                column: "RateGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderTypeSubtypeRateGroups_Company_Type_Subtype",
                table: "OrderTypeSubtypeRateGroups",
                columns: new[] { "CompanyId", "OrderTypeId", "OrderSubtypeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RateGroups_CompanyId_Code",
                table: "RateGroups",
                columns: new[] { "CompanyId", "Code" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_RateGroups_CompanyId_IsActive",
                table: "RateGroups",
                columns: new[] { "CompanyId", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderTypeSubtypeRateGroups");

            migrationBuilder.DropTable(
                name: "RateGroups");
        }
    }
}
