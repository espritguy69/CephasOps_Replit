using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CephasOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReplayOperations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS ""ReplayOperations"" (
                    ""Id"" uuid NOT NULL,
                    ""RequestedByUserId"" uuid NULL,
                    ""RequestedAtUtc"" timestamp with time zone NOT NULL,
                    ""DryRun"" boolean NOT NULL,
                    ""ReplayReason"" character varying(2000) NULL,
                    ""CompanyId"" uuid NULL,
                    ""EventType"" character varying(200) NULL,
                    ""Status"" character varying(50) NULL,
                    ""FromOccurredAtUtc"" timestamp with time zone NULL,
                    ""ToOccurredAtUtc"" timestamp with time zone NULL,
                    ""EntityType"" character varying(200) NULL,
                    ""EntityId"" uuid NULL,
                    ""CorrelationId"" character varying(200) NULL,
                    ""MaxEvents"" integer NULL,
                    ""TotalMatched"" integer NULL,
                    ""TotalEligible"" integer NULL,
                    ""TotalExecuted"" integer NULL,
                    ""TotalSucceeded"" integer NULL,
                    ""TotalFailed"" integer NULL,
                    ""ReplayCorrelationId"" character varying(200) NULL,
                    ""Notes"" character varying(4000) NULL,
                    ""State"" character varying(50) NULL,
                    ""CompletedAtUtc"" timestamp with time zone NULL,
                    CONSTRAINT ""PK_ReplayOperations"" PRIMARY KEY (""Id"")
                );
                CREATE INDEX IF NOT EXISTS ""IX_ReplayOperations_RequestedAtUtc"" ON ""ReplayOperations"" (""RequestedAtUtc"");
                CREATE INDEX IF NOT EXISTS ""IX_ReplayOperations_CompanyId_RequestedAtUtc"" ON ""ReplayOperations"" (""CompanyId"", ""RequestedAtUtc"");
                CREATE INDEX IF NOT EXISTS ""IX_ReplayOperations_RequestedByUserId"" ON ""ReplayOperations"" (""RequestedByUserId"");

                CREATE TABLE IF NOT EXISTS ""ReplayOperationEvents"" (
                    ""Id"" uuid NOT NULL,
                    ""ReplayOperationId"" uuid NOT NULL,
                    ""EventId"" uuid NOT NULL,
                    ""Succeeded"" boolean NOT NULL,
                    ""ErrorMessage"" character varying(2000) NULL,
                    ""ProcessedAtUtc"" timestamp with time zone NOT NULL,
                    CONSTRAINT ""PK_ReplayOperationEvents"" PRIMARY KEY (""Id"")
                );
                CREATE INDEX IF NOT EXISTS ""IX_ReplayOperationEvents_ReplayOperationId"" ON ""ReplayOperationEvents"" (""ReplayOperationId"");
                CREATE INDEX IF NOT EXISTS ""IX_ReplayOperationEvents_EventId"" ON ""ReplayOperationEvents"" (""EventId"");
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReplayOperationEvents");
            migrationBuilder.DropTable(
                name: "ReplayOperations");
        }
    }
}
