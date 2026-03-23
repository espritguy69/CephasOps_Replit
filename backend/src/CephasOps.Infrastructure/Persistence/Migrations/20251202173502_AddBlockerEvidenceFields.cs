using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CephasOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBlockerEvidenceFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BlockerCategory",
                table: "OrderBlockers",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EvidenceAttachmentIds",
                table: "OrderBlockers",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EvidenceNotes",
                table: "OrderBlockers",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EvidenceRequired",
                table: "OrderBlockers",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderBlockers_BlockerCategory",
                table: "OrderBlockers",
                column: "BlockerCategory");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OrderBlockers_BlockerCategory",
                table: "OrderBlockers");

            migrationBuilder.DropColumn(
                name: "BlockerCategory",
                table: "OrderBlockers");

            migrationBuilder.DropColumn(
                name: "EvidenceAttachmentIds",
                table: "OrderBlockers");

            migrationBuilder.DropColumn(
                name: "EvidenceNotes",
                table: "OrderBlockers");

            migrationBuilder.DropColumn(
                name: "EvidenceRequired",
                table: "OrderBlockers");
        }
    }
}
