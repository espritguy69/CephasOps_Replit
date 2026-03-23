using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CephasOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRowVersionConcurrencyTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "WorkflowTransitions",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "WorkflowJobs",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "WorkflowDefinitions",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "VipGroups",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "VipEmails",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Verticals",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "TimeSlots",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "TaskItems",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "SupplierInvoices",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "SupplierInvoiceLineItems",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "StockMovements",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "StockLocations",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "StockBalances",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "SplitterTypes",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Splitters",
                type: "bytea",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "SplitterPorts",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "SiRatePlans",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "SiLeaveRequests",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "SiAvailabilities",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "ServiceInstallers",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "ServiceInstallerContacts",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "SerialisedItems",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "ScheduledSlots",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "RmaRequests",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "RmaRequestItems",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "RateCards",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "RateCardLines",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "PnlTypes",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "PnlPeriods",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "PnlFacts",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "PnlDetailPerOrders",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "PayrollRuns",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "PayrollPeriods",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "PayrollLines",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Payments",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Partners",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "PartnerGroups",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "ParseSessions",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "ParserTemplates",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "ParserRules",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "ParsedOrderDrafts",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "OverheadEntries",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "OrderTypes",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "OrderStatusLogs",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Orders",
                type: "bytea",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "OrderReschedules",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "OrderMaterialUsage",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "OrderDockets",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "OrderBlockers",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "NotificationSettings",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Notifications",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "MaterialTemplates",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Materials",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "MaterialCategories",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "MaterialAllocations",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "KpiProfiles",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "JobEarningRecords",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "InvoiceSubmissionHistory",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Invoices",
                type: "bytea",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "InvoiceLineItems",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "InstallationTypes",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "InstallationMethods",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "GponSiJobRates",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "GponSiCustomRates",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "GponPartnerJobRates",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "GeneratedDocuments",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Files",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "EmailTemplates",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "EmailMessages",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "EmailAccounts",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "DocumentTemplates",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Departments",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "DepartmentMemberships",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "CustomRates",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "CostCentres",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "CompanyDocuments",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "BuildingTypes",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Buildings",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "BillingRatecards",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "AuditOverrides",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "AssetTypes",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Assets",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "AssetMaintenanceRecords",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "AssetDisposals",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "AssetDepreciationEntries",
                type: "bytea",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderReschedules_OrderId",
                table: "OrderReschedules",
                column: "OrderId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderBlockers_Orders_OrderId",
                table: "OrderBlockers",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderReschedules_Orders_OrderId",
                table: "OrderReschedules",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderStatusLogs_Orders_OrderId",
                table: "OrderStatusLogs",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderBlockers_Orders_OrderId",
                table: "OrderBlockers");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderReschedules_Orders_OrderId",
                table: "OrderReschedules");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderStatusLogs_Orders_OrderId",
                table: "OrderStatusLogs");

            migrationBuilder.DropIndex(
                name: "IX_OrderReschedules_OrderId",
                table: "OrderReschedules");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "WorkflowTransitions");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "WorkflowJobs");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "WorkflowDefinitions");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "VipGroups");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "VipEmails");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Verticals");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "TimeSlots");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "TaskItems");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "SupplierInvoices");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "SupplierInvoiceLineItems");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "StockLocations");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "StockBalances");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "SplitterTypes");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Splitters");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "SplitterPorts");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "SiRatePlans");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "SiLeaveRequests");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "SiAvailabilities");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "ServiceInstallers");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "ServiceInstallerContacts");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "SerialisedItems");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "ScheduledSlots");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "RmaRequests");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "RmaRequestItems");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "RateCards");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "RateCardLines");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "PnlTypes");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "PnlPeriods");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "PnlFacts");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "PnlDetailPerOrders");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "PayrollRuns");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "PayrollPeriods");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "PayrollLines");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Partners");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "PartnerGroups");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "ParseSessions");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "ParserTemplates");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "ParserRules");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "ParsedOrderDrafts");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "OverheadEntries");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "OrderTypes");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "OrderStatusLogs");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "OrderReschedules");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "OrderMaterialUsage");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "OrderDockets");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "OrderBlockers");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "NotificationSettings");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "MaterialTemplates");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Materials");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "MaterialCategories");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "MaterialAllocations");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "KpiProfiles");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "JobEarningRecords");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "InvoiceSubmissionHistory");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "InvoiceLineItems");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "InstallationTypes");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "InstallationMethods");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "GponSiJobRates");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "GponSiCustomRates");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "GponPartnerJobRates");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "GeneratedDocuments");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Files");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "EmailTemplates");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "EmailMessages");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "EmailAccounts");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "DocumentTemplates");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Departments");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "DepartmentMemberships");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "CustomRates");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "CostCentres");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "CompanyDocuments");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "BuildingTypes");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Buildings");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "BillingRatecards");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "AuditOverrides");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "AssetTypes");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "AssetMaintenanceRecords");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "AssetDisposals");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "AssetDepreciationEntries");
        }
    }
}
