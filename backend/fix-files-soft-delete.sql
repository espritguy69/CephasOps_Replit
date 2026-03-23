-- Add soft delete columns to Files table
DO $$
BEGIN
    -- Add DeletedAt if not exists
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'public'
        AND table_name = 'files'
        AND column_name = 'DeletedAt'
    ) THEN
        ALTER TABLE "Files" ADD COLUMN "DeletedAt" timestamp with time zone;
        RAISE NOTICE 'Added DeletedAt to Files';
    ELSE
        RAISE NOTICE 'DeletedAt already exists in Files';
    END IF;

    -- Add DeletedByUserId if not exists
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'public'
        AND table_name = 'files'
        AND column_name = 'DeletedByUserId'
    ) THEN
        ALTER TABLE "Files" ADD COLUMN "DeletedByUserId" uuid;
        RAISE NOTICE 'Added DeletedByUserId to Files';
    ELSE
        RAISE NOTICE 'DeletedByUserId already exists in Files';
    END IF;

    -- Add IsDeleted if not exists
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'public'
        AND table_name = 'files'
        AND column_name = 'IsDeleted'
    ) THEN
        ALTER TABLE "Files" ADD COLUMN "IsDeleted" boolean NOT NULL DEFAULT false;
        RAISE NOTICE 'Added IsDeleted to Files';
    ELSE
        RAISE NOTICE 'IsDeleted already exists in Files';
    END IF;
END $$;

