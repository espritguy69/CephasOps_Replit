-- Migration: Update Parser Templates with Flexible Multi-Pattern Subjects
-- Date: 2025-12-16
-- Description: Updates parser templates to support flexible pattern matching with pipe-separated patterns (OR logic)
--              and adjusts priorities to ensure Excel parsers are checked before generic templates

DO $$
BEGIN
    -- TIME FTTH - Match FTTH, Activation, New Activation, Service Order, Work Order
    UPDATE "ParserTemplates" 
    SET "SubjectPattern" = 'FTTH|Activation|Service Order|New Activation|Work Order',
        "Priority" = 20,
        "ExpectedAttachmentTypes" = COALESCE("ExpectedAttachmentTypes", '.xls,.xlsx')
    WHERE "Code" = 'TIME_FTTH';

    -- TIME FTTO - Match FTTO, Activation, New Activation
    UPDATE "ParserTemplates" 
    SET "SubjectPattern" = 'FTTO|Activation|New Activation',
        "Priority" = 20,
        "ExpectedAttachmentTypes" = COALESCE("ExpectedAttachmentTypes", '.xls,.xlsx')
    WHERE "Code" = 'TIME_FTTO';

    -- TIME Modification - Match Modification, Relocation, Outdoor, Indoor, Activate Value Add
    UPDATE "ParserTemplates" 
    SET "SubjectPattern" = 'Modification|Relocation|Outdoor|Indoor|Activate Value Add',
        "Priority" = 18,
        "ExpectedAttachmentTypes" = COALESCE("ExpectedAttachmentTypes", '.xls,.xlsx')
    WHERE "Code" = 'TIME_MODIFICATION';

    -- TIME Modification Outdoor - Match Modification, Outdoor, Relocation
    UPDATE "ParserTemplates" 
    SET "SubjectPattern" = 'Modification|Outdoor|Relocation',
        "Priority" = 19,
        "ExpectedAttachmentTypes" = COALESCE("ExpectedAttachmentTypes", '.xls,.xlsx')
    WHERE "Code" = 'TIME_MOD_OUTDOOR';

    -- TIME Modification Indoor - Match Modification, Indoor, Relocation
    UPDATE "ParserTemplates" 
    SET "SubjectPattern" = 'Modification|Indoor|Relocation',
        "Priority" = 19,
        "ExpectedAttachmentTypes" = COALESCE("ExpectedAttachmentTypes", '.xls,.xlsx')
    WHERE "Code" = 'TIME_MOD_INDOOR';

    -- TIME Relocation
    UPDATE "ParserTemplates" 
    SET "SubjectPattern" = 'Relocation|Modification',
        "Priority" = 18,
        "ExpectedAttachmentTypes" = COALESCE("ExpectedAttachmentTypes", '.xls,.xlsx')
    WHERE "Code" = 'TIME_RELOCATION';

    -- TIME Activation (General) - Match Activation, Service Order, Work Order
    UPDATE "ParserTemplates" 
    SET "SubjectPattern" = 'Activation|Service Order|Work Order|New Activation',
        "Priority" = 15,
        "ExpectedAttachmentTypes" = COALESCE("ExpectedAttachmentTypes", '.xls,.xlsx')
    WHERE "Code" = 'TIME_ACTIVATION';

    -- TIME General Fallback - Match anything from TIME (lowest priority for Excel emails)
    UPDATE "ParserTemplates" 
    SET "SubjectPattern" = '*',
        "Priority" = 1
    WHERE "Code" = 'TIME_GENERAL';

    -- TIME-Digi HSBB - Match TIME-Digi, DIGI, HSBB, Activation, Rescheduled
    UPDATE "ParserTemplates" 
    SET "SubjectPattern" = 'TIME-Digi|DIGI|HSBB|Activation|Rescheduled|Work Order',
        "Priority" = 20,
        "ExpectedAttachmentTypes" = COALESCE("ExpectedAttachmentTypes", '.xls,.xlsx')
    WHERE "Code" = 'DIGI_HSBB';

    -- TIME-Celcom HSBB - Match TIME-Celcom, CELCOM, HSBB, Activation
    UPDATE "ParserTemplates" 
    SET "SubjectPattern" = 'TIME-Celcom|CELCOM|HSBB|Activation|Work Order',
        "Priority" = 20,
        "ExpectedAttachmentTypes" = COALESCE("ExpectedAttachmentTypes", '.xls,.xlsx')
    WHERE "Code" = 'CELCOM_HSBB';

    -- TIME Assurance (TTKT) - Match APPMT, TTKT, Assurance
    UPDATE "ParserTemplates" 
    SET "SubjectPattern" = 'APPMT|TTKT|Assurance',
        "Priority" = 15
    WHERE "Code" = 'TIME_ASSURANCE';

    -- Reschedule Approval - Lower priority, only match if subject starts with "Re:" or contains "Reschedule"
    -- This ensures it doesn't match before Excel parsers
    UPDATE "ParserTemplates" 
    SET "SubjectPattern" = 'Re:|Reschedule',
        "Priority" = 5
    WHERE "Code" = 'RESCHEDULE_APPROVAL';

    -- TIME Termination
    UPDATE "ParserTemplates" 
    SET "SubjectPattern" = 'Termination',
        "Priority" = 15,
        "ExpectedAttachmentTypes" = COALESCE("ExpectedAttachmentTypes", '.xls,.xlsx')
    WHERE "Code" = 'TIME_TERMINATION';

    RAISE NOTICE 'Parser templates updated with flexible multi-pattern subjects';
END $$;

