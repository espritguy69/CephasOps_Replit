using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CephasOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNetworkFieldsToParsedOrderDraft : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InternetGateway",
                table: "ParsedOrderDrafts",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InternetLanIp",
                table: "ParsedOrderDrafts",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InternetSubnetMask",
                table: "ParsedOrderDrafts",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InternetWanIp",
                table: "ParsedOrderDrafts",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Password",
                table: "ParsedOrderDrafts",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Username",
                table: "ParsedOrderDrafts",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InternetGateway",
                table: "ParsedOrderDrafts");

            migrationBuilder.DropColumn(
                name: "InternetLanIp",
                table: "ParsedOrderDrafts");

            migrationBuilder.DropColumn(
                name: "InternetSubnetMask",
                table: "ParsedOrderDrafts");

            migrationBuilder.DropColumn(
                name: "InternetWanIp",
                table: "ParsedOrderDrafts");

            migrationBuilder.DropColumn(
                name: "Password",
                table: "ParsedOrderDrafts");

            migrationBuilder.DropColumn(
                name: "Username",
                table: "ParsedOrderDrafts");
        }
    }
}
