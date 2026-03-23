using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CephasOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderRelocationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NewLocationNote",
                table: "Orders",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OldLocationNote",
                table: "Orders",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RelocationType",
                table: "Orders",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NewLocationNote",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "OldLocationNote",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "RelocationType",
                table: "Orders");
        }
    }
}
