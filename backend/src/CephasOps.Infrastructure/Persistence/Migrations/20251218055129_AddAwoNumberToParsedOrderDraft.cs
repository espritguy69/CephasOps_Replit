using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CephasOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAwoNumberToParsedOrderDraft : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AwoNumber",
                table: "ParsedOrderDrafts",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AwoNumber",
                table: "ParsedOrderDrafts");
        }
    }
}
