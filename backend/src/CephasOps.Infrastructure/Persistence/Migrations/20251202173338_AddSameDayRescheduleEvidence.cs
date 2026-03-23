using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CephasOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSameDayRescheduleEvidence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSameDayReschedule",
                table: "OrderReschedules",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "SameDayEvidenceAttachmentId",
                table: "OrderReschedules",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SameDayEvidenceNotes",
                table: "OrderReschedules",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderReschedules_IsSameDayReschedule",
                table: "OrderReschedules",
                column: "IsSameDayReschedule");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OrderReschedules_IsSameDayReschedule",
                table: "OrderReschedules");

            migrationBuilder.DropColumn(
                name: "IsSameDayReschedule",
                table: "OrderReschedules");

            migrationBuilder.DropColumn(
                name: "SameDayEvidenceAttachmentId",
                table: "OrderReschedules");

            migrationBuilder.DropColumn(
                name: "SameDayEvidenceNotes",
                table: "OrderReschedules");
        }
    }
}
