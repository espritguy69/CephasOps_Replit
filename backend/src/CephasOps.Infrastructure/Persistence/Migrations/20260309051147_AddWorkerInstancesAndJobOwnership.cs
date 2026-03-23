using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CephasOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkerInstancesAndJobOwnership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ClaimedAtUtc",
                table: "ReplayOperations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "WorkerId",
                table: "ReplayOperations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ClaimedAtUtc",
                table: "RebuildOperations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "WorkerId",
                table: "RebuildOperations",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "WorkerInstances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    HostName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ProcessId = table.Column<int>(type: "integer", nullable: false),
                    Role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastHeartbeatUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkerInstances", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReplayOperations_WorkerId",
                table: "ReplayOperations",
                column: "WorkerId");

            migrationBuilder.CreateIndex(
                name: "IX_RebuildOperations_WorkerId",
                table: "RebuildOperations",
                column: "WorkerId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkerInstances_IsActive_LastHeartbeatUtc",
                table: "WorkerInstances",
                columns: new[] { "IsActive", "LastHeartbeatUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkerInstances_LastHeartbeatUtc",
                table: "WorkerInstances",
                column: "LastHeartbeatUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorkerInstances");

            migrationBuilder.DropIndex(
                name: "IX_ReplayOperations_WorkerId",
                table: "ReplayOperations");

            migrationBuilder.DropIndex(
                name: "IX_RebuildOperations_WorkerId",
                table: "RebuildOperations");

            migrationBuilder.DropColumn(
                name: "ClaimedAtUtc",
                table: "ReplayOperations");

            migrationBuilder.DropColumn(
                name: "WorkerId",
                table: "ReplayOperations");

            migrationBuilder.DropColumn(
                name: "ClaimedAtUtc",
                table: "RebuildOperations");

            migrationBuilder.DropColumn(
                name: "WorkerId",
                table: "RebuildOperations");
        }
    }
}
