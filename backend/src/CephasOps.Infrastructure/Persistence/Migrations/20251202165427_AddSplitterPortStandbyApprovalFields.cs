using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CephasOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSplitterPortStandbyApprovalFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ApprovalAttachmentId",
                table: "SplitterPorts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "StandbyOverrideApproved",
                table: "SplitterPorts",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApprovalAttachmentId",
                table: "SplitterPorts");

            migrationBuilder.DropColumn(
                name: "StandbyOverrideApproved",
                table: "SplitterPorts");
        }
    }
}
