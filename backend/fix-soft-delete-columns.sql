-- Fix soft delete columns for Supabase database
-- This script adds DeletedAt, DeletedByUserId, and IsDeleted columns to all CompanyScopedEntity tables
-- It checks if columns exist before adding them to avoid conflicts

DO $$ 
DECLARE
    table_names text[] := ARRAY[
        'WorkflowTransitions', 'WorkflowJobs', 'WorkflowDefinitions', 'VipGroups', 'VipEmails',
        'Verticals', 'TimeSlots', 'TaskItems', 'SupplierInvoices', 'SupplierInvoiceLineItems',
        'StockMovements', 'StockLocations', 'StockBalances', 'SplitterTypes', 'Splitters',
        'SplitterPorts', 'SiRatePlans', 'SiLeaveRequests', 'SiAvailabilities', 'ServiceInstallers',
        'ServiceInstallerContacts', 'SerialisedItems', 'ScheduledSlots', 'RmaRequests', 'RmaRequestItems',
        'RateCards', 'RateCardLines', 'PnlTypes', 'PnlPeriods', 'PnlFacts', 'PnlDetailPerOrders',
        'PayrollRuns', 'PayrollPeriods', 'PayrollLines', 'Payments', 'Partners', 'PartnerGroups',
        'ParseSessions', 'ParserTemplates', 'ParserRules', 'ParsedOrderDrafts', 'OverheadEntries',
        'OrderTypes', 'OrderStatusLogs', 'Orders', 'OrderReschedules', 'OrderMaterialUsage',
        'OrderDockets', 'OrderBlockers', 'NotificationSettings', 'Notifications', 'MaterialTemplates',
        'Materials', 'MaterialCategories', 'MaterialAllocations', 'KpiProfiles', 'JobEarningRecords',
        'InvoiceSubmissionHistory', 'Invoices', 'InvoiceLineItems', 'InstallationTypes', 'InstallationMethods',
        'HubBoxes', 'GponSiJobRates', 'GponSiCustomRates', 'GponPartnerJobRates', 'GeneratedDocuments',
        'EmailTemplates', 'EmailMessages', 'EmailAccounts', 'DocumentTemplates', 'DocumentPlaceholderDefinitions',
        'DeliveryOrders', 'DepartmentMemberships', 'Departments', 'CustomRates', 'CostCentres',
        'Companies', 'Buildings', 'BuildingTypes', 'BuildingContacts', 'BuildingBlocks',
        'BillingRatecards', 'BackgroundJobs', 'AuditOverrides', 'AssetTypes', 'Assets',
        'AssetMaintenances', 'AssetDisposals', 'AssetDepreciations'
    ];
    tbl_name text;
    col_exists boolean;
BEGIN
    FOREACH tbl_name IN ARRAY table_names
    LOOP
        -- Check if table exists first
        IF NOT EXISTS (
            SELECT 1 FROM information_schema.tables t
            WHERE t.table_schema = 'public'
            AND (t.table_name = tbl_name OR t.table_name = lower(tbl_name))
        ) THEN
            RAISE NOTICE 'Table % does not exist, skipping', tbl_name;
            CONTINUE;
        END IF;
        
        -- Check and add DeletedAt (check both PascalCase and lowercase)
        SELECT EXISTS (
            SELECT 1 FROM information_schema.columns c
            WHERE c.table_schema = 'public' 
            AND (c.table_name = tbl_name OR c.table_name = lower(tbl_name))
            AND c.column_name = 'DeletedAt'
        ) INTO col_exists;
        
        IF NOT col_exists THEN
            BEGIN
                EXECUTE format('ALTER TABLE "%s" ADD COLUMN "DeletedAt" timestamp with time zone', tbl_name);
                RAISE NOTICE 'Added DeletedAt to %', tbl_name;
            EXCEPTION WHEN OTHERS THEN
                RAISE NOTICE 'Failed to add DeletedAt to %: %', tbl_name, SQLERRM;
            END;
        ELSE
            RAISE NOTICE 'DeletedAt already exists in %', tbl_name;
        END IF;
        
        -- Check and add DeletedByUserId (check both PascalCase and lowercase)
        SELECT EXISTS (
            SELECT 1 FROM information_schema.columns c
            WHERE c.table_schema = 'public' 
            AND (c.table_name = tbl_name OR c.table_name = lower(tbl_name))
            AND c.column_name = 'DeletedByUserId'
        ) INTO col_exists;
        
        IF NOT col_exists THEN
            BEGIN
                EXECUTE format('ALTER TABLE "%s" ADD COLUMN "DeletedByUserId" uuid', tbl_name);
                RAISE NOTICE 'Added DeletedByUserId to %', tbl_name;
            EXCEPTION WHEN OTHERS THEN
                RAISE NOTICE 'Failed to add DeletedByUserId to %: %', tbl_name, SQLERRM;
            END;
        ELSE
            RAISE NOTICE 'DeletedByUserId already exists in %', tbl_name;
        END IF;
        
        -- Check and add IsDeleted (check both PascalCase and lowercase)
        SELECT EXISTS (
            SELECT 1 FROM information_schema.columns c
            WHERE c.table_schema = 'public' 
            AND (c.table_name = tbl_name OR c.table_name = lower(tbl_name))
            AND c.column_name = 'IsDeleted'
        ) INTO col_exists;
        
        IF NOT col_exists THEN
            BEGIN
                EXECUTE format('ALTER TABLE "%s" ADD COLUMN "IsDeleted" boolean NOT NULL DEFAULT false', tbl_name);
                RAISE NOTICE 'Added IsDeleted to %', tbl_name;
            EXCEPTION WHEN OTHERS THEN
                RAISE NOTICE 'Failed to add IsDeleted to %: %', tbl_name, SQLERRM;
            END;
        ELSE
            RAISE NOTICE 'IsDeleted already exists in %', tbl_name;
        END IF;
    END LOOP;
END $$;

-- Mark the migration as applied if not already
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
SELECT '20251202174653_AddSoftDeleteToCompanyScopedEntities', '10.0.0'
WHERE NOT EXISTS (
    SELECT 1 FROM "__EFMigrationsHistory" 
    WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities'
);
