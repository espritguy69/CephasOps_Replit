using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CephasOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceProfileIdToBaseWorkRate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ServiceProfileId",
                table: "BaseWorkRates",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BaseWorkRates_ServiceProfileId",
                table: "BaseWorkRates",
                column: "ServiceProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_BaseWorkRates_ServiceProfileLookup",
                table: "BaseWorkRates",
                columns: new[] { "RateGroupId", "ServiceProfileId", "InstallationMethodId", "OrderSubtypeId" },
                filter: "\"IsDeleted\" = false AND \"IsActive\" = true");

            migrationBuilder.AddForeignKey(
                name: "FK_BaseWorkRates_ServiceProfiles_ServiceProfileId",
                table: "BaseWorkRates",
                column: "ServiceProfileId",
                principalTable: "ServiceProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BaseWorkRates_ServiceProfiles_ServiceProfileId",
                table: "BaseWorkRates");

            migrationBuilder.DropIndex(
                name: "IX_BaseWorkRates_ServiceProfileId",
                table: "BaseWorkRates");

            migrationBuilder.DropIndex(
                name: "IX_BaseWorkRates_ServiceProfileLookup",
                table: "BaseWorkRates");

            migrationBuilder.DropColumn(
                name: "ServiceProfileId",
                table: "BaseWorkRates");
        }
    }
}
