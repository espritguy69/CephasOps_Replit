using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CephasOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReplayOperationPhase2CheckpointResumeRerun : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ResumeRequired",
                table: "ReplayOperations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastCheckpointAtUtc",
                table: "ReplayOperations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LastProcessedEventId",
                table: "ReplayOperations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastProcessedOccurredAtUtc",
                table: "ReplayOperations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CheckpointCount",
                table: "ReplayOperations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ProcessedCountAtLastCheckpoint",
                table: "ReplayOperations",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrderingStrategyId",
                table: "ReplayOperations",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RetriedFromOperationId",
                table: "ReplayOperations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RerunReason",
                table: "ReplayOperations",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReplayOperations_RetriedFromOperationId",
                table: "ReplayOperations",
                column: "RetriedFromOperationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ReplayOperations_RetriedFromOperationId",
                table: "ReplayOperations");

            migrationBuilder.DropColumn(name: "ResumeRequired", table: "ReplayOperations");
            migrationBuilder.DropColumn(name: "LastCheckpointAtUtc", table: "ReplayOperations");
            migrationBuilder.DropColumn(name: "LastProcessedEventId", table: "ReplayOperations");
            migrationBuilder.DropColumn(name: "LastProcessedOccurredAtUtc", table: "ReplayOperations");
            migrationBuilder.DropColumn(name: "CheckpointCount", table: "ReplayOperations");
            migrationBuilder.DropColumn(name: "ProcessedCountAtLastCheckpoint", table: "ReplayOperations");
            migrationBuilder.DropColumn(name: "OrderingStrategyId", table: "ReplayOperations");
            migrationBuilder.DropColumn(name: "RetriedFromOperationId", table: "ReplayOperations");
            migrationBuilder.DropColumn(name: "RerunReason", table: "ReplayOperations");
        }
    }
}
