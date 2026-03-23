using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CephasOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CapturePendingModelChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "SiLevel",
                table: "ServiceInstallers",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Junior",
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AddColumn<string>(
                name: "AvailabilityStatus",
                table: "ServiceInstallers",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ContractEndDate",
                table: "ServiceInstallers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ContractStartDate",
                table: "ServiceInstallers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContractorCompany",
                table: "ServiceInstallers",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContractorId",
                table: "ServiceInstallers",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmploymentStatus",
                table: "ServiceInstallers",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "HireDate",
                table: "ServiceInstallers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Skills",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Skills", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ServiceInstallerSkills",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceInstallerId = table.Column<Guid>(type: "uuid", nullable: false),
                    SkillId = table.Column<Guid>(type: "uuid", nullable: false),
                    AcquiredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    VerifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    VerifiedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceInstallerSkills", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceInstallerSkills_ServiceInstallers_ServiceInstallerId",
                        column: x => x.ServiceInstallerId,
                        principalTable: "ServiceInstallers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ServiceInstallerSkills_Skills_SkillId",
                        column: x => x.SkillId,
                        principalTable: "Skills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_ServiceInstallers_SiLevel",
                table: "ServiceInstallers",
                sql: "\"SiLevel\" IN ('Junior', 'Senior')");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceInstallerSkills_ServiceInstallerId",
                table: "ServiceInstallerSkills",
                column: "ServiceInstallerId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceInstallerSkills_ServiceInstallerId_IsActive",
                table: "ServiceInstallerSkills",
                columns: new[] { "ServiceInstallerId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceInstallerSkills_ServiceInstallerId_SkillId_IsActive",
                table: "ServiceInstallerSkills",
                columns: new[] { "ServiceInstallerId", "SkillId", "IsActive" },
                unique: true,
                filter: "\"IsActive\" = true AND \"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceInstallerSkills_SkillId",
                table: "ServiceInstallerSkills",
                column: "SkillId");

            migrationBuilder.CreateIndex(
                name: "IX_Skills_CompanyId_Category_IsActive",
                table: "Skills",
                columns: new[] { "CompanyId", "Category", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Skills_CompanyId_Code",
                table: "Skills",
                columns: new[] { "CompanyId", "Code" },
                unique: true,
                filter: "\"IsDeleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ServiceInstallerSkills");

            migrationBuilder.DropTable(
                name: "Skills");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ServiceInstallers_SiLevel",
                table: "ServiceInstallers");

            migrationBuilder.DropColumn(
                name: "AvailabilityStatus",
                table: "ServiceInstallers");

            migrationBuilder.DropColumn(
                name: "ContractEndDate",
                table: "ServiceInstallers");

            migrationBuilder.DropColumn(
                name: "ContractStartDate",
                table: "ServiceInstallers");

            migrationBuilder.DropColumn(
                name: "ContractorCompany",
                table: "ServiceInstallers");

            migrationBuilder.DropColumn(
                name: "ContractorId",
                table: "ServiceInstallers");

            migrationBuilder.DropColumn(
                name: "EmploymentStatus",
                table: "ServiceInstallers");

            migrationBuilder.DropColumn(
                name: "HireDate",
                table: "ServiceInstallers");

            migrationBuilder.AlterColumn<string>(
                name: "SiLevel",
                table: "ServiceInstallers",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldDefaultValue: "Junior");
        }
    }
}
