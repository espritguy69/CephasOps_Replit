using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CephasOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAdditionalFieldsToServiceInstallers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "ServiceInstallers",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BankAccountNumber",
                table: "ServiceInstallers",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BankName",
                table: "ServiceInstallers",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmergencyContact",
                table: "ServiceInstallers",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IcNumber",
                table: "ServiceInstallers",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Address",
                table: "ServiceInstallers");

            migrationBuilder.DropColumn(
                name: "BankAccountNumber",
                table: "ServiceInstallers");

            migrationBuilder.DropColumn(
                name: "BankName",
                table: "ServiceInstallers");

            migrationBuilder.DropColumn(
                name: "EmergencyContact",
                table: "ServiceInstallers");

            migrationBuilder.DropColumn(
                name: "IcNumber",
                table: "ServiceInstallers");
        }
    }
}
