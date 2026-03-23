-- Migration: Ensure reschedule EmailTemplates exist with Direction="Outgoing"
-- Date: 2025-12-16
-- Description: Creates or updates reschedule email templates (RESCHEDULE_TIME_ONLY, RESCHEDULE_DATE_TIME, ASSURANCE_CABLE_REPULL)
--              These are Outgoing templates for admin/clerk to send reschedule requests

DO $$
DECLARE
    v_company_id UUID;
    v_template_id UUID;
BEGIN
    -- Get the first company ID (single company mode)
    SELECT "Id" INTO v_company_id FROM "Companies" LIMIT 1;
    
    IF v_company_id IS NULL THEN
        RAISE NOTICE 'No company found, skipping template creation';
        RETURN;
    END IF;

    -- Template 1: RESCHEDULE_TIME_ONLY
    SELECT "Id" INTO v_template_id 
    FROM "EmailTemplates" 
    WHERE "Code" = 'RESCHEDULE_TIME_ONLY';

    IF v_template_id IS NULL THEN
        INSERT INTO "EmailTemplates" (
            "Id", "CompanyId", "Name", "Code", "SubjectTemplate", "BodyTemplate",
            "RelatedEntityType", "Priority", "IsActive", "AutoProcessReplies", "Direction",
            "CreatedByUserId", "CreatedAt", "UpdatedAt"
        ) VALUES (
            gen_random_uuid(),
            v_company_id,
            'Same Day Time Change Request',
            'RESCHEDULE_TIME_ONLY',
            'Time Change Request - Order {OrderNumber}',
            '<html><body>
                <h2>Appointment Time Change Request</h2>
                <p>Dear {CustomerName},</p>
                <p>We need to change your appointment time on <strong>{OldDate}</strong>.</p>
                <h3>Current Appointment:</h3>
                <ul>
                    <li>Date: {OldDate}</li>
                    <li>Time: {OldTime}</li>
                </ul>
                <h3>Proposed New Time:</h3>
                <ul>
                    <li>Date: {OldDate} (same day)</li>
                    <li>Time: {NewTime}</li>
                </ul>
                <p><strong>Reason:</strong> {Reason}</p>
                <p>Please reply to this email to confirm the new time or suggest an alternative.</p>
                <p>Thank you,<br/>CephasOps Team</p>
            </body></html>',
            'Order',
            10,
            true,
            false,
            'Outgoing',
            '00000000-0000-0000-0000-000000000000'::UUID,
            NOW(),
            NOW()
        );
        RAISE NOTICE 'Created RESCHEDULE_TIME_ONLY email template';
    ELSE
        UPDATE "EmailTemplates"
        SET "Direction" = 'Outgoing',
            "UpdatedAt" = NOW()
        WHERE "Id" = v_template_id;
        RAISE NOTICE 'Updated RESCHEDULE_TIME_ONLY email template Direction';
    END IF;

    -- Template 2: RESCHEDULE_DATE_TIME
    SELECT "Id" INTO v_template_id 
    FROM "EmailTemplates" 
    WHERE "Code" = 'RESCHEDULE_DATE_TIME';

    IF v_template_id IS NULL THEN
        INSERT INTO "EmailTemplates" (
            "Id", "CompanyId", "Name", "Code", "SubjectTemplate", "BodyTemplate",
            "RelatedEntityType", "Priority", "IsActive", "AutoProcessReplies", "ReplyPattern", "Direction",
            "CreatedByUserId", "CreatedAt", "UpdatedAt"
        ) VALUES (
            gen_random_uuid(),
            v_company_id,
            'Date and Time Change Approval Request',
            'RESCHEDULE_DATE_TIME',
            'Reschedule Approval Request - Order {OrderNumber}',
            '<html><body>
                <h2>Reschedule Approval Request</h2>
                <p>Dear TIME Team,</p>
                <p>Customer <strong>{CustomerName}</strong> has requested to reschedule their appointment for order <strong>{OrderNumber}</strong>.</p>
                <h3>Current Appointment:</h3>
                <ul>
                    <li>Date: {OldDate}</li>
                    <li>Time: {OldTime}</li>
                </ul>
                <h3>Requested New Appointment:</h3>
                <ul>
                    <li>Date: {NewDate}</li>
                    <li>Time: {NewTime}</li>
                </ul>
                <p><strong>Reason:</strong> {Reason}</p>
                <p><strong>Customer Address:</strong> {Address}</p>
                <p>Please reply with "Approved" to confirm or provide alternative date/time.</p>
                <p>Thank you,<br/>CephasOps Team</p>
            </body></html>',
            'Order',
            20,
            true,
            true,
            'Approved',
            'Outgoing',
            '00000000-0000-0000-0000-000000000000'::UUID,
            NOW(),
            NOW()
        );
        RAISE NOTICE 'Created RESCHEDULE_DATE_TIME email template';
    ELSE
        UPDATE "EmailTemplates"
        SET "Direction" = 'Outgoing',
            "UpdatedAt" = NOW()
        WHERE "Id" = v_template_id;
        RAISE NOTICE 'Updated RESCHEDULE_DATE_TIME email template Direction';
    END IF;

    -- Template 3: ASSURANCE_CABLE_REPULL
    SELECT "Id" INTO v_template_id 
    FROM "EmailTemplates" 
    WHERE "Code" = 'ASSURANCE_CABLE_REPULL';

    IF v_template_id IS NULL THEN
        INSERT INTO "EmailTemplates" (
            "Id", "CompanyId", "Name", "Code", "SubjectTemplate", "BodyTemplate",
            "RelatedEntityType", "Priority", "IsActive", "AutoProcessReplies", "ReplyPattern", "Direction",
            "CreatedByUserId", "CreatedAt", "UpdatedAt"
        ) VALUES (
            gen_random_uuid(),
            v_company_id,
            'Assurance Cable Repull Approval Request',
            'ASSURANCE_CABLE_REPULL',
            'Cable Repull Approval - Assurance Order {OrderNumber}',
            '<html><body>
                <h2>Cable Repull Approval Request</h2>
                <p>Dear FE Team,</p>
                <p>Fiber cable is broken outside customer premises for assurance order <strong>{OrderNumber}</strong>.</p>
                <h3>Order Details:</h3>
                <ul>
                    <li>Order Number: {OrderNumber}</li>
                    <li>Customer: {CustomerName}</li>
                    <li>Address: {Address}</li>
                </ul>
                <h3>Issue:</h3>
                <p>Cable broken outside customer premises. Photos attached as proof.</p>
                <p><strong>Reason:</strong> {Reason}</p>
                <p>Please review the attached photos and reply with "Approved" to proceed with cable repull and reschedule the order.</p>
                <p>Thank you,<br/>CephasOps Team</p>
            </body></html>',
            'Order',
            30,
            true,
            true,
            'Approved',
            'Outgoing',
            '00000000-0000-0000-0000-000000000000'::UUID,
            NOW(),
            NOW()
        );
        RAISE NOTICE 'Created ASSURANCE_CABLE_REPULL email template';
    ELSE
        UPDATE "EmailTemplates"
        SET "Direction" = 'Outgoing',
            "UpdatedAt" = NOW()
        WHERE "Id" = v_template_id;
        RAISE NOTICE 'Updated ASSURANCE_CABLE_REPULL email template Direction';
    END IF;
END $$;

