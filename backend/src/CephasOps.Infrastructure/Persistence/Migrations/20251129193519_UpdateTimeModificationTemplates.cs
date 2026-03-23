using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CephasOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTimeModificationTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE "ParserTemplates"
                SET "SubjectPattern" = 'Modification-Outdoor',
                    "Priority" = GREATEST("Priority", 130),
                    "ExpectedAttachmentTypes" = 'xls,xlsx'
                WHERE "Code" = 'TIME_MOD_OUTDOOR';
                """);

            migrationBuilder.Sql("""
                UPDATE "ParserTemplates"
                SET "Priority" = GREATEST("Priority", 120),
                    "ExpectedAttachmentTypes" = COALESCE(NULLIF("ExpectedAttachmentTypes", ''), 'xls,xlsx')
                WHERE "Code" = 'TIME_MOD_INDOOR';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE "ParserTemplates"
                SET "SubjectPattern" = 'Modification Outdoor',
                    "Priority" = 95,
                    "ExpectedAttachmentTypes" = NULL
                WHERE "Code" = 'TIME_MOD_OUTDOOR';
                """);

            migrationBuilder.Sql("""
                UPDATE "ParserTemplates"
                SET "Priority" = 96,
                    "ExpectedAttachmentTypes" = NULL
                WHERE "Code" = 'TIME_MOD_INDOOR';
                """);
        }
    }
}
