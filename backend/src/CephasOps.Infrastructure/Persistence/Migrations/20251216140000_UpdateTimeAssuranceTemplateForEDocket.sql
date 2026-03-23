-- Migration: Update TIME Assurance Template for eDocket Emails
-- Date: 2025-12-16
-- Description: Updates TIME Assurance template to match eDocket emails with PDF attachments
--              eDocket emails have subject like "Work Order - AWO443365 - Service Order Approved"
--              and contain PDF attachments with completed assurance work order details

DO $$
BEGIN
    -- Update TIME Assurance template to match:
    -- - APPMT, TTKT, Assurance (existing)
    -- - Work Order (eDocket emails)
    -- - AWO (Assurance Work Order numbers)
    -- - Service Order Approved (completion notifications)
    -- - MRA (Material Return Advice for assurance)
    UPDATE "ParserTemplates" 
    SET "SubjectPattern" = 'APPMT|TTKT|Assurance|Work Order|AWO|Service Order Approved|eDocket|MRA|Material Return',
        "Priority" = 16,
        "ExpectedAttachmentTypes" = COALESCE("ExpectedAttachmentTypes", '.pdf'),
        "OrderTypeCode" = COALESCE("OrderTypeCode", 'ASSURANCE')
    WHERE "Code" = 'TIME_ASSURANCE';

    RAISE NOTICE 'TIME Assurance template updated for eDocket emails with PDF attachments';
END $$;

