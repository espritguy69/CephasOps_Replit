-- Add TemplateFileId to DocumentTemplates

DO $$ 
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_schema = 'public' 
        AND table_name = 'DocumentTemplates'
        AND column_name = 'TemplateFileId'
    ) THEN
        ALTER TABLE "DocumentTemplates" ADD COLUMN "TemplateFileId" uuid;
        RAISE NOTICE 'Added TemplateFileId to DocumentTemplates';
    ELSE
        RAISE NOTICE 'TemplateFileId already exists in DocumentTemplates';
    END IF;
END $$;

-- Mark migration as applied
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
SELECT '20251203004645_AddTemplateFileIdToDocumentTemplate', '10.0.0'
WHERE NOT EXISTS (
    SELECT 1 FROM "__EFMigrationsHistory" 
    WHERE "MigrationId" = '20251203004645_AddTemplateFileIdToDocumentTemplate'
);

