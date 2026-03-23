-- Add DefaultParserTemplateId column to EmailAccounts table if it doesn't exist
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 
        FROM information_schema.columns 
        WHERE table_name = 'EmailAccounts' 
        AND column_name = 'DefaultParserTemplateId'
    ) THEN
        ALTER TABLE "EmailAccounts"
        ADD COLUMN "DefaultParserTemplateId" uuid NULL;

        CREATE INDEX IF NOT EXISTS "IX_EmailAccounts_DefaultParserTemplateId" 
        ON "EmailAccounts" ("DefaultParserTemplateId");

        ALTER TABLE "EmailAccounts"
        ADD CONSTRAINT "FK_EmailAccounts_ParserTemplates_DefaultParserTemplateId"
        FOREIGN KEY ("DefaultParserTemplateId")
        REFERENCES "ParserTemplates" ("Id")
        ON DELETE SET NULL;
    END IF;
END $$;

