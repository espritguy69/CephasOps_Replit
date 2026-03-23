using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CephasOps.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Adds a unique partial index so only one active workflow can exist per scope
    /// (CompanyId, EntityType, PartnerId, DepartmentId, OrderTypeCode).
    /// Nulls are normalized to empty string for the index so general workflows (all null) are unique.
    /// </summary>
    public partial class AddUniqueActiveScopeIndexWorkflowDefinitions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE UNIQUE INDEX ""IX_WorkflowDefinitions_ActiveScope""
ON ""WorkflowDefinitions"" (
    ""CompanyId"",
    ""EntityType"",
    COALESCE(""PartnerId""::text, ''),
    COALESCE(""DepartmentId""::text, ''),
    COALESCE(""OrderTypeCode"", '')
)
WHERE ""IsActive"" = true;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_WorkflowDefinitions_ActiveScope"";");
        }
    }
}
