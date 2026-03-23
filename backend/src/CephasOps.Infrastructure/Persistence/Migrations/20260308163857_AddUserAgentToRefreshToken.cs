using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CephasOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserAgentToRefreshToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Idempotent guard: some environments hit migration drift where a step tried to drop
            // IX_PasswordResetTokens_TokenHash (index did not exist). Ensure drop is safe.
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_PasswordResetTokens_TokenHash"";");

            // v1.4 Phase 3 Session Management: UserAgent for session visibility. Other model changes
            // (OrderPayoutSnapshots, etc.) were already applied via a different migration path.
            migrationBuilder.AddColumn<string>(
                name: "UserAgent",
                table: "RefreshTokens",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserAgent",
                table: "RefreshTokens");
        }
    }
}
