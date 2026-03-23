-- Fix RowVersion columns for Supabase database
-- Adds RowVersion bytea columns for optimistic concurrency

DO $$ 
DECLARE
    table_names text[] := ARRAY[
        'Orders', 'Invoices', 'Splitters', 'Materials', 'Departments'
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

-- Mark the migration as applied if not already
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
SELECT '20251202174940_AddRowVersionConcurrencyTokens', '10.0.0'
WHERE NOT EXISTS (
    SELECT 1 FROM "__EFMigrationsHistory" 
    WHERE "MigrationId" = '20251202174940_AddRowVersionConcurrencyTokens'
);

