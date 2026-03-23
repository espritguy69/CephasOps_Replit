-- Migration: Add EmailTemplates table and update EmailMessages for Direction support
-- Date: 2024-12-01

-- Create EmailTemplates table
CREATE TABLE IF NOT EXISTS "EmailTemplates" (
    "Id" uuid NOT NULL,
    "CompanyId" uuid NOT NULL,
    "Name" character varying(200) NOT NULL,
    "Code" character varying(50) NOT NULL,
    "EmailAccountId" uuid NULL,
    "SubjectTemplate" character varying(500) NOT NULL,
    "BodyTemplate" text NOT NULL,
    "DepartmentId" uuid NULL,
    "RelatedEntityType" character varying(50) NULL,
    "Priority" integer NOT NULL DEFAULT 0,
    "IsActive" boolean NOT NULL DEFAULT true,
    "AutoProcessReplies" boolean NOT NULL DEFAULT false,
    "ReplyPattern" character varying(200) NULL,
    "Description" character varying(1000) NULL,
    "CreatedByUserId" uuid NOT NULL,
    "UpdatedByUserId" uuid NULL,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT (now()),
    "UpdatedAt" timestamp with time zone NOT NULL DEFAULT (now()),
    CONSTRAINT "PK_EmailTemplates" PRIMARY KEY ("Id")
);

-- Create indexes
CREATE UNIQUE INDEX IF NOT EXISTS "IX_EmailTemplates_CompanyId_Code" ON "EmailTemplates" ("CompanyId", "Code");
CREATE INDEX IF NOT EXISTS "IX_EmailTemplates_CompanyId_Priority_IsActive" ON "EmailTemplates" ("CompanyId", "Priority", "IsActive");
CREATE INDEX IF NOT EXISTS "IX_EmailTemplates_CompanyId_DepartmentId_IsActive" ON "EmailTemplates" ("CompanyId", "DepartmentId", "IsActive");

-- Add Direction and SentAt columns to EmailMessages if they don't exist
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'EmailMessages' AND column_name = 'Direction'
    ) THEN
        ALTER TABLE "EmailMessages" ADD COLUMN "Direction" character varying(20) NOT NULL DEFAULT 'Inbound';
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'EmailMessages' AND column_name = 'SentAt'
    ) THEN
        ALTER TABLE "EmailMessages" ADD COLUMN "SentAt" timestamp with time zone NULL;
    END IF;
END $$;

-- Create index for Direction
CREATE INDEX IF NOT EXISTS "IX_EmailMessages_CompanyId_Direction_ReceivedAt" ON "EmailMessages" ("CompanyId", "Direction", "ReceivedAt");

-- Insert 3 initial email templates
-- Note: Replace {COMPANY_ID} and {USER_ID} with actual values, or use NULL for CompanyId in single-company mode

-- Template 1: Same Day Time Change (Time Only)
INSERT INTO "EmailTemplates" (
    "Id", "CompanyId", "Name", "Code", "SubjectTemplate", "BodyTemplate",
    "Priority", "IsActive", "AutoProcessReplies", "RelatedEntityType", "Description",
    "CreatedByUserId", "CreatedAt", "UpdatedAt"
) VALUES (
    gen_random_uuid(),
    NULL, -- Single company mode
    'Same Day Time Change Request',
    'RESCHEDULE_TIME_ONLY',
    'Time Change Request - Order {OrderNumber}',
    '<html>
<body>
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
</body>
</html>',
    10,
    true,
    false,
    'Order',
    'Template for requesting same-day time changes from customers. Used to audit SI requests.',
    '00000000-0000-0000-0000-000000000000', -- Replace with actual user ID
    now(),
    now()
) ON CONFLICT ("CompanyId", "Code") DO NOTHING;

-- Template 2: Date and Time Change (Requires TIME Approval)
INSERT INTO "EmailTemplates" (
    "Id", "CompanyId", "Name", "Code", "SubjectTemplate", "BodyTemplate",
    "Priority", "IsActive", "AutoProcessReplies", "ReplyPattern", "RelatedEntityType", "Description",
    "CreatedByUserId", "CreatedAt", "UpdatedAt"
) VALUES (
    gen_random_uuid(),
    NULL, -- Single company mode
    'Date and Time Change Approval Request',
    'RESCHEDULE_DATE_TIME',
    'Reschedule Approval Request - Order {OrderNumber}',
    '<html>
<body>
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
</body>
</html>',
    20,
    true,
    true, -- Auto-process replies
    'Approved', -- Reply pattern for auto-processing
    'Order',
    'Template for requesting reschedule approval from TIME Internet. Auto-processes "Approved" replies.',
    '00000000-0000-0000-0000-000000000000', -- Replace with actual user ID
    now(),
    now()
) ON CONFLICT ("CompanyId", "Code") DO NOTHING;

-- Template 3: Assurance Cable Repull (Requires FE Approval)
INSERT INTO "EmailTemplates" (
    "Id", "CompanyId", "Name", "Code", "SubjectTemplate", "BodyTemplate",
    "Priority", "IsActive", "AutoProcessReplies", "ReplyPattern", "RelatedEntityType", "Description",
    "CreatedByUserId", "CreatedAt", "UpdatedAt"
) VALUES (
    gen_random_uuid(),
    NULL, -- Single company mode
    'Assurance Cable Repull Approval Request',
    'ASSURANCE_CABLE_REPULL',
    'Cable Repull Approval - Assurance Order {OrderNumber}',
    '<html>
<body>
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
</body>
</html>',
    30,
    true,
    true, -- Auto-process replies
    'Approved', -- Reply pattern for auto-processing
    'Order',
    'Template for requesting FE approval to repull broken cable. Requires photo attachments. Auto-processes "Approved" replies.',
    '00000000-0000-0000-0000-000000000000', -- Replace with actual user ID
    now(),
    now()
) ON CONFLICT ("CompanyId", "Code") DO NOTHING;

