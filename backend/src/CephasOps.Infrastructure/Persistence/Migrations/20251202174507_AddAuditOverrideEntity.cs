using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CephasOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditOverrideEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditOverrides",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    OverrideType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    OriginalValue = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    NewValue = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    EvidenceAttachmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    EvidenceNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    OverriddenByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    OverriddenByRole = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    OverriddenAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RequiredSecondaryApproval = table.Column<bool>(type: "boolean", nullable: false),
                    SecondaryApproverUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    SecondaryApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditOverrides", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditOverrides_CompanyId_EntityType_EntityId",
                table: "AuditOverrides",
                columns: new[] { "CompanyId", "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditOverrides_CompanyId_OverriddenByUserId_OverriddenAt",
                table: "AuditOverrides",
                columns: new[] { "CompanyId", "OverriddenByUserId", "OverriddenAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditOverrides_OverriddenAt",
                table: "AuditOverrides",
                column: "OverriddenAt");

            migrationBuilder.CreateIndex(
                name: "IX_AuditOverrides_OverrideType",
                table: "AuditOverrides",
                column: "OverrideType");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditOverrides");
        }
    }
}
