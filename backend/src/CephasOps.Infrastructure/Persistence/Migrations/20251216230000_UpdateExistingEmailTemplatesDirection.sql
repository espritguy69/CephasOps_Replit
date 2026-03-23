-- Migration: Update existing EmailTemplates to set Direction="Outgoing"
-- Date: 2025-12-16
-- Description: Sets Direction="Outgoing" for all existing EmailTemplates that don't have it set
--              These templates are for sending emails (admin/clerk replies), not parsing incoming emails

DO $$
BEGIN
    -- Update all EmailTemplates to "Outgoing" if Direction is NULL or empty
    -- EmailTemplates are for sending emails, not parsing incoming emails
    UPDATE "EmailTemplates" 
    SET "Direction" = 'Outgoing'
    WHERE "Direction" IS NULL OR "Direction" = '';
    
    RAISE NOTICE 'Updated EmailTemplates Direction to Outgoing';
    
    -- Specifically ensure these templates are Outgoing (in case they were created before Direction field)
    UPDATE "EmailTemplates"
    SET "Direction" = 'Outgoing'
    WHERE "Code" IN (
        'RESCHEDULE_TIME_ONLY',
        'RESCHEDULE_DATE_TIME',
        'ASSURANCE_CABLE_REPULL',
        'TIME_CUSTOMER_UNCONTACTABLE',
        'TIME_RESCHEDULE_NOTIFICATION'
    );
    
    RAISE NOTICE 'Verified specific EmailTemplates have Direction=Outgoing';
    
    -- Show summary
    RAISE NOTICE 'Total EmailTemplates with Direction=Outgoing: %', 
        (SELECT COUNT(*) FROM "EmailTemplates" WHERE "Direction" = 'Outgoing');
END $$;

