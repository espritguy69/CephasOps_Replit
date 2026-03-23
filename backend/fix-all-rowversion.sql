-- Add RowVersion to all tables that might be queried
-- This is a comprehensive fix for all concurrency token columns

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
        'DepartmentMemberships', 'Departments', 'CustomRates', 'CostCentres',
        'Companies', 'Buildings', 'BuildingTypes', 'BuildingContacts', 'BuildingBlocks',
        'BillingRatecards', 'BackgroundJobs', 'AuditOverrides', 'AssetTypes', 'Assets',
        'AssetDisposals'
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
        
        -- Check and add RowVersion
        SELECT EXISTS (
            SELECT 1 FROM information_schema.columns c
            WHERE c.table_schema = 'public' 
            AND (c.table_name = tbl_name OR c.table_name = lower(tbl_name))
            AND c.column_name = 'RowVersion'
        ) INTO col_exists;
        
        IF NOT col_exists THEN
            BEGIN
                EXECUTE format('ALTER TABLE "%s" ADD COLUMN "RowVersion" bytea', tbl_name);
                RAISE NOTICE 'Added RowVersion to %', tbl_name;
            EXCEPTION WHEN OTHERS THEN
                RAISE NOTICE 'Failed to add RowVersion to %: %', tbl_name, SQLERRM;
            END;
        ELSE
            RAISE NOTICE 'RowVersion already exists in %', tbl_name;
        END IF;
    END LOOP;
END $$;

