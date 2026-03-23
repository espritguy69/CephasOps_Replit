using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CephasOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerPreferenceTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "customer_preferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    uses_whatsapp = table.Column<bool>(type: "boolean", nullable: true),
                    last_whatsapp_check = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_whatsapp_success = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_whatsapp_failure = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    consecutive_whatsapp_failures = table.Column<int>(type: "integer", nullable: false),
                    preferred_channel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customer_preferences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "sms_gateways",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    device_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    base_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    api_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    last_seen_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    additional_info = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sms_gateways", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_customer_preferences_customer_phone",
                table: "customer_preferences",
                column: "customer_phone",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_customer_preferences_last_whatsapp_check",
                table: "customer_preferences",
                column: "last_whatsapp_check");

            migrationBuilder.CreateIndex(
                name: "IX_customer_preferences_uses_whatsapp",
                table: "customer_preferences",
                column: "uses_whatsapp");

            migrationBuilder.CreateIndex(
                name: "IX_sms_gateways_is_active",
                table: "sms_gateways",
                column: "is_active",
                filter: "is_active = true");

            migrationBuilder.CreateIndex(
                name: "IX_sms_gateways_last_seen_at_utc",
                table: "sms_gateways",
                column: "last_seen_at_utc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "customer_preferences");

            migrationBuilder.DropTable(
                name: "sms_gateways");
        }
    }
}
