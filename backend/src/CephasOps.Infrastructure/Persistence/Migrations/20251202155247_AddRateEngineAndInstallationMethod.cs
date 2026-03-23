using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CephasOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRateEngineAndInstallationMethod : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Orders_OrderTypeId",
                table: "Orders");

            migrationBuilder.AddColumn<Guid>(
                name: "InstallationMethodId",
                table: "Orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CustomRates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    VerticalId = table.Column<Guid>(type: "uuid", nullable: true),
                    Dimension1 = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Dimension2 = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Dimension3 = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Dimension4 = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CustomRateAmount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    UnitOfMeasure = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "MYR"),
                    ValidFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ValidTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomRates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GponPartnerJobRates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PartnerGroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    PartnerId = table.Column<Guid>(type: "uuid", nullable: true),
                    OrderTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    InstallationTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    InstallationMethodId = table.Column<Guid>(type: "uuid", nullable: true),
                    RevenueAmount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "MYR"),
                    ValidFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ValidTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GponPartnerJobRates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GponSiCustomRates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceInstallerId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    InstallationTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    InstallationMethodId = table.Column<Guid>(type: "uuid", nullable: true),
                    PartnerGroupId = table.Column<Guid>(type: "uuid", nullable: true),
                    CustomPayoutAmount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "MYR"),
                    ValidFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ValidTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ApprovedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GponSiCustomRates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GponSiJobRates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    InstallationTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    InstallationMethodId = table.Column<Guid>(type: "uuid", nullable: true),
                    SiLevel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PartnerGroupId = table.Column<Guid>(type: "uuid", nullable: true),
                    PayoutAmount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "MYR"),
                    ValidFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ValidTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GponSiJobRates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RateCards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VerticalId = table.Column<Guid>(type: "uuid", nullable: true),
                    DepartmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    RateContext = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RateKind = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ValidFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ValidTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RateCards", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RateCardLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RateCardId = table.Column<Guid>(type: "uuid", nullable: false),
                    Dimension1 = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Dimension2 = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Dimension3 = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Dimension4 = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PartnerGroupId = table.Column<Guid>(type: "uuid", nullable: true),
                    PartnerId = table.Column<Guid>(type: "uuid", nullable: true),
                    RateAmount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    UnitOfMeasure = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "MYR"),
                    PayoutType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ExtraJson = table.Column<string>(type: "jsonb", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RateCardLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RateCardLines_RateCards_RateCardId",
                        column: x => x.RateCardId,
                        principalTable: "RateCards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_InstallationMethodId",
                table: "Orders",
                column: "InstallationMethodId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_OrderTypeId_InstallationTypeId_InstallationMethodId",
                table: "Orders",
                columns: new[] { "OrderTypeId", "InstallationTypeId", "InstallationMethodId" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomRates_UserId_DepartmentId_IsActive",
                table: "CustomRates",
                columns: new[] { "UserId", "DepartmentId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomRates_UserId_Dimension1_Dimension2_Dimension3_Dimensi~",
                table: "CustomRates",
                columns: new[] { "UserId", "Dimension1", "Dimension2", "Dimension3", "Dimension4" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomRates_ValidFrom_ValidTo",
                table: "CustomRates",
                columns: new[] { "ValidFrom", "ValidTo" });

            migrationBuilder.CreateIndex(
                name: "IX_GponPartnerJobRates_CompanyId_IsActive",
                table: "GponPartnerJobRates",
                columns: new[] { "CompanyId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_GponPartnerJobRates_PartnerGroupId_OrderTypeId_Installation~",
                table: "GponPartnerJobRates",
                columns: new[] { "PartnerGroupId", "OrderTypeId", "InstallationTypeId", "InstallationMethodId" });

            migrationBuilder.CreateIndex(
                name: "IX_GponPartnerJobRates_PartnerId_OrderTypeId_InstallationTypeI~",
                table: "GponPartnerJobRates",
                columns: new[] { "PartnerId", "OrderTypeId", "InstallationTypeId", "InstallationMethodId" });

            migrationBuilder.CreateIndex(
                name: "IX_GponPartnerJobRates_ValidFrom_ValidTo",
                table: "GponPartnerJobRates",
                columns: new[] { "ValidFrom", "ValidTo" });

            migrationBuilder.CreateIndex(
                name: "IX_GponSiCustomRates_ApprovedByUserId",
                table: "GponSiCustomRates",
                column: "ApprovedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_GponSiCustomRates_CompanyId_IsActive",
                table: "GponSiCustomRates",
                columns: new[] { "CompanyId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_GponSiCustomRates_ServiceInstallerId_OrderTypeId_Installati~",
                table: "GponSiCustomRates",
                columns: new[] { "ServiceInstallerId", "OrderTypeId", "InstallationTypeId", "InstallationMethodId" });

            migrationBuilder.CreateIndex(
                name: "IX_GponSiCustomRates_ServiceInstallerId_PartnerGroupId",
                table: "GponSiCustomRates",
                columns: new[] { "ServiceInstallerId", "PartnerGroupId" });

            migrationBuilder.CreateIndex(
                name: "IX_GponSiCustomRates_ValidFrom_ValidTo",
                table: "GponSiCustomRates",
                columns: new[] { "ValidFrom", "ValidTo" });

            migrationBuilder.CreateIndex(
                name: "IX_GponSiJobRates_CompanyId_IsActive",
                table: "GponSiJobRates",
                columns: new[] { "CompanyId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_GponSiJobRates_OrderTypeId_InstallationTypeId_InstallationM~",
                table: "GponSiJobRates",
                columns: new[] { "OrderTypeId", "InstallationTypeId", "InstallationMethodId", "SiLevel" });

            migrationBuilder.CreateIndex(
                name: "IX_GponSiJobRates_PartnerGroupId_OrderTypeId_InstallationTypeId",
                table: "GponSiJobRates",
                columns: new[] { "PartnerGroupId", "OrderTypeId", "InstallationTypeId" });

            migrationBuilder.CreateIndex(
                name: "IX_GponSiJobRates_ValidFrom_ValidTo",
                table: "GponSiJobRates",
                columns: new[] { "ValidFrom", "ValidTo" });

            migrationBuilder.CreateIndex(
                name: "IX_RateCardLines_RateCardId_Dimension1_Dimension2_Dimension3_D~",
                table: "RateCardLines",
                columns: new[] { "RateCardId", "Dimension1", "Dimension2", "Dimension3", "Dimension4" });

            migrationBuilder.CreateIndex(
                name: "IX_RateCardLines_RateCardId_IsActive",
                table: "RateCardLines",
                columns: new[] { "RateCardId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_RateCardLines_RateCardId_PartnerGroupId_PartnerId",
                table: "RateCardLines",
                columns: new[] { "RateCardId", "PartnerGroupId", "PartnerId" });

            migrationBuilder.CreateIndex(
                name: "IX_RateCards_CompanyId_RateContext_RateKind_IsActive",
                table: "RateCards",
                columns: new[] { "CompanyId", "RateContext", "RateKind", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_RateCards_CompanyId_VerticalId_DepartmentId",
                table: "RateCards",
                columns: new[] { "CompanyId", "VerticalId", "DepartmentId" });

            migrationBuilder.CreateIndex(
                name: "IX_RateCards_ValidFrom_ValidTo",
                table: "RateCards",
                columns: new[] { "ValidFrom", "ValidTo" });

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_InstallationMethods_InstallationMethodId",
                table: "Orders",
                column: "InstallationMethodId",
                principalTable: "InstallationMethods",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_InstallationMethods_InstallationMethodId",
                table: "Orders");

            migrationBuilder.DropTable(
                name: "CustomRates");

            migrationBuilder.DropTable(
                name: "GponPartnerJobRates");

            migrationBuilder.DropTable(
                name: "GponSiCustomRates");

            migrationBuilder.DropTable(
                name: "GponSiJobRates");

            migrationBuilder.DropTable(
                name: "RateCardLines");

            migrationBuilder.DropTable(
                name: "RateCards");

            migrationBuilder.DropIndex(
                name: "IX_Orders_InstallationMethodId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_OrderTypeId_InstallationTypeId_InstallationMethodId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "InstallationMethodId",
                table: "Orders");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_OrderTypeId",
                table: "Orders",
                column: "OrderTypeId");
        }
    }
}
