using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CephasOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReplayOperationSafetyWindow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "SafetyCutoffOccurredAtUtc",
                table: "ReplayOperations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SafetyWindowMinutes",
                table: "ReplayOperations",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SafetyCutoffOccurredAtUtc",
                table: "ReplayOperations");

            migrationBuilder.DropColumn(
                name: "SafetyWindowMinutes",
                table: "ReplayOperations");
        }
    }
}
