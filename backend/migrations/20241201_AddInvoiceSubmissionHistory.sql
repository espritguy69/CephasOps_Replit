-- Migration: Add InvoiceSubmissionHistory table for tracking all invoice submission attempts
-- Date: 2024-12-01
-- Purpose: Track complete history of invoice submissions to portals (MyInvois/e-Invoice)
--          This allows referencing past SubmissionIds for payment status tracking

-- Create InvoiceSubmissionHistory table
CREATE TABLE IF NOT EXISTS "InvoiceSubmissionHistory" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "CompanyId" UUID NOT NULL,
    "InvoiceId" UUID NOT NULL,
    "SubmissionId" VARCHAR(200) NOT NULL,
    "SubmittedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "Status" VARCHAR(50) NOT NULL DEFAULT 'Submitted',
    "ResponseMessage" VARCHAR(1000),
    "ResponseCode" VARCHAR(50),
    "RejectionReason" VARCHAR(500),
    "PortalType" VARCHAR(50) NOT NULL DEFAULT 'MyInvois',
    "SubmittedByUserId" UUID NOT NULL,
    "IsActive" BOOLEAN NOT NULL DEFAULT true,
    "PaymentStatus" VARCHAR(50),
    "PaymentReference" VARCHAR(200),
    "Notes" VARCHAR(1000),
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "IsDeleted" BOOLEAN NOT NULL DEFAULT false,
    "DeletedAt" TIMESTAMP,
    "CreatedByUserId" UUID NOT NULL,
    "UpdatedByUserId" UUID,
    
    CONSTRAINT "FK_InvoiceSubmissionHistory_Company" 
        FOREIGN KEY ("CompanyId") REFERENCES "Companies"("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_InvoiceSubmissionHistory_Invoice" 
        FOREIGN KEY ("InvoiceId") REFERENCES "Invoices"("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_InvoiceSubmissionHistory_User" 
        FOREIGN KEY ("SubmittedByUserId") REFERENCES "Users"("Id")
);

-- Create indexes
CREATE INDEX IF NOT EXISTS "IX_InvoiceSubmissionHistory_InvoiceId" 
    ON "InvoiceSubmissionHistory" ("InvoiceId");

CREATE INDEX IF NOT EXISTS "IX_InvoiceSubmissionHistory_SubmissionId" 
    ON "InvoiceSubmissionHistory" ("SubmissionId");

CREATE INDEX IF NOT EXISTS "IX_InvoiceSubmissionHistory_CompanyId_InvoiceId_IsActive" 
    ON "InvoiceSubmissionHistory" ("CompanyId", "InvoiceId", "IsActive");

CREATE INDEX IF NOT EXISTS "IX_InvoiceSubmissionHistory_CompanyId_Status_SubmittedAt" 
    ON "InvoiceSubmissionHistory" ("CompanyId", "Status", "SubmittedAt");

-- Add comment
COMMENT ON TABLE "InvoiceSubmissionHistory" IS 'Tracks all invoice submission attempts to portals (MyInvois/e-Invoice). Each submission gets a unique SubmissionId that can be referenced for payment status.';
COMMENT ON COLUMN "InvoiceSubmissionHistory"."SubmissionId" IS 'Submission ID from portal (MyInvois/e-Invoice)';
COMMENT ON COLUMN "InvoiceSubmissionHistory"."IsActive" IS 'Whether this is the current active submission for the invoice';
COMMENT ON COLUMN "InvoiceSubmissionHistory"."PaymentStatus" IS 'Payment status for this submission (if tracked by portal)';

