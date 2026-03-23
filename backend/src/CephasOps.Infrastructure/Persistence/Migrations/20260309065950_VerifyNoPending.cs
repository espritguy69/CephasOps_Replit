using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CephasOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class VerifyNoPending : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastClaimedAtUtc",
                table: "EventStore",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastClaimedBy",
                table: "EventStore",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastErrorType",
                table: "EventStore",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ProcessingLeaseExpiresAtUtc",
                table: "EventStore",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProcessingNodeId",
                table: "EventStore",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ProcessingStartedAtUtc",
                table: "EventStore",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EventStoreAttemptHistory",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: true),
                    HandlerName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    AttemptNumber = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FinishedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DurationMs = table.Column<int>(type: "integer", nullable: true),
                    ProcessingNodeId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ErrorType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    StackTraceSummary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    WasRetried = table.Column<bool>(type: "boolean", nullable: false),
                    WasDeadLettered = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventStoreAttemptHistory", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EventStoreAttemptHistory_EventId",
                table: "EventStoreAttemptHistory",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_EventStoreAttemptHistory_EventId_AttemptNumber",
                table: "EventStoreAttemptHistory",
                columns: new[] { "EventId", "AttemptNumber" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventStoreAttemptHistory");

            migrationBuilder.DropColumn(
                name: "LastClaimedAtUtc",
                table: "EventStore");

            migrationBuilder.DropColumn(
                name: "LastClaimedBy",
                table: "EventStore");

            migrationBuilder.DropColumn(
                name: "LastErrorType",
                table: "EventStore");

            migrationBuilder.DropColumn(
                name: "ProcessingLeaseExpiresAtUtc",
                table: "EventStore");

            migrationBuilder.DropColumn(
                name: "ProcessingNodeId",
                table: "EventStore");

            migrationBuilder.DropColumn(
                name: "ProcessingStartedAtUtc",
                table: "EventStore");
        }
    }
}
