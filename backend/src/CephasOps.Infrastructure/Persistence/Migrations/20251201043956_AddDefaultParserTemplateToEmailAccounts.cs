using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CephasOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDefaultParserTemplateToEmailAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add DepartmentId to VipEmails if it doesn't exist
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'VipEmails' 
                        AND column_name = 'DepartmentId'
                    ) THEN
                        ALTER TABLE ""VipEmails"" ADD COLUMN ""DepartmentId"" uuid NULL;
                    END IF;
                END $$;
            ");

            // Add DepartmentId to EmailMessages if it doesn't exist
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'EmailMessages' 
                        AND column_name = 'DepartmentId'
                    ) THEN
                        ALTER TABLE ""EmailMessages"" ADD COLUMN ""DepartmentId"" uuid NULL;
                    END IF;
                END $$;
            ");

            // Add Direction column to EmailMessages if it doesn't exist
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'EmailMessages' 
                        AND column_name = 'Direction'
                    ) THEN
                        ALTER TABLE ""EmailMessages"" ADD COLUMN ""Direction"" character varying(20) NOT NULL DEFAULT 'Inbound';
                    END IF;
                END $$;
            ");

            // Add SentAt column to EmailMessages if it doesn't exist
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'EmailMessages' 
                        AND column_name = 'SentAt'
                    ) THEN
                        ALTER TABLE ""EmailMessages"" ADD COLUMN ""SentAt"" timestamp with time zone NULL;
                    END IF;
                END $$;
            ");

            // Add foreign key constraints for DepartmentId if they don't exist
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    -- FK for VipEmails.DepartmentId
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.table_constraints 
                        WHERE constraint_name = 'FK_VipEmails_Departments_DepartmentId'
                    ) THEN
                        ALTER TABLE ""VipEmails"" 
                        ADD CONSTRAINT ""FK_VipEmails_Departments_DepartmentId"" 
                        FOREIGN KEY (""DepartmentId"") REFERENCES ""Departments""(""Id"") ON DELETE SET NULL;
                    END IF;

                    -- FK for EmailMessages.DepartmentId
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.table_constraints 
                        WHERE constraint_name = 'FK_EmailMessages_Departments_DepartmentId'
                    ) THEN
                        ALTER TABLE ""EmailMessages"" 
                        ADD CONSTRAINT ""FK_EmailMessages_Departments_DepartmentId"" 
                        FOREIGN KEY (""DepartmentId"") REFERENCES ""Departments""(""Id"") ON DELETE SET NULL;
                    END IF;
                END $$;
            ");

            // Create indexes for DepartmentId if they don't exist
            migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS ""IX_VipEmails_DepartmentId"" ON ""VipEmails"" (""DepartmentId"");");
            migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS ""IX_EmailMessages_DepartmentId"" ON ""EmailMessages"" (""DepartmentId"");");


            // Create EmailTemplates table only if it doesn't exist
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.tables 
                        WHERE table_name = 'EmailTemplates'
                    ) THEN
                        CREATE TABLE ""EmailTemplates"" (
                            ""Id"" uuid NOT NULL,
                            ""Name"" character varying(200) NOT NULL,
                            ""Code"" character varying(50) NOT NULL,
                            ""EmailAccountId"" uuid NULL,
                            ""SubjectTemplate"" character varying(500) NOT NULL,
                            ""BodyTemplate"" text NOT NULL,
                            ""DepartmentId"" uuid NULL,
                            ""RelatedEntityType"" character varying(50) NULL,
                            ""Priority"" integer NOT NULL,
                            ""IsActive"" boolean NOT NULL,
                            ""AutoProcessReplies"" boolean NOT NULL,
                            ""ReplyPattern"" character varying(200) NULL,
                            ""Description"" character varying(1000) NULL,
                            ""CreatedByUserId"" uuid NOT NULL,
                            ""UpdatedByUserId"" uuid NULL,
                            ""CompanyId"" uuid NULL,
                            ""CreatedAt"" timestamp with time zone NOT NULL,
                            ""UpdatedAt"" timestamp with time zone NOT NULL,
                            CONSTRAINT ""PK_EmailTemplates"" PRIMARY KEY (""Id"")
                        );
                    END IF;
                END $$;
            ");

            // Create InvoiceSubmissionHistory table only if it doesn't exist
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.tables 
                        WHERE table_name = 'InvoiceSubmissionHistory'
                    ) THEN
                        CREATE TABLE ""InvoiceSubmissionHistory"" (
                            ""Id"" uuid NOT NULL,
                            ""InvoiceId"" uuid NOT NULL,
                            ""SubmissionId"" character varying(200) NOT NULL,
                            ""SubmittedAt"" timestamp with time zone NOT NULL,
                            ""Status"" character varying(50) NOT NULL,
                            ""ResponseMessage"" character varying(1000) NULL,
                            ""ResponseCode"" character varying(50) NULL,
                            ""RejectionReason"" character varying(500) NULL,
                            ""PortalType"" character varying(50) NOT NULL DEFAULT 'MyInvois',
                            ""SubmittedByUserId"" uuid NOT NULL,
                            ""IsActive"" boolean NOT NULL,
                            ""PaymentStatus"" character varying(50) NULL,
                            ""PaymentReference"" character varying(200) NULL,
                            ""Notes"" character varying(1000) NULL,
                            ""CompanyId"" uuid NULL,
                            ""CreatedAt"" timestamp with time zone NOT NULL,
                            ""UpdatedAt"" timestamp with time zone NOT NULL,
                            CONSTRAINT ""PK_InvoiceSubmissionHistory"" PRIMARY KEY (""Id"")
                        );
                    END IF;
                END $$;
            ");

            // Create index for EmailMessages with Direction if it doesn't exist
            migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS ""IX_EmailMessages_CompanyId_Direction_ReceivedAt"" ON ""EmailMessages"" (""CompanyId"", ""Direction"", ""ReceivedAt"");");

            // Create indexes for EmailTemplates only if table exists
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM information_schema.tables 
                        WHERE table_name = 'EmailTemplates'
                    ) THEN
                        -- Create indexes only if they don't exist
                        IF NOT EXISTS (
                            SELECT 1 FROM pg_indexes 
                            WHERE tablename = 'EmailTemplates' 
                            AND indexname = 'IX_EmailTemplates_CompanyId_Code'
                        ) THEN
                            CREATE UNIQUE INDEX ""IX_EmailTemplates_CompanyId_Code"" 
                            ON ""EmailTemplates"" (""CompanyId"", ""Code"");
                        END IF;

                        IF NOT EXISTS (
                            SELECT 1 FROM pg_indexes 
                            WHERE tablename = 'EmailTemplates' 
                            AND indexname = 'IX_EmailTemplates_CompanyId_DepartmentId_IsActive'
                        ) THEN
                            CREATE INDEX ""IX_EmailTemplates_CompanyId_DepartmentId_IsActive"" 
                            ON ""EmailTemplates"" (""CompanyId"", ""DepartmentId"", ""IsActive"");
                        END IF;

                        IF NOT EXISTS (
                            SELECT 1 FROM pg_indexes 
                            WHERE tablename = 'EmailTemplates' 
                            AND indexname = 'IX_EmailTemplates_CompanyId_Priority_IsActive'
                        ) THEN
                            CREATE INDEX ""IX_EmailTemplates_CompanyId_Priority_IsActive"" 
                            ON ""EmailTemplates"" (""CompanyId"", ""Priority"", ""IsActive"");
                        END IF;
                    END IF;
                END $$;
            ");

            // Create indexes for InvoiceSubmissionHistory only if they don't exist
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM information_schema.tables 
                        WHERE table_name = 'InvoiceSubmissionHistory'
                    ) THEN
                        -- Create indexes only if they don't exist
                        IF NOT EXISTS (
                            SELECT 1 FROM pg_indexes 
                            WHERE tablename = 'InvoiceSubmissionHistory' 
                            AND indexname = 'IX_InvoiceSubmissionHistory_CompanyId_InvoiceId_IsActive'
                        ) THEN
                            CREATE INDEX ""IX_InvoiceSubmissionHistory_CompanyId_InvoiceId_IsActive"" 
                            ON ""InvoiceSubmissionHistory"" (""CompanyId"", ""InvoiceId"", ""IsActive"");
                        END IF;

                        IF NOT EXISTS (
                            SELECT 1 FROM pg_indexes 
                            WHERE tablename = 'InvoiceSubmissionHistory' 
                            AND indexname = 'IX_InvoiceSubmissionHistory_CompanyId_Status_SubmittedAt'
                        ) THEN
                            CREATE INDEX ""IX_InvoiceSubmissionHistory_CompanyId_Status_SubmittedAt"" 
                            ON ""InvoiceSubmissionHistory"" (""CompanyId"", ""Status"", ""SubmittedAt"");
                        END IF;

                        IF NOT EXISTS (
                            SELECT 1 FROM pg_indexes 
                            WHERE tablename = 'InvoiceSubmissionHistory' 
                            AND indexname = 'IX_InvoiceSubmissionHistory_InvoiceId'
                        ) THEN
                            CREATE INDEX ""IX_InvoiceSubmissionHistory_InvoiceId"" 
                            ON ""InvoiceSubmissionHistory"" (""InvoiceId"");
                        END IF;

                        IF NOT EXISTS (
                            SELECT 1 FROM pg_indexes 
                            WHERE tablename = 'InvoiceSubmissionHistory' 
                            AND indexname = 'IX_InvoiceSubmissionHistory_SubmissionId'
                        ) THEN
                            CREATE INDEX ""IX_InvoiceSubmissionHistory_SubmissionId"" 
                            ON ""InvoiceSubmissionHistory"" (""SubmissionId"");
                        END IF;
                    END IF;
                END $$;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop tables conditionally
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM information_schema.tables 
                        WHERE table_name = 'EmailTemplates'
                    ) THEN
                        DROP TABLE ""EmailTemplates"";
                    END IF;

                    IF EXISTS (
                        SELECT 1 FROM information_schema.tables 
                        WHERE table_name = 'InvoiceSubmissionHistory'
                    ) THEN
                        DROP TABLE ""InvoiceSubmissionHistory"";
                    END IF;
                END $$;
            ");

            // Drop indexes and columns conditionally
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    -- Drop index if exists
                    IF EXISTS (
                        SELECT 1 FROM pg_indexes 
                        WHERE indexname = 'IX_EmailMessages_CompanyId_Direction_ReceivedAt'
                    ) THEN
                        DROP INDEX ""IX_EmailMessages_CompanyId_Direction_ReceivedAt"";
                    END IF;

                    -- Drop DepartmentId indexes if they exist
                    IF EXISTS (
                        SELECT 1 FROM pg_indexes 
                        WHERE indexname = 'IX_VipEmails_DepartmentId'
                    ) THEN
                        DROP INDEX ""IX_VipEmails_DepartmentId"";
                    END IF;

                    IF EXISTS (
                        SELECT 1 FROM pg_indexes 
                        WHERE indexname = 'IX_EmailMessages_DepartmentId'
                    ) THEN
                        DROP INDEX ""IX_EmailMessages_DepartmentId"";
                    END IF;

                    -- Drop foreign keys if they exist
                    IF EXISTS (
                        SELECT 1 FROM information_schema.table_constraints 
                        WHERE constraint_name = 'FK_VipEmails_Departments_DepartmentId'
                    ) THEN
                        ALTER TABLE ""VipEmails"" DROP CONSTRAINT ""FK_VipEmails_Departments_DepartmentId"";
                    END IF;

                    IF EXISTS (
                        SELECT 1 FROM information_schema.table_constraints 
                        WHERE constraint_name = 'FK_EmailMessages_Departments_DepartmentId'
                    ) THEN
                        ALTER TABLE ""EmailMessages"" DROP CONSTRAINT ""FK_EmailMessages_Departments_DepartmentId"";
                    END IF;

                    -- Drop columns if they exist
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'VipEmails' AND column_name = 'DepartmentId'
                    ) THEN
                        ALTER TABLE ""VipEmails"" DROP COLUMN ""DepartmentId"";
                    END IF;

                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'EmailMessages' AND column_name = 'DepartmentId'
                    ) THEN
                        ALTER TABLE ""EmailMessages"" DROP COLUMN ""DepartmentId"";
                    END IF;

                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'EmailMessages' AND column_name = 'Direction'
                    ) THEN
                        ALTER TABLE ""EmailMessages"" DROP COLUMN ""Direction"";
                    END IF;

                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'EmailMessages' AND column_name = 'SentAt'
                    ) THEN
                        ALTER TABLE ""EmailMessages"" DROP COLUMN ""SentAt"";
                    END IF;
                END $$;
            ");
        }
    }
}
