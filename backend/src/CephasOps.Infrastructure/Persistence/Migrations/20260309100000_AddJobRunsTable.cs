using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CephasOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddJobRunsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "JobRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: true),
                    JobName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    JobType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TriggerSource = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    QueueOrChannel = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PayloadSummary = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DurationMs = table.Column<long>(type: "bigint", nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    WorkerNode = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ErrorCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ErrorDetails = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    InitiatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ParentJobRunId = table.Column<Guid>(type: "uuid", nullable: true),
                    RelatedEntityType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    RelatedEntityId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    BackgroundJobId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobRuns", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JobRuns_StartedAtUtc",
                table: "JobRuns",
                column: "StartedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_JobRuns_Status_StartedAtUtc",
                table: "JobRuns",
                columns: new[] { "Status", "StartedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_JobRuns_JobType_StartedAtUtc",
                table: "JobRuns",
                columns: new[] { "JobType", "StartedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_JobRuns_CompanyId_StartedAtUtc",
                table: "JobRuns",
                columns: new[] { "CompanyId", "StartedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_JobRuns_BackgroundJobId",
                table: "JobRuns",
                column: "BackgroundJobId",
                filter: "\"BackgroundJobId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_JobRuns_CorrelationId",
                table: "JobRuns",
                column: "CorrelationId",
                filter: "\"CorrelationId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JobRuns");
        }
    }
}
