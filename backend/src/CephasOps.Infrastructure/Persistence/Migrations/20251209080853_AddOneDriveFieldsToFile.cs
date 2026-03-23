using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CephasOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOneDriveFieldsToFile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OneDriveFileId",
                table: "Files",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OneDriveWebUrl",
                table: "Files",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OneDriveSyncStatus",
                table: "Files",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "NotSynced");

            migrationBuilder.AddColumn<DateTime>(
                name: "OneDriveSyncedAt",
                table: "Files",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OneDriveSyncError",
                table: "Files",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OneDriveFileId",
                table: "Files");

            migrationBuilder.DropColumn(
                name: "OneDriveWebUrl",
                table: "Files");

            migrationBuilder.DropColumn(
                name: "OneDriveSyncStatus",
                table: "Files");

            migrationBuilder.DropColumn(
                name: "OneDriveSyncedAt",
                table: "Files");

            migrationBuilder.DropColumn(
                name: "OneDriveSyncError",
                table: "Files");
        }
    }
}
