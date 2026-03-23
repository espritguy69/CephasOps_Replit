-- Migration: Add email viewer features (TTL, attachments)
-- Date: 2025-12-17
-- Description: Adds ExpiresAt to EmailMessages, creates EmailAttachments table for mail viewer

DO $$
BEGIN
    -- Add ExpiresAt column to EmailMessages
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'EmailMessages' AND column_name = 'ExpiresAt') THEN
        ALTER TABLE "EmailMessages" ADD COLUMN "ExpiresAt" timestamp with time zone NOT NULL DEFAULT (NOW() + INTERVAL '48 hours');
        RAISE NOTICE 'Added ExpiresAt column to EmailMessages';
    END IF;

    -- Update existing emails to have ExpiresAt = CreatedAt + 48 hours
    UPDATE "EmailMessages"
    SET "ExpiresAt" = "CreatedAt" + INTERVAL '48 hours'
    WHERE "ExpiresAt" IS NULL OR "ExpiresAt" = (NOW() + INTERVAL '48 hours');

    -- Create EmailAttachments table
    IF NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'EmailAttachments') THEN
        CREATE TABLE "EmailAttachments" (
            "Id" UUID NOT NULL PRIMARY KEY DEFAULT gen_random_uuid(),
            "CompanyId" UUID NULL,
            "EmailMessageId" UUID NOT NULL,
            "FileName" character varying(500) NOT NULL,
            "ContentType" character varying(255) NOT NULL DEFAULT 'application/octet-stream',
            "SizeBytes" bigint NOT NULL,
            "StoragePath" character varying(1000) NOT NULL,
            "FileId" UUID NULL,
            "IsInline" boolean NOT NULL DEFAULT false,
            "ContentId" character varying(255) NULL,
            "ExpiresAt" timestamp with time zone NOT NULL,
            "CreatedAt" timestamp with time zone NOT NULL DEFAULT NOW(),
            "UpdatedAt" timestamp with time zone NOT NULL DEFAULT NOW(),
            "IsDeleted" boolean NOT NULL DEFAULT false,
            "DeletedAt" timestamp with time zone NULL,
            "RowVersion" bytea NULL
        );

        -- Foreign key to EmailMessages
        ALTER TABLE "EmailAttachments"
        ADD CONSTRAINT "FK_EmailAttachments_EmailMessages_EmailMessageId"
        FOREIGN KEY ("EmailMessageId")
        REFERENCES "EmailMessages" ("Id")
        ON DELETE CASCADE;

        -- Indexes
        CREATE INDEX "IX_EmailAttachments_ExpiresAt_CompanyId" ON "EmailAttachments" ("ExpiresAt", "CompanyId");
        CREATE INDEX "IX_EmailAttachments_EmailMessageId_CompanyId" ON "EmailAttachments" ("EmailMessageId", "CompanyId");

        RAISE NOTICE 'Created EmailAttachments table';
    END IF;

    -- Add index for EmailMessages.ExpiresAt if not exists
    IF NOT EXISTS (
        SELECT 1 FROM pg_indexes 
        WHERE tablename = 'EmailMessages' 
        AND indexname = 'IX_EmailMessages_ExpiresAt_CompanyId'
    ) THEN
        CREATE INDEX "IX_EmailMessages_ExpiresAt_CompanyId" ON "EmailMessages" ("ExpiresAt", "CompanyId");
        RAISE NOTICE 'Created index IX_EmailMessages_ExpiresAt_CompanyId';
    END IF;
END $$;

