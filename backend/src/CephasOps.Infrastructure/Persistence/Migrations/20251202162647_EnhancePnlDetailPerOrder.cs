using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CephasOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EnhancePnlDetailPerOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CalculatedAt",
                table: "PnlDetailPerOrders",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "PnlDetailPerOrders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DataQualityNotes",
                table: "PnlDetailPerOrders",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DepartmentId",
                table: "PnlDetailPerOrders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "GrossProfit",
                table: "PnlDetailPerOrders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "InstallationMethod",
                table: "PnlDetailPerOrders",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InstallationType",
                table: "PnlDetailPerOrders",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "KpiResult",
                table: "PnlDetailPerOrders",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LabourRateSource",
                table: "PnlDetailPerOrders",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RescheduleCount",
                table: "PnlDetailPerOrders",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "RevenueRateSource",
                table: "PnlDetailPerOrders",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ServiceInstallerId",
                table: "PnlDetailPerOrders",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PnlDetailPerOrders_CompanyId_Period_OrderType",
                table: "PnlDetailPerOrders",
                columns: new[] { "CompanyId", "Period", "OrderType" });

            migrationBuilder.CreateIndex(
                name: "IX_PnlDetailPerOrders_DepartmentId",
                table: "PnlDetailPerOrders",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_PnlDetailPerOrders_ServiceInstallerId",
                table: "PnlDetailPerOrders",
                column: "ServiceInstallerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PnlDetailPerOrders_CompanyId_Period_OrderType",
                table: "PnlDetailPerOrders");

            migrationBuilder.DropIndex(
                name: "IX_PnlDetailPerOrders_DepartmentId",
                table: "PnlDetailPerOrders");

            migrationBuilder.DropIndex(
                name: "IX_PnlDetailPerOrders_ServiceInstallerId",
                table: "PnlDetailPerOrders");

            migrationBuilder.DropColumn(
                name: "CalculatedAt",
                table: "PnlDetailPerOrders");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "PnlDetailPerOrders");

            migrationBuilder.DropColumn(
                name: "DataQualityNotes",
                table: "PnlDetailPerOrders");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "PnlDetailPerOrders");

            migrationBuilder.DropColumn(
                name: "GrossProfit",
                table: "PnlDetailPerOrders");

            migrationBuilder.DropColumn(
                name: "InstallationMethod",
                table: "PnlDetailPerOrders");

            migrationBuilder.DropColumn(
                name: "InstallationType",
                table: "PnlDetailPerOrders");

            migrationBuilder.DropColumn(
                name: "KpiResult",
                table: "PnlDetailPerOrders");

            migrationBuilder.DropColumn(
                name: "LabourRateSource",
                table: "PnlDetailPerOrders");

            migrationBuilder.DropColumn(
                name: "RescheduleCount",
                table: "PnlDetailPerOrders");

            migrationBuilder.DropColumn(
                name: "RevenueRateSource",
                table: "PnlDetailPerOrders");

            migrationBuilder.DropColumn(
                name: "ServiceInstallerId",
                table: "PnlDetailPerOrders");
        }
    }
}
