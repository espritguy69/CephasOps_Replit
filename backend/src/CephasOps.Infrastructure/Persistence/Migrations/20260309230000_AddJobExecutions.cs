using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CephasOps.Infrastructure.Persistence.Migrations;

/// <summary>
/// Phase 3 Job Orchestration: JobExecutions table for worker-driven job dispatch.
/// </summary>
public partial class AddJobExecutions : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "JobExecutions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                JobType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                PayloadJson = table.Column<string>(type: "jsonb", nullable: false),
                Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                AttemptCount = table.Column<int>(type: "integer", nullable: false),
                MaxAttempts = table.Column<int>(type: "integer", nullable: false),
                NextRunAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                LastError = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                LastErrorAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CompanyId = table.Column<Guid>(type: "uuid", nullable: true),
                CorrelationId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                CausationId = table.Column<Guid>(type: "uuid", nullable: true),
                ProcessingNodeId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                ProcessingLeaseExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                ClaimedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                Priority = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_JobExecutions", x => x.Id));

        migrationBuilder.CreateIndex(
            name: "IX_JobExecutions_Status_NextRunAtUtc",
            table: "JobExecutions",
            columns: new[] { "Status", "NextRunAtUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_JobExecutions_CompanyId_Status",
            table: "JobExecutions",
            columns: new[] { "CompanyId", "Status" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "JobExecutions");
    }
}
