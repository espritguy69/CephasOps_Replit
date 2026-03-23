using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CephasOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SaasScalingSubscriptionAndMetrics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BillingCycle",
                table: "TenantSubscriptions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextBillingDateUtc",
                table: "TenantSubscriptions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SeatLimit",
                table: "TenantSubscriptions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "StorageLimitBytes",
                table: "TenantSubscriptions",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TrialEndsAtUtc",
                table: "TenantSubscriptions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ParserError",
                table: "EmailMessages",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "BodyPreview",
                table: "EmailMessages",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "TenantMetricsDaily",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    DateUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ActiveUsers = table.Column<int>(type: "integer", nullable: false),
                    TotalUsers = table.Column<int>(type: "integer", nullable: false),
                    OrdersCreated = table.Column<int>(type: "integer", nullable: false),
                    BackgroundJobsExecuted = table.Column<int>(type: "integer", nullable: false),
                    StorageBytes = table.Column<long>(type: "bigint", nullable: false),
                    ApiCalls = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantMetricsDaily", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TenantMetricsMonthly",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    Month = table.Column<int>(type: "integer", nullable: false),
                    ActiveUsers = table.Column<int>(type: "integer", nullable: false),
                    TotalUsers = table.Column<int>(type: "integer", nullable: false),
                    OrdersCreated = table.Column<int>(type: "integer", nullable: false),
                    BackgroundJobsExecuted = table.Column<int>(type: "integer", nullable: false),
                    StorageBytes = table.Column<long>(type: "bigint", nullable: false),
                    ApiCalls = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantMetricsMonthly", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TenantMetricsDaily_TenantId_DateUtc",
                table: "TenantMetricsDaily",
                columns: new[] { "TenantId", "DateUtc" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantMetricsMonthly_TenantId_Year_Month",
                table: "TenantMetricsMonthly",
                columns: new[] { "TenantId", "Year", "Month" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TenantMetricsDaily");

            migrationBuilder.DropTable(
                name: "TenantMetricsMonthly");

            migrationBuilder.DropColumn(
                name: "BillingCycle",
                table: "TenantSubscriptions");

            migrationBuilder.DropColumn(
                name: "NextBillingDateUtc",
                table: "TenantSubscriptions");

            migrationBuilder.DropColumn(
                name: "SeatLimit",
                table: "TenantSubscriptions");

            migrationBuilder.DropColumn(
                name: "StorageLimitBytes",
                table: "TenantSubscriptions");

            migrationBuilder.DropColumn(
                name: "TrialEndsAtUtc",
                table: "TenantSubscriptions");

            migrationBuilder.AlterColumn<string>(
                name: "ParserError",
                table: "EmailMessages",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "BodyPreview",
                table: "EmailMessages",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }
    }
}
