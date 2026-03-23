-- Migration: Remove TIME_MODIFICATION template and make modification templates more flexible
-- Date: 2025-12-16
-- Description: Removes TIME_MODIFICATION template (keep TIME_MOD_INDOOR and TIME_MOD_OUTDOOR)
--              and updates modification templates with more flexible subject patterns

DO $$
BEGIN
    -- Remove TIME_MODIFICATION template (keep TIME_MOD_INDOOR and TIME_MOD_OUTDOOR)
    DELETE FROM "ParserTemplates" WHERE "Code" = 'TIME_MODIFICATION';
    RAISE NOTICE 'Removed TIME_MODIFICATION template';

    -- Make TIME_MOD_INDOOR more flexible - match any modification/relocation that's indoor
    UPDATE "ParserTemplates" 
    SET "SubjectPattern" = 'Modification|Relocation|Indoor|Move within|Room to room|Kitchen|Hall|Bedroom|Activate Value Add',
        "Priority" = 19,
        "ExpectedAttachmentTypes" = COALESCE("ExpectedAttachmentTypes", '.xls,.xlsx')
    WHERE "Code" = 'TIME_MOD_INDOOR';

    -- Make TIME_MOD_OUTDOOR more flexible - match any modification/relocation that's outdoor
    UPDATE "ParserTemplates" 
    SET "SubjectPattern" = 'Modification|Relocation|Outdoor|Old Address|New Address|Different address',
        "Priority" = 19,
        "ExpectedAttachmentTypes" = COALESCE("ExpectedAttachmentTypes", '.xls,.xlsx')
    WHERE "Code" = 'TIME_MOD_OUTDOOR';

    RAISE NOTICE 'Updated modification templates with flexible patterns';
END $$;

