using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CephasOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUnmatchedMaterialAuditToParsedOrderDraft : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UnmatchedMaterialCount",
                table: "ParsedOrderDrafts",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UnmatchedMaterialNamesJson",
                table: "ParsedOrderDrafts",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UnmatchedMaterialCount",
                table: "ParsedOrderDrafts");

            migrationBuilder.DropColumn(
                name: "UnmatchedMaterialNamesJson",
                table: "ParsedOrderDrafts");
        }
    }
}
