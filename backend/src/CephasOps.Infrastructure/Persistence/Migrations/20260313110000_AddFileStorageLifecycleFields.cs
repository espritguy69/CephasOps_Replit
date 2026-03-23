using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CephasOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFileStorageLifecycleFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastAccessedAtUtc",
                table: "Files",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StorageTier",
                table: "Files",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Hot");

            migrationBuilder.CreateIndex(
                name: "IX_Files_CompanyId_StorageTier",
                table: "Files",
                columns: new[] { "CompanyId", "StorageTier" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_Files_CompanyId_StorageTier", table: "Files");
            migrationBuilder.DropColumn(name: "LastAccessedAtUtc", table: "Files");
            migrationBuilder.DropColumn(name: "StorageTier", table: "Files");
        }
    }
}
