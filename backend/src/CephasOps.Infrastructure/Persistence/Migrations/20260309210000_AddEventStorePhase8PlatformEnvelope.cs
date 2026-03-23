using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CephasOps.Infrastructure.Persistence.Migrations;

/// <summary>
/// Phase 8: Platform event envelope columns and indexes for EventStore.
/// Adds RootEventId, PartitionKey, ReplayId, SourceService, SourceModule, CapturedAtUtc, IdempotencyKey, TraceId, SpanId, Priority.
/// </summary>
public partial class AddEventStorePhase8PlatformEnvelope : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "RootEventId",
            table: "EventStore",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "PartitionKey",
            table: "EventStore",
            type: "character varying(500)",
            maxLength: 500,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ReplayId",
            table: "EventStore",
            type: "character varying(100)",
            maxLength: 100,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "SourceService",
            table: "EventStore",
            type: "character varying(100)",
            maxLength: 100,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "SourceModule",
            table: "EventStore",
            type: "character varying(100)",
            maxLength: 100,
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "CapturedAtUtc",
            table: "EventStore",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "IdempotencyKey",
            table: "EventStore",
            type: "character varying(500)",
            maxLength: 500,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "TraceId",
            table: "EventStore",
            type: "character varying(64)",
            maxLength: 64,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "SpanId",
            table: "EventStore",
            type: "character varying(64)",
            maxLength: 64,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "Priority",
            table: "EventStore",
            type: "character varying(50)",
            maxLength: 50,
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_EventStore_RootEventId",
            table: "EventStore",
            column: "RootEventId",
            filter: "\"RootEventId\" IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "IX_EventStore_PartitionKey",
            table: "EventStore",
            column: "PartitionKey",
            filter: "\"PartitionKey\" IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "IX_EventStore_ReplayId",
            table: "EventStore",
            column: "ReplayId",
            filter: "\"ReplayId\" IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "IX_EventStore_PartitionKey_CreatedAtUtc_EventId",
            table: "EventStore",
            columns: new[] { "PartitionKey", "CreatedAtUtc", "EventId" },
            filter: "\"PartitionKey\" IS NOT NULL");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_EventStore_RootEventId",
            table: "EventStore");

        migrationBuilder.DropIndex(
            name: "IX_EventStore_PartitionKey",
            table: "EventStore");

        migrationBuilder.DropIndex(
            name: "IX_EventStore_ReplayId",
            table: "EventStore");

        migrationBuilder.DropIndex(
            name: "IX_EventStore_PartitionKey_CreatedAtUtc_EventId",
            table: "EventStore");

        migrationBuilder.DropColumn(name: "RootEventId", table: "EventStore");
        migrationBuilder.DropColumn(name: "PartitionKey", table: "EventStore");
        migrationBuilder.DropColumn(name: "ReplayId", table: "EventStore");
        migrationBuilder.DropColumn(name: "SourceService", table: "EventStore");
        migrationBuilder.DropColumn(name: "SourceModule", table: "EventStore");
        migrationBuilder.DropColumn(name: "CapturedAtUtc", table: "EventStore");
        migrationBuilder.DropColumn(name: "IdempotencyKey", table: "EventStore");
        migrationBuilder.DropColumn(name: "TraceId", table: "EventStore");
        migrationBuilder.DropColumn(name: "SpanId", table: "EventStore");
        migrationBuilder.DropColumn(name: "Priority", table: "EventStore");
    }
}
