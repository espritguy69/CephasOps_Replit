using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CephasOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBaseWorkRates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BaseWorkRates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: true),
                    RateGroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderCategoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    InstallationMethodId = table.Column<Guid>(type: "uuid", nullable: true),
                    OrderSubtypeId = table.Column<Guid>(type: "uuid", nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EffectiveTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BaseWorkRates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BaseWorkRates_RateGroups_RateGroupId",
                        column: x => x.RateGroupId,
                        principalTable: "RateGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BaseWorkRates_OrderCategories_OrderCategoryId",
                        column: x => x.OrderCategoryId,
                        principalTable: "OrderCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BaseWorkRates_InstallationMethods_InstallationMethodId",
                        column: x => x.InstallationMethodId,
                        principalTable: "InstallationMethods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BaseWorkRates_OrderTypes_OrderSubtypeId",
                        column: x => x.OrderSubtypeId,
                        principalTable: "OrderTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BaseWorkRates_RateGroupId",
                table: "BaseWorkRates",
                column: "RateGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_BaseWorkRates_OrderCategoryId",
                table: "BaseWorkRates",
                column: "OrderCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_BaseWorkRates_InstallationMethodId",
                table: "BaseWorkRates",
                column: "InstallationMethodId");

            migrationBuilder.CreateIndex(
                name: "IX_BaseWorkRates_OrderSubtypeId",
                table: "BaseWorkRates",
                column: "OrderSubtypeId");

            migrationBuilder.CreateIndex(
                name: "IX_BaseWorkRates_CompanyId_RateGroupId_IsActive",
                table: "BaseWorkRates",
                columns: new[] { "CompanyId", "RateGroupId", "IsActive" },
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_BaseWorkRates_Lookup",
                table: "BaseWorkRates",
                columns: new[] { "RateGroupId", "OrderCategoryId", "InstallationMethodId", "OrderSubtypeId" },
                filter: "\"IsDeleted\" = false AND \"IsActive\" = true");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "BaseWorkRates");
        }
    }
}
