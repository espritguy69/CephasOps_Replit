using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CephasOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEventProcessingLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EventProcessingLog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    HandlerName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ReplayOperationId = table.Column<Guid>(type: "uuid", nullable: true),
                    State = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventProcessingLog", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EventProcessingLog_EventId",
                table: "EventProcessingLog",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_EventProcessingLog_EventId_HandlerName",
                table: "EventProcessingLog",
                columns: new[] { "EventId", "HandlerName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventProcessingLog_ReplayOperationId",
                table: "EventProcessingLog",
                column: "ReplayOperationId",
                filter: "\"ReplayOperationId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EventProcessingLog_State_StartedAtUtc",
                table: "EventProcessingLog",
                columns: new[] { "State", "StartedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventProcessingLog");
        }
    }
}
