using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CephasOps.Infrastructure.Persistence.Migrations;

/// <summary>
/// Phase 2 Notifications extraction: NotificationDispatches table for persistent, worker-driven delivery work.
/// </summary>
public partial class AddNotificationDispatches : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "NotificationDispatches",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CompanyId = table.Column<Guid>(type: "uuid", nullable: true),
                Channel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                Target = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                TemplateKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                PayloadJson = table.Column<string>(type: "jsonb", nullable: true),
                Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                AttemptCount = table.Column<int>(type: "integer", nullable: false),
                MaxAttempts = table.Column<int>(type: "integer", nullable: false),
                NextRetryAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                LastError = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                LastErrorAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CorrelationId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                CausationId = table.Column<Guid>(type: "uuid", nullable: true),
                SourceEventId = table.Column<Guid>(type: "uuid", nullable: true),
                IdempotencyKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                ProcessingNodeId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                ProcessingLeaseExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_NotificationDispatches", x => x.Id));

        migrationBuilder.CreateIndex(
            name: "IX_NotificationDispatches_Status_NextRetryAtUtc",
            table: "NotificationDispatches",
            columns: new[] { "Status", "NextRetryAtUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_NotificationDispatches_CompanyId_Status",
            table: "NotificationDispatches",
            columns: new[] { "CompanyId", "Status" });

        migrationBuilder.CreateIndex(
            name: "IX_NotificationDispatches_IdempotencyKey",
            table: "NotificationDispatches",
            column: "IdempotencyKey",
            unique: true,
            filter: "\"IdempotencyKey\" IS NOT NULL");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "NotificationDispatches");
    }
}
