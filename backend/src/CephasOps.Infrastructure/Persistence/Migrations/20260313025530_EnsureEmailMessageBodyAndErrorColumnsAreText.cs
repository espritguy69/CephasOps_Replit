using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CephasOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EnsureEmailMessageBodyAndErrorColumnsAreText : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Ensure all email body and error columns use PostgreSQL text to avoid
            // "value too long for type character varying(2000)" when storing full payloads.
            // Idempotent: altering to text is safe when column is already text or varchar(n).
            migrationBuilder.AlterColumn<string>(
                name: "BodyText",
                table: "EmailMessages",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "BodyHtml",
                table: "EmailMessages",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "BodyPreview",
                table: "EmailMessages",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ParserError",
                table: "EmailMessages",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert only BodyPreview and ParserError to varchar(2000).
            // BodyText and BodyHtml remain text (they were added as text by AddFullEmailBodyToEmailMessages).
            migrationBuilder.AlterColumn<string>(
                name: "BodyPreview",
                table: "EmailMessages",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ParserError",
                table: "EmailMessages",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);
        }
    }
}
