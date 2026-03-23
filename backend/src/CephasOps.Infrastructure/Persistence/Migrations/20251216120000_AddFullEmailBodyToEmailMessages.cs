using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CephasOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFullEmailBodyToEmailMessages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BodyHtml",
                table: "EmailMessages",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BodyText",
                table: "EmailMessages",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BodyHtml",
                table: "EmailMessages");

            migrationBuilder.DropColumn(
                name: "BodyText",
                table: "EmailMessages");
        }
    }
}

