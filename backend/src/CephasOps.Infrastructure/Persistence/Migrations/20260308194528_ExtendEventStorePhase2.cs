using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CephasOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ExtendEventStorePhase2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "EventStore",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.AddColumn<Guid>(
                name: "EntityId",
                table: "EventStore",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EntityType",
                table: "EventStore",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastError",
                table: "EventStore",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastErrorAtUtc",
                table: "EventStore",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastHandler",
                table: "EventStore",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ParentEventId",
                table: "EventStore",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "EventStore",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TriggeredByUserId",
                table: "EventStore",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventStore_OccurredAtUtc",
                table: "EventStore",
                column: "OccurredAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EventStore_OccurredAtUtc",
                table: "EventStore");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "EventStore");

            migrationBuilder.DropColumn(
                name: "EntityId",
                table: "EventStore");

            migrationBuilder.DropColumn(
                name: "EntityType",
                table: "EventStore");

            migrationBuilder.DropColumn(
                name: "LastError",
                table: "EventStore");

            migrationBuilder.DropColumn(
                name: "LastErrorAtUtc",
                table: "EventStore");

            migrationBuilder.DropColumn(
                name: "LastHandler",
                table: "EventStore");

            migrationBuilder.DropColumn(
                name: "ParentEventId",
                table: "EventStore");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "EventStore");

            migrationBuilder.DropColumn(
                name: "TriggeredByUserId",
                table: "EventStore");
        }
    }
}
