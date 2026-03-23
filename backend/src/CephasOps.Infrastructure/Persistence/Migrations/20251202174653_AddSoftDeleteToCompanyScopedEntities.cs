using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CephasOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSoftDeleteToCompanyScopedEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add columns with IF NOT EXISTS check to handle existing columns
            migrationBuilder.Sql(@"
                DO $$ 
                BEGIN
                    -- Helper function to add column if not exists
                    CREATE OR REPLACE FUNCTION add_soft_delete_columns(table_name text) RETURNS void AS $func$
                    BEGIN
                        -- Add DeletedAt if not exists
                        IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = $1 AND column_name = 'DeletedAt') THEN
                            EXECUTE format('ALTER TABLE %I ADD COLUMN ""DeletedAt"" timestamp with time zone', $1);
                        END IF;
                        
                        -- Add DeletedByUserId if not exists
                        IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = $1 AND column_name = 'DeletedByUserId') THEN
                            EXECUTE format('ALTER TABLE %I ADD COLUMN ""DeletedByUserId"" uuid', $1);
                        END IF;
                        
                        -- Add IsDeleted if not exists
                        IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = $1 AND column_name = 'IsDeleted') THEN
                            EXECUTE format('ALTER TABLE %I ADD COLUMN ""IsDeleted"" boolean NOT NULL DEFAULT false', $1);
                        END IF;
                    END;
                    $func$ LANGUAGE plpgsql;
                    
                    -- Apply to all CompanyScopedEntity tables
                    PERFORM add_soft_delete_columns('WorkflowTransitions');
                    PERFORM add_soft_delete_columns('WorkflowJobs');
                    PERFORM add_soft_delete_columns('WorkflowDefinitions');
                    PERFORM add_soft_delete_columns('VipGroups');
                    PERFORM add_soft_delete_columns('VipEmails');
                    PERFORM add_soft_delete_columns('Verticals');
                    PERFORM add_soft_delete_columns('TimeSlots');
                    PERFORM add_soft_delete_columns('TaskItems');
                    PERFORM add_soft_delete_columns('SupplierInvoices');
                    PERFORM add_soft_delete_columns('SupplierInvoiceLineItems');
                    PERFORM add_soft_delete_columns('StockMovements');
                    PERFORM add_soft_delete_columns('StockLocations');
                    PERFORM add_soft_delete_columns('StockBalances');
                    PERFORM add_soft_delete_columns('SplitterTypes');
                    PERFORM add_soft_delete_columns('Splitters');
                    PERFORM add_soft_delete_columns('SplitterPorts');
                    PERFORM add_soft_delete_columns('SiRatePlans');
                    PERFORM add_soft_delete_columns('SiLeaveRequests');
                    PERFORM add_soft_delete_columns('SiAvailabilities');
                    PERFORM add_soft_delete_columns('ServiceInstallers');
                    PERFORM add_soft_delete_columns('ServiceInstallerContacts');
                    PERFORM add_soft_delete_columns('SerialisedItems');
                    PERFORM add_soft_delete_columns('ScheduledSlots');
                    PERFORM add_soft_delete_columns('RmaRequests');
                    PERFORM add_soft_delete_columns('RmaRequestItems');
                    PERFORM add_soft_delete_columns('RateCards');
                    PERFORM add_soft_delete_columns('RateCardLines');
                    PERFORM add_soft_delete_columns('PnlTypes');
                    PERFORM add_soft_delete_columns('PnlPeriods');
                    PERFORM add_soft_delete_columns('PnlFacts');
                    PERFORM add_soft_delete_columns('PnlDetailPerOrders');
                    PERFORM add_soft_delete_columns('PayrollRuns');
                    PERFORM add_soft_delete_columns('PayrollPeriods');
                    PERFORM add_soft_delete_columns('PayrollLines');
                    PERFORM add_soft_delete_columns('Payments');
                    PERFORM add_soft_delete_columns('Partners');
                    PERFORM add_soft_delete_columns('PartnerGroups');
                    PERFORM add_soft_delete_columns('ParseSessions');
                    PERFORM add_soft_delete_columns('ParserTemplates');
                    PERFORM add_soft_delete_columns('ParserRules');
                    PERFORM add_soft_delete_columns('ParsedOrderDrafts');
                    PERFORM add_soft_delete_columns('OverheadEntries');
                    PERFORM add_soft_delete_columns('OrderTypes');
                    PERFORM add_soft_delete_columns('OrderStatusLogs');
                    PERFORM add_soft_delete_columns('Orders');
                    PERFORM add_soft_delete_columns('OrderReschedules');
                    PERFORM add_soft_delete_columns('OrderMaterialUsage');
                    PERFORM add_soft_delete_columns('OrderDockets');
                    PERFORM add_soft_delete_columns('OrderBlockers');
                    PERFORM add_soft_delete_columns('NotificationSettings');
                    PERFORM add_soft_delete_columns('Notifications');
                    PERFORM add_soft_delete_columns('MaterialTemplates');
                    PERFORM add_soft_delete_columns('Materials');
                    PERFORM add_soft_delete_columns('MaterialCategories');
                    PERFORM add_soft_delete_columns('MaterialAllocations');
                    PERFORM add_soft_delete_columns('KpiProfiles');
                    PERFORM add_soft_delete_columns('JobEarningRecords');
                    PERFORM add_soft_delete_columns('InvoiceSubmissionHistory');
                    PERFORM add_soft_delete_columns('Invoices');
                    PERFORM add_soft_delete_columns('InvoiceLineItems');
                    PERFORM add_soft_delete_columns('InstallationTypes');
                    PERFORM add_soft_delete_columns('InstallationMethods');
                    PERFORM add_soft_delete_columns('HubBoxes');
                    PERFORM add_soft_delete_columns('GponSiJobRates');
                    PERFORM add_soft_delete_columns('GponSiCustomRates');
                    PERFORM add_soft_delete_columns('GponPartnerJobRates');
                    PERFORM add_soft_delete_columns('GeneratedDocuments');
                    PERFORM add_soft_delete_columns('EmailTemplates');
                    PERFORM add_soft_delete_columns('EmailMessages');
                    PERFORM add_soft_delete_columns('EmailAccounts');
                    PERFORM add_soft_delete_columns('DocumentTemplates');
                    PERFORM add_soft_delete_columns('DocumentPlaceholderDefinitions');
                    PERFORM add_soft_delete_columns('DeliveryOrders');
                    PERFORM add_soft_delete_columns('DepartmentMemberships');
                    PERFORM add_soft_delete_columns('Departments');
                    PERFORM add_soft_delete_columns('CustomRates');
                    PERFORM add_soft_delete_columns('CostCentres');
                    PERFORM add_soft_delete_columns('Companies');
                    PERFORM add_soft_delete_columns('Buildings');
                    PERFORM add_soft_delete_columns('BuildingTypes');
                    PERFORM add_soft_delete_columns('BuildingContacts');
                    PERFORM add_soft_delete_columns('BuildingBlocks');
                    PERFORM add_soft_delete_columns('BillingRatecards');
                    PERFORM add_soft_delete_columns('BackgroundJobs');
                    PERFORM add_soft_delete_columns('AuditOverrides');
                    PERFORM add_soft_delete_columns('AssetTypes');
                    PERFORM add_soft_delete_columns('Assets');
                    PERFORM add_soft_delete_columns('AssetMaintenances');
                    PERFORM add_soft_delete_columns('AssetDisposals');
                    PERFORM add_soft_delete_columns('AssetDepreciations');
                    
                    -- Drop the helper function
                    DROP FUNCTION add_soft_delete_columns(text);
                END $$;
            ");

            // Original AddColumn calls removed - replaced with SQL above
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "WorkflowTransitions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "WorkflowTransitions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "WorkflowTransitions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "WorkflowJobs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "WorkflowJobs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "WorkflowJobs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "WorkflowDefinitions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "WorkflowDefinitions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "WorkflowDefinitions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "VipGroups",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "VipGroups",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "VipGroups",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "VipEmails",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "VipEmails",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "VipEmails",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Verticals",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "Verticals",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Verticals",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "TimeSlots",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "TimeSlots",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "TimeSlots",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "TaskItems",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "TaskItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "TaskItems",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "SupplierInvoices",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "SupplierInvoices",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "SupplierInvoices",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "SupplierInvoiceLineItems",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "SupplierInvoiceLineItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "SupplierInvoiceLineItems",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "StockMovements",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "StockMovements",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "StockMovements",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "StockLocations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "StockLocations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "StockLocations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "StockBalances",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "StockBalances",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "StockBalances",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "SplitterTypes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "SplitterTypes",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "SplitterTypes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Splitters",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "Splitters",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Splitters",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "SplitterPorts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "SplitterPorts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "SplitterPorts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "SiRatePlans",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "SiRatePlans",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "SiRatePlans",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "SiLeaveRequests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "SiLeaveRequests",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "SiLeaveRequests",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "SiAvailabilities",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "SiAvailabilities",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "SiAvailabilities",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "ServiceInstallers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "ServiceInstallers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "ServiceInstallers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "ServiceInstallerContacts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "ServiceInstallerContacts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "ServiceInstallerContacts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "SerialisedItems",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "SerialisedItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "SerialisedItems",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "ScheduledSlots",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "ScheduledSlots",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "ScheduledSlots",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "RmaRequests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "RmaRequests",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "RmaRequests",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "RmaRequestItems",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "RmaRequestItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "RmaRequestItems",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "RateCards",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "RateCards",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "RateCards",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "RateCardLines",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "RateCardLines",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "RateCardLines",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "PnlTypes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "PnlTypes",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "PnlTypes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "PnlPeriods",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "PnlPeriods",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "PnlPeriods",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "PnlFacts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "PnlFacts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "PnlFacts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "PnlDetailPerOrders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "PnlDetailPerOrders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "PnlDetailPerOrders",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "PayrollRuns",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "PayrollRuns",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "PayrollRuns",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "PayrollPeriods",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "PayrollPeriods",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "PayrollPeriods",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "PayrollLines",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "PayrollLines",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "PayrollLines",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Payments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "Payments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Payments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Partners",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "Partners",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Partners",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "PartnerGroups",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "PartnerGroups",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "PartnerGroups",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "ParseSessions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "ParseSessions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "ParseSessions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "ParserTemplates",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "ParserTemplates",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "ParserTemplates",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "ParserRules",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "ParserRules",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "ParserRules",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "ParsedOrderDrafts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "ParsedOrderDrafts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "ParsedOrderDrafts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "OverheadEntries",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "OverheadEntries",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "OverheadEntries",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "OrderTypes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "OrderTypes",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "OrderTypes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "OrderStatusLogs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "OrderStatusLogs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "OrderStatusLogs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "Orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Orders",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "OrderReschedules",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "OrderReschedules",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "OrderReschedules",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "OrderMaterialUsage",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "OrderMaterialUsage",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "OrderMaterialUsage",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "OrderDockets",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "OrderDockets",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "OrderDockets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "OrderBlockers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "OrderBlockers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "OrderBlockers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "NotificationSettings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "NotificationSettings",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "NotificationSettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Notifications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "Notifications",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Notifications",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "MaterialTemplates",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "MaterialTemplates",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "MaterialTemplates",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Materials",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "Materials",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Materials",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "MaterialCategories",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "MaterialCategories",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "MaterialCategories",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "MaterialAllocations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "MaterialAllocations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "MaterialAllocations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "KpiProfiles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "KpiProfiles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "KpiProfiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "JobEarningRecords",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "JobEarningRecords",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "JobEarningRecords",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "InvoiceSubmissionHistory",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "InvoiceSubmissionHistory",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "InvoiceSubmissionHistory",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Invoices",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "Invoices",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Invoices",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "InvoiceLineItems",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "InvoiceLineItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "InvoiceLineItems",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "InstallationTypes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "InstallationTypes",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "InstallationTypes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "InstallationMethods",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "InstallationMethods",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "InstallationMethods",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "GponSiJobRates",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "GponSiJobRates",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "GponSiJobRates",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "GponSiCustomRates",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "GponSiCustomRates",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "GponSiCustomRates",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "GponPartnerJobRates",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "GponPartnerJobRates",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "GponPartnerJobRates",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "GeneratedDocuments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "GeneratedDocuments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "GeneratedDocuments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Files",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "Files",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Files",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "EmailTemplates",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "EmailTemplates",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "EmailTemplates",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "EmailMessages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "EmailMessages",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "EmailMessages",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "EmailAccounts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "EmailAccounts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "EmailAccounts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "DocumentTemplates",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "DocumentTemplates",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "DocumentTemplates",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Departments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "Departments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Departments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "DepartmentMemberships",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "DepartmentMemberships",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "DepartmentMemberships",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "CustomRates",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "CustomRates",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "CustomRates",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "CostCentres",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "CostCentres",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "CostCentres",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "CompanyDocuments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "CompanyDocuments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "CompanyDocuments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "BuildingTypes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "BuildingTypes",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "BuildingTypes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Buildings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "Buildings",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Buildings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "BillingRatecards",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "BillingRatecards",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "BillingRatecards",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "AuditOverrides",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "AuditOverrides",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "AuditOverrides",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "AssetTypes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "AssetTypes",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "AssetTypes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Assets",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "Assets",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Assets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "AssetMaintenanceRecords",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "AssetMaintenanceRecords",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "AssetMaintenanceRecords",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "AssetDisposals",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "AssetDisposals",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "AssetDisposals",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "AssetDepreciationEntries",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "AssetDepreciationEntries",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "AssetDepreciationEntries",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "WorkflowTransitions");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "WorkflowTransitions");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "WorkflowTransitions");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "WorkflowJobs");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "WorkflowJobs");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "WorkflowJobs");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "WorkflowDefinitions");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "WorkflowDefinitions");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "WorkflowDefinitions");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "VipGroups");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "VipGroups");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "VipGroups");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "VipEmails");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "VipEmails");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "VipEmails");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Verticals");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "Verticals");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Verticals");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "TimeSlots");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "TimeSlots");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "TimeSlots");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "TaskItems");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "TaskItems");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "TaskItems");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "SupplierInvoices");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "SupplierInvoices");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "SupplierInvoices");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "SupplierInvoiceLineItems");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "SupplierInvoiceLineItems");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "SupplierInvoiceLineItems");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "StockLocations");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "StockLocations");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "StockLocations");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "StockBalances");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "StockBalances");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "StockBalances");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "SplitterTypes");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "SplitterTypes");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "SplitterTypes");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Splitters");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "Splitters");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Splitters");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "SplitterPorts");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "SplitterPorts");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "SplitterPorts");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "SiRatePlans");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "SiRatePlans");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "SiRatePlans");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "SiLeaveRequests");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "SiLeaveRequests");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "SiLeaveRequests");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "SiAvailabilities");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "SiAvailabilities");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "SiAvailabilities");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "ServiceInstallers");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "ServiceInstallers");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "ServiceInstallers");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "ServiceInstallerContacts");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "ServiceInstallerContacts");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "ServiceInstallerContacts");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "SerialisedItems");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "SerialisedItems");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "SerialisedItems");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "ScheduledSlots");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "ScheduledSlots");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "ScheduledSlots");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "RmaRequests");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "RmaRequests");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "RmaRequests");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "RmaRequestItems");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "RmaRequestItems");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "RmaRequestItems");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "RateCards");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "RateCards");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "RateCards");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "RateCardLines");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "RateCardLines");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "RateCardLines");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "PnlTypes");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "PnlTypes");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "PnlTypes");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "PnlPeriods");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "PnlPeriods");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "PnlPeriods");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "PnlFacts");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "PnlFacts");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "PnlFacts");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "PnlDetailPerOrders");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "PnlDetailPerOrders");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "PnlDetailPerOrders");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "PayrollRuns");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "PayrollRuns");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "PayrollRuns");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "PayrollPeriods");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "PayrollPeriods");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "PayrollPeriods");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "PayrollLines");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "PayrollLines");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "PayrollLines");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Partners");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "Partners");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Partners");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "PartnerGroups");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "PartnerGroups");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "PartnerGroups");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "ParseSessions");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "ParseSessions");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "ParseSessions");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "ParserTemplates");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "ParserTemplates");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "ParserTemplates");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "ParserRules");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "ParserRules");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "ParserRules");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "ParsedOrderDrafts");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "ParsedOrderDrafts");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "ParsedOrderDrafts");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "OverheadEntries");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "OverheadEntries");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "OverheadEntries");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "OrderTypes");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "OrderTypes");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "OrderTypes");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "OrderStatusLogs");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "OrderStatusLogs");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "OrderStatusLogs");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "OrderReschedules");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "OrderReschedules");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "OrderReschedules");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "OrderMaterialUsage");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "OrderMaterialUsage");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "OrderMaterialUsage");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "OrderDockets");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "OrderDockets");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "OrderDockets");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "OrderBlockers");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "OrderBlockers");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "OrderBlockers");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "NotificationSettings");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "NotificationSettings");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "NotificationSettings");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "MaterialTemplates");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "MaterialTemplates");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "MaterialTemplates");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Materials");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "Materials");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Materials");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "MaterialCategories");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "MaterialCategories");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "MaterialCategories");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "MaterialAllocations");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "MaterialAllocations");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "MaterialAllocations");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "KpiProfiles");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "KpiProfiles");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "KpiProfiles");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "JobEarningRecords");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "JobEarningRecords");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "JobEarningRecords");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "InvoiceSubmissionHistory");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "InvoiceSubmissionHistory");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "InvoiceSubmissionHistory");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "InvoiceLineItems");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "InvoiceLineItems");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "InvoiceLineItems");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "InstallationTypes");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "InstallationTypes");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "InstallationTypes");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "InstallationMethods");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "InstallationMethods");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "InstallationMethods");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "GponSiJobRates");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "GponSiJobRates");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "GponSiJobRates");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "GponSiCustomRates");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "GponSiCustomRates");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "GponSiCustomRates");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "GponPartnerJobRates");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "GponPartnerJobRates");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "GponPartnerJobRates");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "GeneratedDocuments");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "GeneratedDocuments");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "GeneratedDocuments");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Files");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "Files");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Files");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "EmailTemplates");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "EmailTemplates");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "EmailTemplates");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "EmailMessages");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "EmailMessages");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "EmailMessages");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "EmailAccounts");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "EmailAccounts");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "EmailAccounts");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "DocumentTemplates");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "DocumentTemplates");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "DocumentTemplates");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Departments");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "Departments");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Departments");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "DepartmentMemberships");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "DepartmentMemberships");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "DepartmentMemberships");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "CustomRates");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "CustomRates");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "CustomRates");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "CostCentres");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "CostCentres");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "CostCentres");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "CompanyDocuments");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "CompanyDocuments");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "CompanyDocuments");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "BuildingTypes");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "BuildingTypes");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "BuildingTypes");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Buildings");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "Buildings");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Buildings");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "BillingRatecards");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "BillingRatecards");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "BillingRatecards");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "AuditOverrides");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "AuditOverrides");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "AuditOverrides");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "AssetTypes");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "AssetTypes");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "AssetTypes");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "AssetMaintenanceRecords");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "AssetMaintenanceRecords");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "AssetMaintenanceRecords");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "AssetDisposals");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "AssetDisposals");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "AssetDisposals");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "AssetDepreciationEntries");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "AssetDepreciationEntries");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "AssetDepreciationEntries");
        }
    }
}
