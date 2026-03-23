using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CephasOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEventStorePhase4NextRetryAndVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "NextRetryAtUtc",
                table: "EventStore",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PayloadVersion",
                table: "EventStore",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventStore_Status_NextRetryAtUtc",
                table: "EventStore",
                columns: new[] { "Status", "NextRetryAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EventStore_Status_NextRetryAtUtc",
                table: "EventStore");

            migrationBuilder.DropColumn(
                name: "NextRetryAtUtc",
                table: "EventStore");

            migrationBuilder.DropColumn(
                name: "PayloadVersion",
                table: "EventStore");
        }
    }
}
