using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CephasOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPartnerGroupIdToBillingRatecard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ServiceCategory",
                table: "BillingRatecards",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "PartnerId",
                table: "BillingRatecards",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "PartnerGroupId",
                table: "BillingRatecards",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BillingRatecards_CompanyId_PartnerGroupId_OrderTypeId_Insta~",
                table: "BillingRatecards",
                columns: new[] { "CompanyId", "PartnerGroupId", "OrderTypeId", "InstallationMethodId" });

            migrationBuilder.CreateIndex(
                name: "IX_BillingRatecards_CompanyId_PartnerGroupId_PartnerId_OrderTy~",
                table: "BillingRatecards",
                columns: new[] { "CompanyId", "PartnerGroupId", "PartnerId", "OrderTypeId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BillingRatecards_CompanyId_PartnerGroupId_OrderTypeId_Insta~",
                table: "BillingRatecards");

            migrationBuilder.DropIndex(
                name: "IX_BillingRatecards_CompanyId_PartnerGroupId_PartnerId_OrderTy~",
                table: "BillingRatecards");

            migrationBuilder.DropColumn(
                name: "PartnerGroupId",
                table: "BillingRatecards");

            migrationBuilder.AlterColumn<string>(
                name: "ServiceCategory",
                table: "BillingRatecards",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "PartnerId",
                table: "BillingRatecards",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
