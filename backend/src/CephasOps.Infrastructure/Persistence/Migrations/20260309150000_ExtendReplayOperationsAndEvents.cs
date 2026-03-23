using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CephasOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ExtendReplayOperationsAndEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReplayTarget",
                table: "ReplayOperations",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReplayMode",
                table: "ReplayOperations",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartedAtUtc",
                table: "ReplayOperations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DurationMs",
                table: "ReplayOperations",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SkippedCount",
                table: "ReplayOperations",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ErrorSummary",
                table: "ReplayOperations",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BackgroundJobId",
                table: "ReplayOperations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EventType",
                table: "ReplayOperationEvents",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EntityType",
                table: "ReplayOperationEvents",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "EntityId",
                table: "ReplayOperationEvents",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SkippedReason",
                table: "ReplayOperationEvents",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DurationMs",
                table: "ReplayOperationEvents",
                type: "bigint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "ReplayTarget", table: "ReplayOperations");
            migrationBuilder.DropColumn(name: "ReplayMode", table: "ReplayOperations");
            migrationBuilder.DropColumn(name: "StartedAtUtc", table: "ReplayOperations");
            migrationBuilder.DropColumn(name: "DurationMs", table: "ReplayOperations");
            migrationBuilder.DropColumn(name: "SkippedCount", table: "ReplayOperations");
            migrationBuilder.DropColumn(name: "ErrorSummary", table: "ReplayOperations");
            migrationBuilder.DropColumn(name: "BackgroundJobId", table: "ReplayOperations");
            migrationBuilder.DropColumn(name: "EventType", table: "ReplayOperationEvents");
            migrationBuilder.DropColumn(name: "EntityType", table: "ReplayOperationEvents");
            migrationBuilder.DropColumn(name: "EntityId", table: "ReplayOperationEvents");
            migrationBuilder.DropColumn(name: "SkippedReason", table: "ReplayOperationEvents");
            migrationBuilder.DropColumn(name: "DurationMs", table: "ReplayOperationEvents");
        }
    }
}
