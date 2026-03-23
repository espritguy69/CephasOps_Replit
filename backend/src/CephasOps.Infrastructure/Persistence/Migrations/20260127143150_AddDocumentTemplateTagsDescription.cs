using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CephasOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentTemplateTagsDescription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "DocumentTemplates",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Tags",
                table: "DocumentTemplates",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.Sql(@"
DO $$
DECLARE
    rec RECORD;
    metadata jsonb;
    tags_list text;
BEGIN
    FOR rec IN
        SELECT ""Id"", ""JsonSchema""
        FROM ""DocumentTemplates""
        WHERE ""JsonSchema"" IS NOT NULL
    LOOP
        BEGIN
            metadata := NULL;
            IF (rec.""JsonSchema""::jsonb ? 'metadata') THEN
                metadata := rec.""JsonSchema""::jsonb -> 'metadata';
            ELSIF (rec.""JsonSchema""::jsonb ? 'tags') OR (rec.""JsonSchema""::jsonb ? 'description') THEN
                metadata := rec.""JsonSchema""::jsonb;
            END IF;

            IF metadata IS NOT NULL THEN
                IF (metadata ? 'tags') THEN
                    SELECT string_agg(value, ',')
                    INTO tags_list
                    FROM jsonb_array_elements_text(metadata -> 'tags');
                ELSE
                    tags_list := NULL;
                END IF;

                UPDATE ""DocumentTemplates""
                SET
                    ""Description"" = COALESCE(""Description"", metadata ->> 'description'),
                    ""Tags"" = COALESCE(""Tags"", tags_list)
                WHERE ""Id"" = rec.""Id"";
            END IF;
        EXCEPTION WHEN others THEN
            -- Skip rows with invalid JSON
            CONTINUE;
        END;
    END LOOP;
END $$;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "DocumentTemplates");

            migrationBuilder.DropColumn(
                name: "Tags",
                table: "DocumentTemplates");
        }
    }
}
