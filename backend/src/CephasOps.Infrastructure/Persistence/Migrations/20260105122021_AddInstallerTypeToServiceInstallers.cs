using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CephasOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInstallerTypeToServiceInstallers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Add InstallerType column as nullable initially
            migrationBuilder.AddColumn<string>(
                name: "InstallerType",
                table: "ServiceInstallers",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            // Step 2: Migrate existing data from IsSubcontractor to InstallerType
            migrationBuilder.Sql(@"
                UPDATE ""ServiceInstallers""
                SET ""InstallerType"" = CASE 
                    WHEN ""IsSubcontractor"" = true THEN 'Subcontractor' 
                    ELSE 'InHouse' 
                END
                WHERE ""InstallerType"" IS NULL;
            ");

            // Step 3: Make InstallerType NOT NULL with default
            migrationBuilder.AlterColumn<string>(
                name: "InstallerType",
                table: "ServiceInstallers",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "InHouse");

            // Step 4: Add check constraint to ensure valid values
            migrationBuilder.Sql(@"
                ALTER TABLE ""ServiceInstallers""
                ADD CONSTRAINT ""CK_ServiceInstallers_InstallerType"" 
                CHECK (""InstallerType"" IN ('InHouse', 'Subcontractor'));
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop constraint first
            migrationBuilder.Sql(@"
                ALTER TABLE ""ServiceInstallers""
                DROP CONSTRAINT IF EXISTS ""CK_ServiceInstallers_InstallerType"";
            ");

            // Drop column
            migrationBuilder.DropColumn(
                name: "InstallerType",
                table: "ServiceInstallers");
        }
    }
}
