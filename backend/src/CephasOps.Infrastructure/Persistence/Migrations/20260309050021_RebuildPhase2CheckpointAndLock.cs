using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CephasOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RebuildPhase2CheckpointAndLock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BackgroundJobId",
                table: "RebuildOperations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CheckpointCount",
                table: "RebuildOperations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastCheckpointAtUtc",
                table: "RebuildOperations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LastProcessedEventId",
                table: "RebuildOperations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastProcessedOccurredAtUtc",
                table: "RebuildOperations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProcessedCountAtLastCheckpoint",
                table: "RebuildOperations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "RerunReason",
                table: "RebuildOperations",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ResumeRequired",
                table: "RebuildOperations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "RetriedFromOperationId",
                table: "RebuildOperations",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RebuildExecutionLocks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RebuildTargetId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ScopeKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    RebuildOperationId = table.Column<Guid>(type: "uuid", nullable: false),
                    AcquiredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReleasedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RebuildExecutionLocks", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RebuildOperations_BackgroundJobId",
                table: "RebuildOperations",
                column: "BackgroundJobId");

            migrationBuilder.CreateIndex(
                name: "IX_RebuildOperations_State_RequestedAtUtc",
                table: "RebuildOperations",
                columns: new[] { "State", "RequestedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_RebuildExecutionLocks_RebuildOperationId",
                table: "RebuildExecutionLocks",
                column: "RebuildOperationId");

            migrationBuilder.CreateIndex(
                name: "IX_RebuildExecutionLocks_RebuildTargetId_ScopeKey",
                table: "RebuildExecutionLocks",
                columns: new[] { "RebuildTargetId", "ScopeKey" },
                unique: true,
                filter: "\"ReleasedAtUtc\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RebuildExecutionLocks_ReleasedAtUtc",
                table: "RebuildExecutionLocks",
                column: "ReleasedAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RebuildExecutionLocks");

            migrationBuilder.DropIndex(
                name: "IX_RebuildOperations_BackgroundJobId",
                table: "RebuildOperations");

            migrationBuilder.DropIndex(
                name: "IX_RebuildOperations_State_RequestedAtUtc",
                table: "RebuildOperations");

            migrationBuilder.DropColumn(
                name: "BackgroundJobId",
                table: "RebuildOperations");

            migrationBuilder.DropColumn(
                name: "CheckpointCount",
                table: "RebuildOperations");

            migrationBuilder.DropColumn(
                name: "LastCheckpointAtUtc",
                table: "RebuildOperations");

            migrationBuilder.DropColumn(
                name: "LastProcessedEventId",
                table: "RebuildOperations");

            migrationBuilder.DropColumn(
                name: "LastProcessedOccurredAtUtc",
                table: "RebuildOperations");

            migrationBuilder.DropColumn(
                name: "ProcessedCountAtLastCheckpoint",
                table: "RebuildOperations");

            migrationBuilder.DropColumn(
                name: "RerunReason",
                table: "RebuildOperations");

            migrationBuilder.DropColumn(
                name: "ResumeRequired",
                table: "RebuildOperations");

            migrationBuilder.DropColumn(
                name: "RetriedFromOperationId",
                table: "RebuildOperations");
        }
    }
}
