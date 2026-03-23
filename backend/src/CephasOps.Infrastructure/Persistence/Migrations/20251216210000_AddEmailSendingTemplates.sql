-- Migration: Add email sending templates for Customer Uncontactable and Reschedule notifications
-- Date: 2025-12-16
-- Description: Creates email templates for sending Customer Uncontactable and Reschedule notifications

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

    -- Check if Customer Uncontactable email template exists
    SELECT "Id" INTO v_template_id 
    FROM "EmailTemplates" 
    WHERE "Code" = 'TIME_CUSTOMER_UNCONTACTABLE';

    IF v_template_id IS NULL THEN
        INSERT INTO "EmailTemplates" (
            "Id", "CompanyId", "Name", "Code", "SubjectTemplate", "BodyTemplate",
            "RelatedEntityType", "Priority", "IsActive", "AutoProcessReplies", "CreatedByUserId", "CreatedAt", "UpdatedAt"
        ) VALUES (
            gen_random_uuid(),
            v_company_id,
            'Customer Uncontactable Notification',
            'TIME_CUSTOMER_UNCONTACTABLE',
            'Customer Uncontactable - {ServiceId} - {CustomerName}',
            '<html><body>
                <h2>Customer Uncontactable Notification</h2>
                <p>Dear Team,</p>
                <p>Customer is uncontactable, kindly provide alternate number</p>
                <ul>
                    <li><strong>Service ID:</strong> {ServiceId}</li>
                    <li><strong>Customer Name:</strong> {CustomerName}</li>
                    <li><strong>Date:</strong> {Date}</li>
                    <li><strong>Time:</strong> {Time}</li>
                </ul>
                <p>Regards,<br/>{SenderName}<br/>{SenderPhone}</p>
            </body></html>',
            'Order',
            10,
            true,
            false,
            '00000000-0000-0000-0000-000000000000'::UUID,
            NOW(),
            NOW()
        );
        RAISE NOTICE 'Created TIME_CUSTOMER_UNCONTACTABLE email template';
    ELSE
        UPDATE "EmailTemplates"
        SET "SubjectTemplate" = 'Customer Uncontactable - {ServiceId} - {CustomerName}',
            "BodyTemplate" = '<html><body>
                <h2>Customer Uncontactable Notification</h2>
                <p>Dear Team,</p>
                <p>Customer is uncontactable, kindly provide alternate number</p>
                <ul>
                    <li><strong>Service ID:</strong> {ServiceId}</li>
                    <li><strong>Customer Name:</strong> {CustomerName}</li>
                    <li><strong>Date:</strong> {Date}</li>
                    <li><strong>Time:</strong> {Time}</li>
                </ul>
                <p>Regards,<br/>{SenderName}<br/>{SenderPhone}</p>
            </body></html>',
            "UpdatedAt" = NOW()
        WHERE "Id" = v_template_id;
        RAISE NOTICE 'Updated TIME_CUSTOMER_UNCONTACTABLE email template';
    END IF;

    -- Check if Reschedule email template exists
    SELECT "Id" INTO v_template_id 
    FROM "EmailTemplates" 
    WHERE "Code" = 'TIME_RESCHEDULE_NOTIFICATION';

    IF v_template_id IS NULL THEN
        INSERT INTO "EmailTemplates" (
            "Id", "CompanyId", "Name", "Code", "SubjectTemplate", "BodyTemplate",
            "RelatedEntityType", "Priority", "IsActive", "AutoProcessReplies", "CreatedByUserId", "CreatedAt", "UpdatedAt"
        ) VALUES (
            gen_random_uuid(),
            v_company_id,
            'TIME Reschedule Notification',
            'TIME_RESCHEDULE_NOTIFICATION',
            'Reschedule {OrderType} - {CustomerName}',
            '<html><body>
                <h2>Appointment Reschedule Notification</h2>
                <p>Dear Team,</p>
                <p>Please be informed that the appointment for the following order has been rescheduled:</p>
                <ul>
                    <li><strong>Service ID:</strong> {ServiceId}</li>
                    <li><strong>Customer Name:</strong> {CustomerName}</li>
                    <li><strong>Original Date:</strong> {OriginalDate}</li>
                    <li><strong>Original Time:</strong> {OriginalTime}</li>
                    <li><strong>New Date:</strong> {NewDate}</li>
                    <li><strong>New Time:</strong> {NewTime}</li>
                    <li><strong>Reason:</strong> {Reason}</li>
                </ul>
                <p>Please update your records accordingly.</p>
            </body></html>',
            'Order',
            10,
            true,
            false,
            '00000000-0000-0000-0000-000000000000'::UUID,
            NOW(),
            NOW()
        );
        RAISE NOTICE 'Created TIME_RESCHEDULE_NOTIFICATION email template';
    ELSE
        UPDATE "EmailTemplates"
        SET "SubjectTemplate" = 'Reschedule {OrderType} - {CustomerName}',
            "BodyTemplate" = '<html><body>
                <h2>Appointment Reschedule Notification</h2>
                <p>Dear Team,</p>
                <p>Please be informed that the appointment for the following order has been rescheduled:</p>
                <ul>
                    <li><strong>Service ID:</strong> {ServiceId}</li>
                    <li><strong>Customer Name:</strong> {CustomerName}</li>
                    <li><strong>Original Date:</strong> {OriginalDate}</li>
                    <li><strong>Original Time:</strong> {OriginalTime}</li>
                    <li><strong>New Date:</strong> {NewDate}</li>
                    <li><strong>New Time:</strong> {NewTime}</li>
                    <li><strong>Reason:</strong> {Reason}</li>
                </ul>
                <p>Please update your records accordingly.</p>
            </body></html>',
            "UpdatedAt" = NOW()
        WHERE "Id" = v_template_id;
        RAISE NOTICE 'Updated TIME_RESCHEDULE_NOTIFICATION email template';
    END IF;
END $$;

