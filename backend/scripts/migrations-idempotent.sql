CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE TABLE "BackgroundJobs" (
        "Id" uuid NOT NULL,
        "JobType" character varying(100) NOT NULL,
        "PayloadJson" jsonb NOT NULL,
        "State" character varying(50) NOT NULL,
        "RetryCount" integer NOT NULL,
        "MaxRetries" integer NOT NULL,
        "LastError" character varying(2000),
        "Priority" integer NOT NULL,
        "ScheduledAt" timestamp with time zone,
        "CreatedAt" timestamp with time zone NOT NULL,
        "StartedAt" timestamp with time zone,
        "CompletedAt" timestamp with time zone,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_BackgroundJobs" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE TABLE "Buildings" (
        "Id" uuid NOT NULL,
        "Name" character varying(500) NOT NULL,
        "Code" character varying(100),
        "AddressLine1" character varying(500) NOT NULL,
        "AddressLine2" character varying(500),
        "City" character varying(100) NOT NULL,
        "State" character varying(100) NOT NULL,
        "Postcode" character varying(20) NOT NULL,
        "Latitude" numeric,
        "Longitude" numeric,
        "BuildingType" character varying(50),
        "IsActive" boolean NOT NULL,
        "CompanyId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Buildings" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE TABLE "Companies" (
        "Id" uuid NOT NULL,
        "LegalName" character varying(500) NOT NULL,
        "ShortName" character varying(100) NOT NULL,
        "RegistrationNo" character varying(100),
        "TaxId" character varying(100),
        "Vertical" character varying(50) NOT NULL,
        "Address" text,
        "Phone" character varying(50),
        "Email" character varying(255),
        "IsActive" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Companies" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE TABLE "CostCentres" (
        "Id" uuid NOT NULL,
        "Code" character varying(50) NOT NULL,
        "Name" character varying(200) NOT NULL,
        "Description" character varying(1000),
        "IsActive" boolean NOT NULL,
        "CompanyId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_CostCentres" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE TABLE "Departments" (
        "Id" uuid NOT NULL,
        "Name" character varying(200) NOT NULL,
        "Code" character varying(50),
        "Description" character varying(1000),
        "CostCentreId" uuid,
        "IsActive" boolean NOT NULL,
        "CompanyId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Departments" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE TABLE "DocumentPlaceholderDefinitions" (
        "Id" uuid NOT NULL,
        "DocumentType" character varying(100) NOT NULL,
        "Key" character varying(200) NOT NULL,
        "Description" character varying(500) NOT NULL,
        "ExampleValue" character varying(500),
        "IsRequired" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_DocumentPlaceholderDefinitions" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE TABLE "DocumentTemplates" (
        "Id" uuid NOT NULL,
        "Name" character varying(200) NOT NULL,
        "DocumentType" character varying(100) NOT NULL,
        "PartnerId" uuid,
        "IsActive" boolean NOT NULL,
        "Engine" character varying(50) NOT NULL,
        "HtmlBody" text NOT NULL,
        "JsonSchema" text,
        "Version" integer NOT NULL,
        "CreatedByUserId" uuid,
        "UpdatedByUserId" uuid,
        "CompanyId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_DocumentTemplates" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE TABLE "EmailMessages" (
        "Id" uuid NOT NULL,
        "EmailAccountId" uuid NOT NULL,
        "MessageId" character varying(500) NOT NULL,
        "FromAddress" character varying(255) NOT NULL,
        "ToAddresses" character varying(1000) NOT NULL,
        "CcAddresses" character varying(1000),
        "Subject" character varying(1000) NOT NULL,
        "BodyPreview" character varying(2000),
        "ReceivedAt" timestamp with time zone NOT NULL,
        "RawStoragePath" text,
        "HasAttachments" boolean NOT NULL,
        "ParserStatus" character varying(50) NOT NULL,
        "ParserError" character varying(2000),
        "CompanyId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_EmailMessages" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE TABLE "Files" (
        "Id" uuid NOT NULL,
        "FileName" character varying(500) NOT NULL,
        "StoragePath" character varying(1000) NOT NULL,
        "ContentType" character varying(100) NOT NULL,
        "SizeBytes" bigint NOT NULL,
        "Checksum" character varying(100),
        "CreatedById" uuid NOT NULL,
        "Module" character varying(50),
        "EntityId" uuid,
        "EntityType" character varying(50),
        "CompanyId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Files" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE TABLE "GeneratedDocuments" (
        "Id" uuid NOT NULL,
        "DocumentType" character varying(100) NOT NULL,
        "ReferenceEntity" character varying(100) NOT NULL,
        "ReferenceId" uuid NOT NULL,
        "TemplateId" uuid NOT NULL,
        "FileId" uuid NOT NULL,
        "Format" character varying(50) NOT NULL,
        "GeneratedAt" timestamp with time zone NOT NULL,
        "GeneratedByUserId" uuid,
        "MetadataJson" character varying(2000),
        "CompanyId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_GeneratedDocuments" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE TABLE "GlobalSettings" (
        "Id" uuid NOT NULL,
        "Key" character varying(200) NOT NULL,
        "Value" character varying(5000) NOT NULL,
        "ValueType" character varying(50) NOT NULL,
        "Description" character varying(1000),
        "Module" character varying(100),
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedByUserId" uuid,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "UpdatedByUserId" uuid,
        CONSTRAINT "PK_GlobalSettings" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE TABLE "Invoices" (
        "Id" uuid NOT NULL,
        "InvoiceNumber" character varying(100) NOT NULL,
        "PartnerId" uuid NOT NULL,
        "InvoiceDate" timestamp with time zone NOT NULL,
        "DueDate" timestamp with time zone,
        "TotalAmount" numeric(18,2) NOT NULL,
        "TaxAmount" numeric(18,2) NOT NULL,
        "SubTotal" numeric(18,2) NOT NULL,
        "Status" character varying(50) NOT NULL,
        "SubmissionId" character varying(200),
        "SubmittedAt" timestamp with time zone,
        "PaidAt" timestamp with time zone,
        "CreatedByUserId" uuid NOT NULL,
        "CompanyId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Invoices" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE TABLE "KpiProfiles" (
        "Id" uuid NOT NULL,
        "Name" character varying(200) NOT NULL,
        "PartnerId" uuid,
        "OrderType" character varying(100) NOT NULL,
        "BuildingTypeId" uuid,
        "MaxJobDurationMinutes" integer NOT NULL,
        "DocketKpiMinutes" integer NOT NULL,
        "MaxReschedulesAllowed" integer,
        "IsDefault" boolean NOT NULL,
        "EffectiveFrom" timestamp with time zone,
        "EffectiveTo" timestamp with time zone,
        "CreatedByUserId" uuid,
        "UpdatedByUserId" uuid,
        "CompanyId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_KpiProfiles" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE TABLE "Materials" (
        "Id" uuid NOT NULL,
        "ItemCode" character varying(100) NOT NULL,
        "Description" character varying(500) NOT NULL,
        "Category" character varying(100),
        "IsSerialised" boolean NOT NULL,
        "UnitOfMeasure" character varying(50) NOT NULL,
        "DefaultCost" numeric,
        "PartnerId" uuid,
        "VerticalFlags" character varying(200),
        "IsActive" boolean NOT NULL,
        "CompanyId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Materials" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE TABLE "MaterialTemplates" (
        "Id" uuid NOT NULL,
        "Name" character varying(200) NOT NULL,
        "OrderType" character varying(100) NOT NULL,
        "BuildingTypeId" uuid,
        "PartnerId" uuid,
        "IsDefault" boolean NOT NULL,
        "IsActive" boolean NOT NULL,
        "CreatedByUserId" uuid,
        "UpdatedByUserId" uuid,
        "CompanyId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_MaterialTemplates" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE TABLE "Notifications" (
        "Id" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "Type" character varying(50) NOT NULL,
        "Priority" character varying(20) NOT NULL,
        "Status" character varying(20) NOT NULL,
        "Title" character varying(500) NOT NULL,
        "Message" character varying(2000) NOT NULL,
        "ActionUrl" character varying(1000),
        "ActionText" character varying(200),
        "RelatedEntityId" uuid,
        "RelatedEntityType" character varying(100),
        "MetadataJson" jsonb,
        "ReadAt" timestamp with time zone,
        "ArchivedAt" timestamp with time zone,
        "ReadByUserId" uuid,
        "ExpiresAt" timestamp with time zone,
        "DeliveryChannels" character varying(200),
        "CompanyId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Notifications" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE TABLE "NotificationSettings" (
        "Id" uuid NOT NULL,
        "UserId" uuid,
        "NotificationType" character varying(50),
        "Channel" character varying(20) NOT NULL,
        "Enabled" boolean NOT NULL,
        "MinimumPriority" character varying(20),
        "SoundEnabled" boolean NOT NULL,
        "DesktopNotificationsEnabled" boolean NOT NULL,
        "Notes" character varying(1000),
        "CompanyId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_NotificationSettings" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE TABLE "OrderBlockers" (
        "Id" uuid NOT NULL,
        "OrderId" uuid NOT NULL,
        "BlockerType" character varying(50) NOT NULL,
        "Description" character varying(2000) NOT NULL,
        "RaisedBySiId" uuid,
        "RaisedByUserId" uuid,
        "RaisedAt" timestamp with time zone NOT NULL,
        "Resolved" boolean NOT NULL,
        "ResolvedAt" timestamp with time zone,
        "ResolvedByUserId" uuid,
        "ResolutionNotes" character varying(2000),
        "CompanyId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_OrderBlockers" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE TABLE "OrderDockets" (
        "Id" uuid NOT NULL,
        "OrderId" uuid NOT NULL,
        "FileId" uuid NOT NULL,
        "UploadedBySiId" uuid,
        "UploadedByUserId" uuid,
        "UploadSource" character varying(50) NOT NULL,
        "IsFinal" boolean NOT NULL,
        "Notes" character varying(2000),
        "CompanyId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_OrderDockets" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE TABLE "OrderMaterialUsage" (
        "Id" uuid NOT NULL,
        "OrderId" uuid NOT NULL,
        "MaterialId" uuid NOT NULL,
        "SerialisedItemId" uuid,
        "Quantity" numeric(18,4) NOT NULL,
        "UnitCost" numeric(18,4),
        "TotalCost" numeric(18,4),
        "SourceLocationId" uuid,
        "StockMovementId" uuid,
        "RecordedBySiId" uuid,
        "RecordedByUserId" uuid,
        "RecordedAt" timestamp with time zone NOT NULL,
        "Notes" character varying(2000),
        "CompanyId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_OrderMaterialUsage" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE TABLE "OrderReschedules" (
        "Id" uuid NOT NULL,
        "OrderId" uuid NOT NULL,
        "RequestedByUserId" uuid,
        "RequestedBySiId" uuid,
        "RequestedBySource" character varying(50) NOT NULL,
        "RequestedAt" timestamp with time zone NOT NULL,
        "OriginalDate" timestamp with time zone NOT NULL,
        "OriginalWindowFrom" interval NOT NULL,
        "OriginalWindowTo" interval NOT NULL,
        "NewDate" timestamp with time zone NOT NULL,
        "NewWindowFrom" interval NOT NULL,
        "NewWindowTo" interval NOT NULL,
        "Reason" character varying(2000) NOT NULL,
        "ApprovalSource" character varying(50),
        "ApprovalEmailId" uuid,
        "Status" character varying(50) NOT NULL,
        "StatusChangedByUserId" uuid,
        "StatusChangedAt" timestamp with time zone,
        "CompanyId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_OrderReschedules" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE TABLE "Orders" (
        "Id" uuid NOT NULL,
        "PartnerId" uuid NOT NULL,
        "SourceSystem" character varying(50) NOT NULL,
        "SourceEmailId" uuid,
        "OrderTypeId" uuid NOT NULL,
        "ServiceId" character varying(200) NOT NULL,
        "TicketId" character varying(200),
        "ExternalRef" character varying(200),
        "Status" character varying(50) NOT NULL,
        "StatusReason" character varying(500),
        "Priority" character varying(20),
        "BuildingId" uuid NOT NULL,
        "BuildingName" character varying(500),
        "UnitNo" character varying(50),
        "AddressLine1" character varying(500) NOT NULL,
        "AddressLine2" character varying(500),
        "City" character varying(100) NOT NULL,
        "State" character varying(100) NOT NULL,
        "Postcode" character varying(20) NOT NULL,
        "Latitude" numeric,
        "Longitude" numeric,
        "CustomerName" character varying(200) NOT NULL,
        "CustomerPhone" character varying(50) NOT NULL,
        "CustomerEmail" character varying(255),
        "OrderNotesInternal" text,
        "PartnerNotes" text,
        "RequestedAppointmentAt" timestamp with time zone,
        "AppointmentDate" timestamp with time zone NOT NULL,
        "AppointmentWindowFrom" interval NOT NULL,
        "AppointmentWindowTo" interval NOT NULL,
        "AssignedSiId" uuid,
        "AssignedTeamId" uuid,
        "KpiCategory" character varying(50),
        "KpiDueAt" timestamp with time zone,
        "KpiBreachedAt" timestamp with time zone,
        "HasReschedules" boolean NOT NULL,
        "RescheduleCount" integer NOT NULL,
        "DocketUploaded" boolean NOT NULL,
        "PhotosUploaded" boolean NOT NULL,
        "SerialsValidated" boolean NOT NULL,
        "InvoiceEligible" boolean NOT NULL,
        "InvoiceId" uuid,
        "PayrollPeriodId" uuid,
        "PnlPeriod" character varying(10),
        "CreatedByUserId" uuid NOT NULL,
        "CancelledAt" timestamp with time zone,
        "CancelledByUserId" uuid,
        "CompanyId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Orders" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE TABLE "OrderStatusLogs" (
        "Id" uuid NOT NULL,
        "OrderId" uuid NOT NULL,
        "FromStatus" character varying(50),
        "ToStatus" character varying(50) NOT NULL,
        "TransitionReason" character varying(500),
        "TriggeredByUserId" uuid,
        "TriggeredBySiId" uuid,
        "Source" character varying(50) NOT NULL,
        "MetadataJson" jsonb,
        "CompanyId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_OrderStatusLogs" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE TABLE "OverheadEntries" (
        "Id" uuid NOT NULL,
        "CostCentreId" uuid NOT NULL,
        "Period" character varying(20) NOT NULL,
        "Amount" numeric(18,2) NOT NULL,
        "Description" character varying(500) NOT NULL,
        "AllocationBasis" character varying(200),
        "CreatedByUserId" uuid NOT NULL,
        "CompanyId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_OverheadEntries" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE TABLE "ParsedOrderDrafts" (
        "Id" uuid NOT NULL,
        "ParseSessionId" uuid NOT NULL,
        "PartnerId" uuid,
        "BuildingId" uuid,
        "ServiceId" character varying(200),
        "TicketId" character varying(200),
        "CustomerName" character varying(200),
        "CustomerPhone" character varying(50),
        "AddressText" character varying(1000),
        "AppointmentDate" timestamp with time zone,
        "AppointmentWindow" character varying(50),
        "OrderTypeHint" character varying(100),
        "ConfidenceScore" numeric(5,4) NOT NULL,
        "ValidationStatus" character varying(50) NOT NULL,
        "ValidationNotes" character varying(2000),
        "CreatedOrderId" uuid,
        "CreatedByUserId" uuid,
        "CompanyId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_ParsedOrderDrafts" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE TABLE "ParserRules" (
        "Id" uuid NOT NULL,
        "EmailAccountId" uuid,
        "FromAddressPattern" character varying(500),
        "DomainPattern" character varying(200),
        "SubjectContains" character varying(500),
        "IsVip" boolean NOT NULL,
        "TargetDepartmentId" uuid,
        "TargetUserId" uuid,
        "ActionType" character varying(50) NOT NULL,
        "Priority" integer NOT NULL,
        "IsActive" boolean NOT NULL,
        "Description" character varying(1000),
        "CreatedByUserId" uuid NOT NULL,
        "UpdatedByUserId" uuid,
        "CompanyId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_ParserRules" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE TABLE "ParseSessions" (
        "Id" uuid NOT NULL,
        "EmailMessageId" uuid NOT NULL,
        "ParserTemplateId" uuid,
        "Status" character varying(50) NOT NULL,
        "ErrorMessage" character varying(2000),
        "SnapshotFileId" uuid,
        "ParsedOrdersCount" integer NOT NULL,
        "CompletedAt" timestamp with time zone,
        "CompanyId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_ParseSessions" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE TABLE "PartnerGroups" (
        "Id" uuid NOT NULL,
        "Name" character varying(200) NOT NULL,
        "CompanyId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_PartnerGroups" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE TABLE "Partners" (
        "Id" uuid NOT NULL,
        "Name" character varying(500) NOT NULL,
        "PartnerType" character varying(50) NOT NULL,
        "GroupId" uuid,
        "BillingAddress" text,
        "ContactName" character varying(200),
        "ContactEmail" character varying(255),
        "ContactPhone" character varying(50),
        "IsActive" boolean NOT NULL,
        "CompanyId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Partners" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE TABLE "PayrollPeriods" (
        "Id" uuid NOT NULL,
        "Period" character varying(20) NOT NULL,
        "PeriodStart" timestamp with time zone NOT NULL,
        "PeriodEnd" timestamp with time zone NOT NULL,
        "Status" character varying(50) NOT NULL,
        "IsLocked" boolean NOT NULL,
        "CreatedByUserId" uuid NOT NULL,
        "CompanyId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_PayrollPeriods" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE TABLE "Permissions" (
        "Id" uuid NOT NULL,
        "Name" character varying(200) NOT NULL,
        "Description" character varying(500),
        CONSTRAINT "PK_Permissions" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE TABLE "PnlDetailPerOrders" (
        "Id" uuid NOT NULL,
        "OrderId" uuid NOT NULL,
        "PartnerId" uuid NOT NULL,
        "Period" character varying(20) NOT NULL,
        "OrderType" character varying(50) NOT NULL,
        "RevenueAmount" numeric(18,2) NOT NULL,
        "MaterialCost" numeric(18,2) NOT NULL,
        "LabourCost" numeric(18,2) NOT NULL,
        "OverheadAllocated" numeric(18,2) NOT NULL,
        "ProfitForOrder" numeric(18,2) NOT NULL,
        "CompanyId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_PnlDetailPerOrders" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE TABLE "PnlPeriods" (
        "Id" uuid NOT NULL,
        "Period" character varying(20) NOT NULL,
        "PeriodStart" timestamp with time zone NOT NULL,
        "PeriodEnd" timestamp with time zone NOT NULL,
        "IsLocked" boolean NOT NULL,
        "CreatedByUserId" uuid NOT NULL,
        "LastRecalculatedAt" timestamp with time zone,
        "CompanyId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_PnlPeriods" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE TABLE "RmaRequests" (
        "Id" uuid NOT NULL,
        "PartnerId" uuid NOT NULL,
        "RmaNumber" character varying(100),
        "RequestDate" timestamp with time zone NOT NULL,
        "Reason" character varying(1000) NOT NULL,
        "Status" character varying(50) NOT NULL,
        "MraDocumentId" uuid,
        "CreatedByUserId" uuid NOT NULL,
        "CompanyId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_RmaRequests" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE TABLE "Roles" (
        "Id" uuid NOT NULL,
        "Name" character varying(100) NOT NULL,
        "Scope" character varying(50) NOT NULL,
        CONSTRAINT "PK_Roles" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE TABLE "ScheduledSlots" (
        "Id" uuid NOT NULL,
        "OrderId" uuid NOT NULL,
        "ServiceInstallerId" uuid NOT NULL,
        "Date" timestamp with time zone NOT NULL,
        "WindowFrom" interval NOT NULL,
        "WindowTo" interval NOT NULL,
        "PlannedTravelMin" integer,
        "SequenceIndex" integer NOT NULL,
        "Status" character varying(50) NOT NULL,
        "CreatedByUserId" uuid NOT NULL,
        "CompanyId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_ScheduledSlots" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE TABLE "ServiceInstallers" (
        "Id" uuid NOT NULL,
        "Name" character varying(200) NOT NULL,
        "EmployeeId" character varying(50),
        "Phone" character varying(50),
        "Email" character varying(255),
        "SiLevel" character varying(50) NOT NULL,
        "IsSubcontractor" boolean NOT NULL,
        "IsActive" boolean NOT NULL,
        "UserId" uuid,
        "CompanyId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_ServiceInstallers" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE TABLE "SiAvailabilities" (
        "Id" uuid NOT NULL,
        "ServiceInstallerId" uuid NOT NULL,
        "Date" timestamp with time zone NOT NULL,
        "IsWorkingDay" boolean NOT NULL,
        "WorkingFrom" interval,
        "WorkingTo" interval,
        "MaxJobs" integer NOT NULL,
        "CurrentJobsCount" integer NOT NULL,
        "Notes" character varying(1000),
        "CompanyId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_SiAvailabilities" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE TABLE "SiLeaveRequests" (
        "Id" uuid NOT NULL,
        "ServiceInstallerId" uuid NOT NULL,
        "DateFrom" timestamp with time zone NOT NULL,
        "DateTo" timestamp with time zone NOT NULL,
        "Reason" character varying(500) NOT NULL,
        "Status" character varying(50) NOT NULL,
        "ApprovedByUserId" uuid,
        "CompanyId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_SiLeaveRequests" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE TABLE "SiRatePlans" (
        "Id" uuid NOT NULL,
        "ServiceInstallerId" uuid NOT NULL,
        "Level" character varying(50) NOT NULL,
        "ActivationRate" numeric(18,2),
        "ModificationRate" numeric(18,2),
        "AssuranceRate" numeric(18,2),
        "FttrRate" numeric(18,2),
        "FttcRate" numeric(18,2),
        "SduRate" numeric(18,2),
        "RdfPoleRate" numeric(18,2),
        "OnTimeBonus" numeric(18,2),
        "LatePenalty" numeric(18,2),
        "ReworkRate" numeric(18,2),
        "IsActive" boolean NOT NULL,
        "CompanyId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_SiRatePlans" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE TABLE "SplitterPorts" (
        "Id" uuid NOT NULL,
        "SplitterId" uuid NOT NULL,
        "PortNumber" integer NOT NULL,
        "Status" character varying(50) NOT NULL,
        "OrderId" uuid,
        "AssignedAt" timestamp with time zone,
        "IsStandby" boolean NOT NULL,
        "CompanyId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_SplitterPorts" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE TABLE "Splitters" (
        "Id" uuid NOT NULL,
        "BuildingId" uuid NOT NULL,
        "Name" character varying(200) NOT NULL,
        "Code" character varying(100),
        "SplitterType" character varying(20) NOT NULL,
        "Location" character varying(200),
        "IsActive" boolean NOT NULL,
        "CompanyId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Splitters" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE TABLE "StockLocations" (
        "Id" uuid NOT NULL,
        "Name" character varying(200) NOT NULL,
        "Type" character varying(50) NOT NULL,
        "LinkedServiceInstallerId" uuid,
        "LinkedBuildingId" uuid,
        "IsActive" boolean NOT NULL,
        "CompanyId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_StockLocations" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE TABLE "SystemLogs" (
        "Id" uuid NOT NULL,
        "CompanyId" uuid,
        "Severity" character varying(50) NOT NULL,
        "Category" character varying(100) NOT NULL,
        "Message" character varying(1000) NOT NULL,
        "DetailsJson" jsonb,
        "UserId" uuid,
        "EntityType" character varying(100),
        "EntityId" uuid,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_SystemLogs" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE TABLE "TaskItems" (
        "Id" uuid NOT NULL,
        "DepartmentId" uuid,
        "RequestedByUserId" uuid NOT NULL,
        "AssignedToUserId" uuid NOT NULL,
        "Title" character varying(256) NOT NULL,
        "Description" character varying(4000),
        "RequestedAt" timestamp with time zone NOT NULL,
        "DueAt" timestamp with time zone,
        "Priority" character varying(20) NOT NULL,
        "Status" character varying(20) NOT NULL,
        "StartedAt" timestamp with time zone,
        "CompletedAt" timestamp with time zone,
        "CreatedByUserId" uuid NOT NULL,
        "UpdatedByUserId" uuid NOT NULL,
        "CompanyId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_TaskItems" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE TABLE "Users" (
        "Id" uuid NOT NULL,
        "Name" character varying(200) NOT NULL,
        "Email" character varying(255) NOT NULL,
        "Phone" character varying(50),
        "PasswordHash" character varying(500),
        "IsActive" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Users" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE TABLE "WorkflowDefinitions" (
        "Id" uuid NOT NULL,
        "Name" character varying(250) NOT NULL,
        "EntityType" character varying(100) NOT NULL,
        "Description" character varying(1000),
        "IsActive" boolean NOT NULL,
        "PartnerId" uuid,
        "CreatedByUserId" uuid,
        "UpdatedByUserId" uuid,
        "CompanyId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_WorkflowDefinitions" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE TABLE "MaterialAllocations" (
        "Id" uuid NOT NULL,
        "DepartmentId" uuid NOT NULL,
        "MaterialId" uuid NOT NULL,
        "Quantity" numeric(18,3) NOT NULL,
        "Notes" character varying(1000),
        "CompanyId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_MaterialAllocations" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_MaterialAllocations_Departments_DepartmentId" FOREIGN KEY ("DepartmentId") REFERENCES "Departments" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE TABLE "InvoiceLineItems" (
        "Id" uuid NOT NULL,
        "InvoiceId" uuid NOT NULL,
        "Description" character varying(500) NOT NULL,
        "Quantity" numeric(18,3) NOT NULL,
        "UnitPrice" numeric(18,2) NOT NULL,
        "Total" numeric(18,2) NOT NULL,
        "OrderId" uuid,
        "CompanyId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_InvoiceLineItems" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_InvoiceLineItems_Invoices_InvoiceId" FOREIGN KEY ("InvoiceId") REFERENCES "Invoices" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE TABLE "MaterialTemplateItems" (
        "Id" uuid NOT NULL,
        "MaterialTemplateId" uuid NOT NULL,
        "MaterialId" uuid NOT NULL,
        "Quantity" numeric NOT NULL,
        "UnitOfMeasure" character varying(50) NOT NULL,
        "IsSerialised" boolean NOT NULL,
        "Notes" character varying(500),
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_MaterialTemplateItems" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_MaterialTemplateItems_MaterialTemplates_MaterialTemplateId" FOREIGN KEY ("MaterialTemplateId") REFERENCES "MaterialTemplates" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE TABLE "PayrollRuns" (
        "Id" uuid NOT NULL,
        "PayrollPeriodId" uuid NOT NULL,
        "PeriodStart" timestamp with time zone NOT NULL,
        "PeriodEnd" timestamp with time zone NOT NULL,
        "Status" character varying(50) NOT NULL,
        "TotalAmount" numeric(18,2) NOT NULL,
        "Notes" character varying(1000),
        "ExportReference" character varying(200),
        "CreatedByUserId" uuid NOT NULL,
        "FinalizedAt" timestamp with time zone,
        "PaidAt" timestamp with time zone,
        "CompanyId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_PayrollRuns" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_PayrollRuns_PayrollPeriods_PayrollPeriodId" FOREIGN KEY ("PayrollPeriodId") REFERENCES "PayrollPeriods" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE TABLE "PnlFacts" (
        "Id" uuid NOT NULL,
        "PnlPeriodId" uuid,
        "PartnerId" uuid,
        "Vertical" character varying(50),
        "CostCentreId" uuid,
        "Period" character varying(20) NOT NULL,
        "OrderType" character varying(50),
        "RevenueAmount" numeric(18,2) NOT NULL,
        "DirectMaterialCost" numeric(18,2) NOT NULL,
        "DirectLabourCost" numeric(18,2) NOT NULL,
        "IndirectCost" numeric(18,2) NOT NULL,
        "GrossProfit" numeric(18,2) NOT NULL,
        "NetProfit" numeric(18,2) NOT NULL,
        "JobsCount" integer NOT NULL,
        "OrdersCompletedCount" integer NOT NULL,
        "ReschedulesCount" integer NOT NULL,
        "AssuranceJobsCount" integer NOT NULL,
        "LastRecalculatedAt" timestamp with time zone,
        "CompanyId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_PnlFacts" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_PnlFacts_PnlPeriods_PnlPeriodId" FOREIGN KEY ("PnlPeriodId") REFERENCES "PnlPeriods" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE TABLE "RmaRequestItems" (
        "Id" uuid NOT NULL,
        "RmaRequestId" uuid NOT NULL,
        "SerialisedItemId" uuid NOT NULL,
        "OriginalOrderId" uuid,
        "Notes" character varying(1000),
        "Result" character varying(50),
        "CompanyId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_RmaRequestItems" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_RmaRequestItems_RmaRequests_RmaRequestId" FOREIGN KEY ("RmaRequestId") REFERENCES "RmaRequests" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE TABLE "RolePermissions" (
        "RoleId" uuid NOT NULL,
        "PermissionId" uuid NOT NULL,
        CONSTRAINT "PK_RolePermissions" PRIMARY KEY ("RoleId", "PermissionId"),
        CONSTRAINT "FK_RolePermissions_Permissions_PermissionId" FOREIGN KEY ("PermissionId") REFERENCES "Permissions" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_RolePermissions_Roles_RoleId" FOREIGN KEY ("RoleId") REFERENCES "Roles" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE TABLE "SerialisedItems" (
        "Id" uuid NOT NULL,
        "MaterialId" uuid NOT NULL,
        "SerialNumber" character varying(200) NOT NULL,
        "CurrentLocationId" uuid,
        "Status" character varying(50) NOT NULL,
        "LastOrderId" uuid,
        "LastServiceId" uuid,
        "Notes" character varying(1000),
        "CompanyId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_SerialisedItems" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_SerialisedItems_Materials_MaterialId" FOREIGN KEY ("MaterialId") REFERENCES "Materials" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_SerialisedItems_StockLocations_CurrentLocationId" FOREIGN KEY ("CurrentLocationId") REFERENCES "StockLocations" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE TABLE "StockBalances" (
        "Id" uuid NOT NULL,
        "MaterialId" uuid NOT NULL,
        "StockLocationId" uuid NOT NULL,
        "Quantity" numeric NOT NULL,
        "CompanyId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_StockBalances" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_StockBalances_Materials_MaterialId" FOREIGN KEY ("MaterialId") REFERENCES "Materials" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_StockBalances_StockLocations_StockLocationId" FOREIGN KEY ("StockLocationId") REFERENCES "StockLocations" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE TABLE "StockMovements" (
        "Id" uuid NOT NULL,
        "FromLocationId" uuid,
        "ToLocationId" uuid,
        "MaterialId" uuid NOT NULL,
        "Quantity" numeric NOT NULL,
        "MovementType" character varying(50) NOT NULL,
        "OrderId" uuid,
        "ServiceInstallerId" uuid,
        "PartnerId" uuid,
        "Remarks" character varying(1000),
        "CreatedByUserId" uuid NOT NULL,
        "CompanyId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_StockMovements" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_StockMovements_Materials_MaterialId" FOREIGN KEY ("MaterialId") REFERENCES "Materials" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_StockMovements_StockLocations_FromLocationId" FOREIGN KEY ("FromLocationId") REFERENCES "StockLocations" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_StockMovements_StockLocations_ToLocationId" FOREIGN KEY ("ToLocationId") REFERENCES "StockLocations" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE TABLE "UserCompanies" (
        "Id" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "CompanyId" uuid NOT NULL,
        "IsDefault" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_UserCompanies" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_UserCompanies_Companies_CompanyId" FOREIGN KEY ("CompanyId") REFERENCES "Companies" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_UserCompanies_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE TABLE "UserRoles" (
        "Id" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "CompanyId" uuid NOT NULL,
        "RoleId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_UserRoles" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_UserRoles_Roles_RoleId" FOREIGN KEY ("RoleId") REFERENCES "Roles" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_UserRoles_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE TABLE "WorkflowJobs" (
        "Id" uuid NOT NULL,
        "WorkflowDefinitionId" uuid NOT NULL,
        "EntityType" character varying(100) NOT NULL,
        "EntityId" uuid NOT NULL,
        "CurrentStatus" character varying(100) NOT NULL,
        "TargetStatus" character varying(100) NOT NULL,
        "State" character varying(50) NOT NULL,
        "LastError" character varying(2000),
        "PayloadJson" jsonb,
        "InitiatedByUserId" uuid,
        "StartedAt" timestamp with time zone,
        "CompletedAt" timestamp with time zone,
        "CompanyId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_WorkflowJobs" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_WorkflowJobs_WorkflowDefinitions_WorkflowDefinitionId" FOREIGN KEY ("WorkflowDefinitionId") REFERENCES "WorkflowDefinitions" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE TABLE "WorkflowTransitions" (
        "Id" uuid NOT NULL,
        "WorkflowDefinitionId" uuid NOT NULL,
        "FromStatus" character varying(100),
        "ToStatus" character varying(100) NOT NULL,
        "AllowedRolesJson" jsonb NOT NULL,
        "GuardConditionsJson" jsonb,
        "SideEffectsConfigJson" jsonb,
        "DisplayOrder" integer NOT NULL,
        "IsActive" boolean NOT NULL,
        "CreatedByUserId" uuid,
        "UpdatedByUserId" uuid,
        "CompanyId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_WorkflowTransitions" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_WorkflowTransitions_WorkflowDefinitions_WorkflowDefinitionId" FOREIGN KEY ("WorkflowDefinitionId") REFERENCES "WorkflowDefinitions" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE TABLE "JobEarningRecords" (
        "Id" uuid NOT NULL,
        "PayrollRunId" uuid,
        "OrderId" uuid NOT NULL,
        "ServiceInstallerId" uuid NOT NULL,
        "JobType" character varying(50) NOT NULL,
        "KpiResult" character varying(50),
        "BaseRate" numeric(18,2) NOT NULL,
        "KpiAdjustment" numeric(18,2) NOT NULL,
        "FinalPay" numeric(18,2) NOT NULL,
        "Period" character varying(20) NOT NULL,
        "Status" character varying(50) NOT NULL,
        "ConfirmedAt" timestamp with time zone,
        "PaidAt" timestamp with time zone,
        "CompanyId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_JobEarningRecords" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_JobEarningRecords_PayrollRuns_PayrollRunId" FOREIGN KEY ("PayrollRunId") REFERENCES "PayrollRuns" ("Id") ON DELETE SET NULL
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE TABLE "PayrollLines" (
        "Id" uuid NOT NULL,
        "PayrollRunId" uuid NOT NULL,
        "ServiceInstallerId" uuid NOT NULL,
        "TotalJobs" integer NOT NULL,
        "TotalPay" numeric(18,2) NOT NULL,
        "Adjustments" numeric(18,2) NOT NULL,
        "NetPay" numeric(18,2) NOT NULL,
        "ExportReference" character varying(200),
        "CompanyId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_PayrollLines" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_PayrollLines_PayrollRuns_PayrollRunId" FOREIGN KEY ("PayrollRunId") REFERENCES "PayrollRuns" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_BackgroundJobs_JobType_State" ON "BackgroundJobs" ("JobType", "State");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_BackgroundJobs_ScheduledAt" ON "BackgroundJobs" ("ScheduledAt") WHERE "ScheduledAt" IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_BackgroundJobs_State_Priority_CreatedAt" ON "BackgroundJobs" ("State", "Priority", "CreatedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_Buildings_CompanyId_Code" ON "Buildings" ("CompanyId", "Code");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_Buildings_CompanyId_IsActive" ON "Buildings" ("CompanyId", "IsActive");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_Buildings_CompanyId_Name" ON "Buildings" ("CompanyId", "Name");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_Companies_IsActive" ON "Companies" ("IsActive");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_Companies_ShortName" ON "Companies" ("ShortName");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_CostCentres_CompanyId_Code" ON "CostCentres" ("CompanyId", "Code");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_CostCentres_CompanyId_IsActive" ON "CostCentres" ("CompanyId", "IsActive");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_Departments_CompanyId_Code" ON "Departments" ("CompanyId", "Code");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_Departments_CompanyId_IsActive" ON "Departments" ("CompanyId", "IsActive");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_Departments_CompanyId_Name" ON "Departments" ("CompanyId", "Name");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_DocumentPlaceholderDefinitions_DocumentType" ON "DocumentPlaceholderDefinitions" ("DocumentType");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_DocumentPlaceholderDefinitions_DocumentType_Key" ON "DocumentPlaceholderDefinitions" ("DocumentType", "Key");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_DocumentTemplates_CompanyId_DocumentType_PartnerId_IsActive" ON "DocumentTemplates" ("CompanyId", "DocumentType", "PartnerId", "IsActive");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_DocumentTemplates_CompanyId_IsActive" ON "DocumentTemplates" ("CompanyId", "IsActive");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_EmailMessages_CompanyId_MessageId" ON "EmailMessages" ("CompanyId", "MessageId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_EmailMessages_CompanyId_ParserStatus_ReceivedAt" ON "EmailMessages" ("CompanyId", "ParserStatus", "ReceivedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_Files_CompanyId_EntityId_EntityType" ON "Files" ("CompanyId", "EntityId", "EntityType");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_Files_CompanyId_Id" ON "Files" ("CompanyId", "Id");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_Files_CreatedAt" ON "Files" ("CreatedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_GeneratedDocuments_CompanyId_DocumentType_GeneratedAt" ON "GeneratedDocuments" ("CompanyId", "DocumentType", "GeneratedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_GeneratedDocuments_CompanyId_ReferenceEntity_ReferenceId" ON "GeneratedDocuments" ("CompanyId", "ReferenceEntity", "ReferenceId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_GeneratedDocuments_FileId" ON "GeneratedDocuments" ("FileId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_GeneratedDocuments_TemplateId" ON "GeneratedDocuments" ("TemplateId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_GlobalSettings_Key" ON "GlobalSettings" ("Key");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_GlobalSettings_Module" ON "GlobalSettings" ("Module");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_InvoiceLineItems_CompanyId_InvoiceId" ON "InvoiceLineItems" ("CompanyId", "InvoiceId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_InvoiceLineItems_InvoiceId" ON "InvoiceLineItems" ("InvoiceId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_InvoiceLineItems_OrderId" ON "InvoiceLineItems" ("OrderId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_Invoices_CompanyId_InvoiceDate" ON "Invoices" ("CompanyId", "InvoiceDate");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_Invoices_CompanyId_InvoiceNumber" ON "Invoices" ("CompanyId", "InvoiceNumber");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_Invoices_CompanyId_PartnerId" ON "Invoices" ("CompanyId", "PartnerId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_Invoices_CompanyId_Status" ON "Invoices" ("CompanyId", "Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_Invoices_SubmissionId" ON "Invoices" ("SubmissionId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_JobEarningRecords_CompanyId_OrderId" ON "JobEarningRecords" ("CompanyId", "OrderId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_JobEarningRecords_CompanyId_Period" ON "JobEarningRecords" ("CompanyId", "Period");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_JobEarningRecords_CompanyId_ServiceInstallerId" ON "JobEarningRecords" ("CompanyId", "ServiceInstallerId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_JobEarningRecords_PayrollRunId" ON "JobEarningRecords" ("PayrollRunId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_KpiProfiles_CompanyId_IsDefault_OrderType" ON "KpiProfiles" ("CompanyId", "IsDefault", "OrderType");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_KpiProfiles_CompanyId_PartnerId_OrderType_BuildingTypeId_Ef~" ON "KpiProfiles" ("CompanyId", "PartnerId", "OrderType", "BuildingTypeId", "EffectiveFrom");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_MaterialAllocations_CompanyId_DepartmentId" ON "MaterialAllocations" ("CompanyId", "DepartmentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_MaterialAllocations_DepartmentId" ON "MaterialAllocations" ("DepartmentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_MaterialAllocations_MaterialId" ON "MaterialAllocations" ("MaterialId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_Materials_CompanyId_Category" ON "Materials" ("CompanyId", "Category");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_Materials_CompanyId_IsActive" ON "Materials" ("CompanyId", "IsActive");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_Materials_CompanyId_ItemCode" ON "Materials" ("CompanyId", "ItemCode");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_MaterialTemplateItems_MaterialId" ON "MaterialTemplateItems" ("MaterialId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_MaterialTemplateItems_MaterialTemplateId" ON "MaterialTemplateItems" ("MaterialTemplateId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_MaterialTemplates_CompanyId_IsActive" ON "MaterialTemplates" ("CompanyId", "IsActive");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_MaterialTemplates_CompanyId_IsDefault_OrderType" ON "MaterialTemplates" ("CompanyId", "IsDefault", "OrderType");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_MaterialTemplates_CompanyId_OrderType_BuildingTypeId_Partne~" ON "MaterialTemplates" ("CompanyId", "OrderType", "BuildingTypeId", "PartnerId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_Notifications_CompanyId_Type_CreatedAt" ON "Notifications" ("CompanyId", "Type", "CreatedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_Notifications_RelatedEntityId_RelatedEntityType" ON "Notifications" ("RelatedEntityId", "RelatedEntityType");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_Notifications_UserId_CompanyId_Status" ON "Notifications" ("UserId", "CompanyId", "Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_NotificationSettings_CompanyId_NotificationType" ON "NotificationSettings" ("CompanyId", "NotificationType");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_NotificationSettings_UserId_CompanyId_NotificationType" ON "NotificationSettings" ("UserId", "CompanyId", "NotificationType");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_OrderBlockers_CompanyId_OrderId_Resolved" ON "OrderBlockers" ("CompanyId", "OrderId", "Resolved");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_OrderBlockers_OrderId" ON "OrderBlockers" ("OrderId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_OrderDockets_CompanyId_OrderId" ON "OrderDockets" ("CompanyId", "OrderId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_OrderDockets_CompanyId_OrderId_IsFinal" ON "OrderDockets" ("CompanyId", "OrderId", "IsFinal");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_OrderMaterialUsage_CompanyId_MaterialId" ON "OrderMaterialUsage" ("CompanyId", "MaterialId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_OrderMaterialUsage_CompanyId_OrderId" ON "OrderMaterialUsage" ("CompanyId", "OrderId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_OrderMaterialUsage_SerialisedItemId" ON "OrderMaterialUsage" ("SerialisedItemId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_OrderReschedules_CompanyId_OrderId" ON "OrderReschedules" ("CompanyId", "OrderId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_OrderReschedules_CompanyId_Status" ON "OrderReschedules" ("CompanyId", "Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_Orders_CompanyId_AssignedSiId_AppointmentDate" ON "Orders" ("CompanyId", "AssignedSiId", "AppointmentDate");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_Orders_CompanyId_BuildingId" ON "Orders" ("CompanyId", "BuildingId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_Orders_CompanyId_PartnerId" ON "Orders" ("CompanyId", "PartnerId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_Orders_CompanyId_ServiceId" ON "Orders" ("CompanyId", "ServiceId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_Orders_CompanyId_Status_AppointmentDate" ON "Orders" ("CompanyId", "Status", "AppointmentDate");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_Orders_Status" ON "Orders" ("Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_OrderStatusLogs_CompanyId_CreatedAt" ON "OrderStatusLogs" ("CompanyId", "CreatedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_OrderStatusLogs_CompanyId_OrderId" ON "OrderStatusLogs" ("CompanyId", "OrderId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_OrderStatusLogs_OrderId" ON "OrderStatusLogs" ("OrderId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_OverheadEntries_CompanyId_CostCentreId" ON "OverheadEntries" ("CompanyId", "CostCentreId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_OverheadEntries_CompanyId_Period" ON "OverheadEntries" ("CompanyId", "Period");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_ParsedOrderDrafts_CompanyId_ParseSessionId" ON "ParsedOrderDrafts" ("CompanyId", "ParseSessionId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_ParsedOrderDrafts_CompanyId_ValidationStatus" ON "ParsedOrderDrafts" ("CompanyId", "ValidationStatus");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_ParsedOrderDrafts_CreatedOrderId" ON "ParsedOrderDrafts" ("CreatedOrderId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_ParserRules_CompanyId_EmailAccountId_IsActive" ON "ParserRules" ("CompanyId", "EmailAccountId", "IsActive");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_ParserRules_CompanyId_Priority_IsActive" ON "ParserRules" ("CompanyId", "Priority", "IsActive");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_ParseSessions_CompanyId_EmailMessageId" ON "ParseSessions" ("CompanyId", "EmailMessageId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_ParseSessions_CompanyId_Status_CreatedAt" ON "ParseSessions" ("CompanyId", "Status", "CreatedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_PartnerGroups_CompanyId_Name" ON "PartnerGroups" ("CompanyId", "Name");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_Partners_CompanyId_IsActive" ON "Partners" ("CompanyId", "IsActive");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_Partners_CompanyId_Name" ON "Partners" ("CompanyId", "Name");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_PayrollLines_CompanyId_PayrollRunId" ON "PayrollLines" ("CompanyId", "PayrollRunId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_PayrollLines_PayrollRunId" ON "PayrollLines" ("PayrollRunId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_PayrollLines_ServiceInstallerId" ON "PayrollLines" ("ServiceInstallerId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_PayrollPeriods_CompanyId_Period" ON "PayrollPeriods" ("CompanyId", "Period");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_PayrollPeriods_CompanyId_Status" ON "PayrollPeriods" ("CompanyId", "Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_PayrollRuns_CompanyId_PayrollPeriodId" ON "PayrollRuns" ("CompanyId", "PayrollPeriodId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_PayrollRuns_CompanyId_PeriodStart_PeriodEnd" ON "PayrollRuns" ("CompanyId", "PeriodStart", "PeriodEnd");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_PayrollRuns_CompanyId_Status" ON "PayrollRuns" ("CompanyId", "Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_PayrollRuns_PayrollPeriodId" ON "PayrollRuns" ("PayrollPeriodId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_Permissions_Name" ON "Permissions" ("Name");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_PnlDetailPerOrders_CompanyId_OrderId" ON "PnlDetailPerOrders" ("CompanyId", "OrderId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_PnlDetailPerOrders_CompanyId_Period" ON "PnlDetailPerOrders" ("CompanyId", "Period");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_PnlDetailPerOrders_PartnerId" ON "PnlDetailPerOrders" ("PartnerId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_PnlFacts_CompanyId_CostCentreId" ON "PnlFacts" ("CompanyId", "CostCentreId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_PnlFacts_CompanyId_PartnerId" ON "PnlFacts" ("CompanyId", "PartnerId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_PnlFacts_CompanyId_Period" ON "PnlFacts" ("CompanyId", "Period");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_PnlFacts_PnlPeriodId" ON "PnlFacts" ("PnlPeriodId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_PnlPeriods_CompanyId_Period" ON "PnlPeriods" ("CompanyId", "Period");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_RmaRequestItems_CompanyId_RmaRequestId" ON "RmaRequestItems" ("CompanyId", "RmaRequestId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_RmaRequestItems_RmaRequestId" ON "RmaRequestItems" ("RmaRequestId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_RmaRequestItems_SerialisedItemId" ON "RmaRequestItems" ("SerialisedItemId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_RmaRequests_CompanyId_PartnerId" ON "RmaRequests" ("CompanyId", "PartnerId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_RmaRequests_CompanyId_RequestDate" ON "RmaRequests" ("CompanyId", "RequestDate");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_RmaRequests_CompanyId_Status" ON "RmaRequests" ("CompanyId", "Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_RmaRequests_RmaNumber" ON "RmaRequests" ("RmaNumber");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_RolePermissions_PermissionId" ON "RolePermissions" ("PermissionId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_Roles_Name" ON "Roles" ("Name");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_ScheduledSlots_CompanyId_Date_Status" ON "ScheduledSlots" ("CompanyId", "Date", "Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_ScheduledSlots_CompanyId_OrderId" ON "ScheduledSlots" ("CompanyId", "OrderId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_ScheduledSlots_CompanyId_ServiceInstallerId_Date" ON "ScheduledSlots" ("CompanyId", "ServiceInstallerId", "Date");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_SerialisedItems_CompanyId_MaterialId" ON "SerialisedItems" ("CompanyId", "MaterialId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_SerialisedItems_CompanyId_SerialNumber" ON "SerialisedItems" ("CompanyId", "SerialNumber");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_SerialisedItems_CompanyId_Status" ON "SerialisedItems" ("CompanyId", "Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_SerialisedItems_CurrentLocationId" ON "SerialisedItems" ("CurrentLocationId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_SerialisedItems_LastOrderId" ON "SerialisedItems" ("LastOrderId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_SerialisedItems_MaterialId" ON "SerialisedItems" ("MaterialId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_ServiceInstallers_CompanyId_EmployeeId" ON "ServiceInstallers" ("CompanyId", "EmployeeId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_ServiceInstallers_CompanyId_IsActive" ON "ServiceInstallers" ("CompanyId", "IsActive");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_ServiceInstallers_UserId" ON "ServiceInstallers" ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_SiAvailabilities_CompanyId_Date" ON "SiAvailabilities" ("CompanyId", "Date");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_SiAvailabilities_CompanyId_ServiceInstallerId_Date" ON "SiAvailabilities" ("CompanyId", "ServiceInstallerId", "Date");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_SiLeaveRequests_CompanyId_ServiceInstallerId_DateFrom_DateTo" ON "SiLeaveRequests" ("CompanyId", "ServiceInstallerId", "DateFrom", "DateTo");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_SiLeaveRequests_CompanyId_Status" ON "SiLeaveRequests" ("CompanyId", "Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_SiRatePlans_CompanyId_ServiceInstallerId_IsActive" ON "SiRatePlans" ("CompanyId", "ServiceInstallerId", "IsActive");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_SplitterPorts_CompanyId_SplitterId_PortNumber" ON "SplitterPorts" ("CompanyId", "SplitterId", "PortNumber");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_SplitterPorts_CompanyId_SplitterId_Status" ON "SplitterPorts" ("CompanyId", "SplitterId", "Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_SplitterPorts_OrderId" ON "SplitterPorts" ("OrderId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_Splitters_CompanyId_BuildingId" ON "Splitters" ("CompanyId", "BuildingId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_Splitters_CompanyId_IsActive" ON "Splitters" ("CompanyId", "IsActive");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_StockBalances_CompanyId_MaterialId_StockLocationId" ON "StockBalances" ("CompanyId", "MaterialId", "StockLocationId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_StockBalances_CompanyId_StockLocationId" ON "StockBalances" ("CompanyId", "StockLocationId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_StockBalances_MaterialId" ON "StockBalances" ("MaterialId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_StockBalances_StockLocationId" ON "StockBalances" ("StockLocationId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_StockLocations_CompanyId_Name" ON "StockLocations" ("CompanyId", "Name");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_StockLocations_CompanyId_Type" ON "StockLocations" ("CompanyId", "Type");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_StockMovements_CompanyId_CreatedAt" ON "StockMovements" ("CompanyId", "CreatedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_StockMovements_CompanyId_MaterialId" ON "StockMovements" ("CompanyId", "MaterialId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_StockMovements_CompanyId_OrderId" ON "StockMovements" ("CompanyId", "OrderId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_StockMovements_FromLocationId" ON "StockMovements" ("FromLocationId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_StockMovements_MaterialId" ON "StockMovements" ("MaterialId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_StockMovements_MovementType" ON "StockMovements" ("MovementType");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_StockMovements_ToLocationId" ON "StockMovements" ("ToLocationId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_SystemLogs_CompanyId_Category_CreatedAt" ON "SystemLogs" ("CompanyId", "Category", "CreatedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_SystemLogs_EntityType_EntityId_CreatedAt" ON "SystemLogs" ("EntityType", "EntityId", "CreatedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_SystemLogs_Severity_CreatedAt" ON "SystemLogs" ("Severity", "CreatedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_TaskItems_CompanyId_AssignedToUserId_Status" ON "TaskItems" ("CompanyId", "AssignedToUserId", "Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_TaskItems_CompanyId_DepartmentId_Status" ON "TaskItems" ("CompanyId", "DepartmentId", "Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_TaskItems_CompanyId_RequestedByUserId" ON "TaskItems" ("CompanyId", "RequestedByUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_UserCompanies_CompanyId" ON "UserCompanies" ("CompanyId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_UserCompanies_UserId_CompanyId" ON "UserCompanies" ("UserId", "CompanyId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_UserCompanies_UserId_IsDefault" ON "UserCompanies" ("UserId", "IsDefault");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_UserRoles_RoleId" ON "UserRoles" ("RoleId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_UserRoles_UserId_CompanyId_RoleId" ON "UserRoles" ("UserId", "CompanyId", "RoleId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_Users_Email" ON "Users" ("Email");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_Users_IsActive" ON "Users" ("IsActive");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_WorkflowDefinitions_CompanyId_EntityType_IsActive" ON "WorkflowDefinitions" ("CompanyId", "EntityType", "IsActive");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_WorkflowDefinitions_CompanyId_Name" ON "WorkflowDefinitions" ("CompanyId", "Name");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_WorkflowJobs_CompanyId_EntityType_EntityId" ON "WorkflowJobs" ("CompanyId", "EntityType", "EntityId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_WorkflowJobs_CompanyId_State_CreatedAt" ON "WorkflowJobs" ("CompanyId", "State", "CreatedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_WorkflowJobs_WorkflowDefinitionId_EntityId" ON "WorkflowJobs" ("WorkflowDefinitionId", "EntityId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_WorkflowTransitions_CompanyId_WorkflowDefinitionId_FromStat~" ON "WorkflowTransitions" ("CompanyId", "WorkflowDefinitionId", "FromStatus", "ToStatus");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_WorkflowTransitions_CompanyId_WorkflowDefinitionId_IsActive" ON "WorkflowTransitions" ("CompanyId", "WorkflowDefinitionId", "IsActive");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    CREATE INDEX "IX_WorkflowTransitions_WorkflowDefinitionId" ON "WorkflowTransitions" ("WorkflowDefinitionId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123142132_InitialCreate') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251123142132_InitialCreate', '10.0.3');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123185309_AddCompanyDocuments') THEN
    CREATE TABLE "BillingRatecards" (
        "Id" uuid NOT NULL,
        "PartnerId" uuid NOT NULL,
        "OrderTypeId" uuid NOT NULL,
        "BuildingType" character varying(100),
        "Description" character varying(500),
        "Amount" numeric(18,2) NOT NULL,
        "TaxRate" numeric(5,4) NOT NULL,
        "IsActive" boolean NOT NULL,
        "EffectiveFrom" timestamp with time zone,
        "EffectiveTo" timestamp with time zone,
        "CompanyId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_BillingRatecards" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123185309_AddCompanyDocuments') THEN
    CREATE TABLE "CompanyDocuments" (
        "Id" uuid NOT NULL,
        "Category" character varying(50) NOT NULL,
        "Title" character varying(500) NOT NULL,
        "DocumentType" character varying(200) NOT NULL,
        "FileId" uuid NOT NULL,
        "EffectiveDate" timestamp with time zone,
        "ExpiryDate" timestamp with time zone,
        "IsCritical" boolean NOT NULL,
        "Notes" character varying(2000),
        "RelatedModule" character varying(100),
        "RelatedEntityId" uuid,
        "CreatedByUserId" uuid NOT NULL,
        "CompanyId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_CompanyDocuments" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123185309_AddCompanyDocuments') THEN
    CREATE INDEX "IX_BillingRatecards_CompanyId_EffectiveFrom_EffectiveTo" ON "BillingRatecards" ("CompanyId", "EffectiveFrom", "EffectiveTo");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123185309_AddCompanyDocuments') THEN
    CREATE INDEX "IX_BillingRatecards_CompanyId_IsActive" ON "BillingRatecards" ("CompanyId", "IsActive");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123185309_AddCompanyDocuments') THEN
    CREATE INDEX "IX_BillingRatecards_CompanyId_PartnerId_OrderTypeId" ON "BillingRatecards" ("CompanyId", "PartnerId", "OrderTypeId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123185309_AddCompanyDocuments') THEN
    CREATE INDEX "IX_CompanyDocuments_CompanyId_Category" ON "CompanyDocuments" ("CompanyId", "Category");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123185309_AddCompanyDocuments') THEN
    CREATE INDEX "IX_CompanyDocuments_CompanyId_ExpiryDate" ON "CompanyDocuments" ("CompanyId", "ExpiryDate");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123185309_AddCompanyDocuments') THEN
    CREATE INDEX "IX_CompanyDocuments_CompanyId_IsCritical" ON "CompanyDocuments" ("CompanyId", "IsCritical");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123185309_AddCompanyDocuments') THEN
    CREATE INDEX "IX_CompanyDocuments_FileId" ON "CompanyDocuments" ("FileId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123185309_AddCompanyDocuments') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251123185309_AddCompanyDocuments', '10.0.3');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123193706_AddDepartmentIdToPartnersBuildingsSplitters') THEN
    ALTER TABLE "Splitters" ADD "Block" character varying(50);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123193706_AddDepartmentIdToPartnersBuildingsSplitters') THEN
    ALTER TABLE "Splitters" ADD "DepartmentId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123193706_AddDepartmentIdToPartnersBuildingsSplitters') THEN
    ALTER TABLE "Splitters" ADD "Floor" character varying(50);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123193706_AddDepartmentIdToPartnersBuildingsSplitters') THEN
    ALTER TABLE "Partners" ADD "DepartmentId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123193706_AddDepartmentIdToPartnersBuildingsSplitters') THEN
    ALTER TABLE "Buildings" ADD "DepartmentId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123193706_AddDepartmentIdToPartnersBuildingsSplitters') THEN
    CREATE INDEX "IX_Splitters_CompanyId_DepartmentId" ON "Splitters" ("CompanyId", "DepartmentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123193706_AddDepartmentIdToPartnersBuildingsSplitters') THEN
    CREATE INDEX "IX_Splitters_DepartmentId" ON "Splitters" ("DepartmentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123193706_AddDepartmentIdToPartnersBuildingsSplitters') THEN
    CREATE INDEX "IX_Partners_CompanyId_DepartmentId" ON "Partners" ("CompanyId", "DepartmentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123193706_AddDepartmentIdToPartnersBuildingsSplitters') THEN
    CREATE INDEX "IX_Partners_DepartmentId" ON "Partners" ("DepartmentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123193706_AddDepartmentIdToPartnersBuildingsSplitters') THEN
    CREATE INDEX "IX_Buildings_CompanyId_DepartmentId" ON "Buildings" ("CompanyId", "DepartmentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123193706_AddDepartmentIdToPartnersBuildingsSplitters') THEN
    CREATE INDEX "IX_Buildings_DepartmentId" ON "Buildings" ("DepartmentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123193706_AddDepartmentIdToPartnersBuildingsSplitters') THEN
    ALTER TABLE "Buildings" ADD CONSTRAINT "FK_Buildings_Departments_DepartmentId" FOREIGN KEY ("DepartmentId") REFERENCES "Departments" ("Id") ON DELETE SET NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123193706_AddDepartmentIdToPartnersBuildingsSplitters') THEN
    ALTER TABLE "Partners" ADD CONSTRAINT "FK_Partners_Departments_DepartmentId" FOREIGN KEY ("DepartmentId") REFERENCES "Departments" ("Id") ON DELETE SET NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123193706_AddDepartmentIdToPartnersBuildingsSplitters') THEN
    ALTER TABLE "Splitters" ADD CONSTRAINT "FK_Splitters_Departments_DepartmentId" FOREIGN KEY ("DepartmentId") REFERENCES "Departments" ("Id") ON DELETE SET NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123193706_AddDepartmentIdToPartnersBuildingsSplitters') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251123193706_AddDepartmentIdToPartnersBuildingsSplitters', '10.0.3');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251124035749_AddOrderTypeInstallationTypeBuildingTypeSplitterType') THEN
    ALTER TABLE "Splitters" DROP COLUMN "SplitterType";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251124035749_AddOrderTypeInstallationTypeBuildingTypeSplitterType') THEN
    ALTER TABLE "Buildings" DROP COLUMN "BuildingType";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251124035749_AddOrderTypeInstallationTypeBuildingTypeSplitterType') THEN
    ALTER TABLE "Splitters" ADD "SplitterTypeId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251124035749_AddOrderTypeInstallationTypeBuildingTypeSplitterType') THEN
    ALTER TABLE "Orders" ADD "InstallationTypeId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251124035749_AddOrderTypeInstallationTypeBuildingTypeSplitterType') THEN
    ALTER TABLE "Buildings" ADD "BuildingTypeId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251124035749_AddOrderTypeInstallationTypeBuildingTypeSplitterType') THEN
    CREATE TABLE "BuildingTypes" (
        "Id" uuid NOT NULL,
        "DepartmentId" uuid,
        "Name" character varying(100) NOT NULL,
        "Code" character varying(50) NOT NULL,
        "Description" character varying(500),
        "IsActive" boolean NOT NULL,
        "DisplayOrder" integer NOT NULL,
        "CompanyId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_BuildingTypes" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_BuildingTypes_Departments_DepartmentId" FOREIGN KEY ("DepartmentId") REFERENCES "Departments" ("Id") ON DELETE SET NULL
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251124035749_AddOrderTypeInstallationTypeBuildingTypeSplitterType') THEN
    CREATE TABLE "InstallationTypes" (
        "Id" uuid NOT NULL,
        "DepartmentId" uuid,
        "Name" character varying(100) NOT NULL,
        "Code" character varying(50) NOT NULL,
        "Description" character varying(500),
        "IsActive" boolean NOT NULL,
        "DisplayOrder" integer NOT NULL,
        "CompanyId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_InstallationTypes" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_InstallationTypes_Departments_DepartmentId" FOREIGN KEY ("DepartmentId") REFERENCES "Departments" ("Id") ON DELETE SET NULL
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251124035749_AddOrderTypeInstallationTypeBuildingTypeSplitterType') THEN
    CREATE TABLE "OrderTypes" (
        "Id" uuid NOT NULL,
        "DepartmentId" uuid,
        "Name" character varying(100) NOT NULL,
        "Code" character varying(50) NOT NULL,
        "Description" character varying(500),
        "IsActive" boolean NOT NULL,
        "DisplayOrder" integer NOT NULL,
        "CompanyId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_OrderTypes" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_OrderTypes_Departments_DepartmentId" FOREIGN KEY ("DepartmentId") REFERENCES "Departments" ("Id") ON DELETE SET NULL
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251124035749_AddOrderTypeInstallationTypeBuildingTypeSplitterType') THEN
    CREATE TABLE "SplitterTypes" (
        "Id" uuid NOT NULL,
        "DepartmentId" uuid,
        "Name" character varying(50) NOT NULL,
        "Code" character varying(20) NOT NULL,
        "TotalPorts" integer NOT NULL,
        "StandbyPortNumber" integer,
        "Description" character varying(500),
        "IsActive" boolean NOT NULL,
        "DisplayOrder" integer NOT NULL,
        "CompanyId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_SplitterTypes" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_SplitterTypes_Departments_DepartmentId" FOREIGN KEY ("DepartmentId") REFERENCES "Departments" ("Id") ON DELETE SET NULL
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251124035749_AddOrderTypeInstallationTypeBuildingTypeSplitterType') THEN
    CREATE INDEX "IX_Splitters_SplitterTypeId" ON "Splitters" ("SplitterTypeId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251124035749_AddOrderTypeInstallationTypeBuildingTypeSplitterType') THEN
    CREATE INDEX "IX_Orders_InstallationTypeId" ON "Orders" ("InstallationTypeId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251124035749_AddOrderTypeInstallationTypeBuildingTypeSplitterType') THEN
    CREATE INDEX "IX_Orders_OrderTypeId" ON "Orders" ("OrderTypeId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251124035749_AddOrderTypeInstallationTypeBuildingTypeSplitterType') THEN
    CREATE INDEX "IX_Buildings_BuildingTypeId" ON "Buildings" ("BuildingTypeId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251124035749_AddOrderTypeInstallationTypeBuildingTypeSplitterType') THEN
    CREATE INDEX "IX_BuildingTypes_CompanyId_Code" ON "BuildingTypes" ("CompanyId", "Code");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251124035749_AddOrderTypeInstallationTypeBuildingTypeSplitterType') THEN
    CREATE INDEX "IX_BuildingTypes_CompanyId_DepartmentId" ON "BuildingTypes" ("CompanyId", "DepartmentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251124035749_AddOrderTypeInstallationTypeBuildingTypeSplitterType') THEN
    CREATE INDEX "IX_BuildingTypes_CompanyId_IsActive" ON "BuildingTypes" ("CompanyId", "IsActive");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251124035749_AddOrderTypeInstallationTypeBuildingTypeSplitterType') THEN
    CREATE INDEX "IX_BuildingTypes_DepartmentId" ON "BuildingTypes" ("DepartmentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251124035749_AddOrderTypeInstallationTypeBuildingTypeSplitterType') THEN
    CREATE INDEX "IX_InstallationTypes_CompanyId_Code" ON "InstallationTypes" ("CompanyId", "Code");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251124035749_AddOrderTypeInstallationTypeBuildingTypeSplitterType') THEN
    CREATE INDEX "IX_InstallationTypes_CompanyId_DepartmentId" ON "InstallationTypes" ("CompanyId", "DepartmentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251124035749_AddOrderTypeInstallationTypeBuildingTypeSplitterType') THEN
    CREATE INDEX "IX_InstallationTypes_CompanyId_IsActive" ON "InstallationTypes" ("CompanyId", "IsActive");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251124035749_AddOrderTypeInstallationTypeBuildingTypeSplitterType') THEN
    CREATE INDEX "IX_InstallationTypes_DepartmentId" ON "InstallationTypes" ("DepartmentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251124035749_AddOrderTypeInstallationTypeBuildingTypeSplitterType') THEN
    CREATE INDEX "IX_OrderTypes_CompanyId_Code" ON "OrderTypes" ("CompanyId", "Code");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251124035749_AddOrderTypeInstallationTypeBuildingTypeSplitterType') THEN
    CREATE INDEX "IX_OrderTypes_CompanyId_DepartmentId" ON "OrderTypes" ("CompanyId", "DepartmentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251124035749_AddOrderTypeInstallationTypeBuildingTypeSplitterType') THEN
    CREATE INDEX "IX_OrderTypes_CompanyId_IsActive" ON "OrderTypes" ("CompanyId", "IsActive");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251124035749_AddOrderTypeInstallationTypeBuildingTypeSplitterType') THEN
    CREATE INDEX "IX_OrderTypes_DepartmentId" ON "OrderTypes" ("DepartmentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251124035749_AddOrderTypeInstallationTypeBuildingTypeSplitterType') THEN
    CREATE INDEX "IX_SplitterTypes_CompanyId_Code" ON "SplitterTypes" ("CompanyId", "Code");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251124035749_AddOrderTypeInstallationTypeBuildingTypeSplitterType') THEN
    CREATE INDEX "IX_SplitterTypes_CompanyId_DepartmentId" ON "SplitterTypes" ("CompanyId", "DepartmentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251124035749_AddOrderTypeInstallationTypeBuildingTypeSplitterType') THEN
    CREATE INDEX "IX_SplitterTypes_CompanyId_IsActive" ON "SplitterTypes" ("CompanyId", "IsActive");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251124035749_AddOrderTypeInstallationTypeBuildingTypeSplitterType') THEN
    CREATE INDEX "IX_SplitterTypes_DepartmentId" ON "SplitterTypes" ("DepartmentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251124035749_AddOrderTypeInstallationTypeBuildingTypeSplitterType') THEN
    ALTER TABLE "Buildings" ADD CONSTRAINT "FK_Buildings_BuildingTypes_BuildingTypeId" FOREIGN KEY ("BuildingTypeId") REFERENCES "BuildingTypes" ("Id") ON DELETE SET NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251124035749_AddOrderTypeInstallationTypeBuildingTypeSplitterType') THEN
    ALTER TABLE "Orders" ADD CONSTRAINT "FK_Orders_InstallationTypes_InstallationTypeId" FOREIGN KEY ("InstallationTypeId") REFERENCES "InstallationTypes" ("Id") ON DELETE SET NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251124035749_AddOrderTypeInstallationTypeBuildingTypeSplitterType') THEN
    ALTER TABLE "Orders" ADD CONSTRAINT "FK_Orders_OrderTypes_OrderTypeId" FOREIGN KEY ("OrderTypeId") REFERENCES "OrderTypes" ("Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251124035749_AddOrderTypeInstallationTypeBuildingTypeSplitterType') THEN
    ALTER TABLE "Splitters" ADD CONSTRAINT "FK_Splitters_SplitterTypes_SplitterTypeId" FOREIGN KEY ("SplitterTypeId") REFERENCES "SplitterTypes" ("Id") ON DELETE SET NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251124035749_AddOrderTypeInstallationTypeBuildingTypeSplitterType') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251124035749_AddOrderTypeInstallationTypeBuildingTypeSplitterType', '10.0.3');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251124051255_AddVerticalsTable') THEN
    CREATE TABLE "Verticals" (
        "Id" uuid NOT NULL,
        "Name" character varying(100) NOT NULL,
        "Code" character varying(50) NOT NULL,
        "Description" character varying(500),
        "IsActive" boolean NOT NULL,
        "DisplayOrder" integer NOT NULL DEFAULT 0,
        "CompanyId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Verticals" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251124051255_AddVerticalsTable') THEN
    CREATE UNIQUE INDEX "IX_Verticals_CompanyId_Code" ON "Verticals" ("CompanyId", "Code");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251124051255_AddVerticalsTable') THEN
    CREATE INDEX "IX_Verticals_CompanyId_IsActive" ON "Verticals" ("CompanyId", "IsActive");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251124051255_AddVerticalsTable') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251124051255_AddVerticalsTable', '10.0.3');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251125070624_AddDepartmentIdToServiceInstaller') THEN
    ALTER TABLE "WorkflowTransitions" ALTER COLUMN "CompanyId" DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251125070624_AddDepartmentIdToServiceInstaller') THEN
    ALTER TABLE "WorkflowJobs" ALTER COLUMN "CompanyId" DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251125070624_AddDepartmentIdToServiceInstaller') THEN
    ALTER TABLE "WorkflowDefinitions" ALTER COLUMN "CompanyId" DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251125070624_AddDepartmentIdToServiceInstaller') THEN
    ALTER TABLE "UserRoles" ALTER COLUMN "CompanyId" DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251125070624_AddDepartmentIdToServiceInstaller') THEN
    ALTER TABLE "TaskItems" ALTER COLUMN "CompanyId" DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251125070624_AddDepartmentIdToServiceInstaller') THEN
    ALTER TABLE "StockMovements" ALTER COLUMN "CompanyId" DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251125070624_AddDepartmentIdToServiceInstaller') THEN
    ALTER TABLE "StockLocations" ALTER COLUMN "CompanyId" DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251125070624_AddDepartmentIdToServiceInstaller') THEN
    ALTER TABLE "StockBalances" ALTER COLUMN "CompanyId" DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251125070624_AddDepartmentIdToServiceInstaller') THEN
    ALTER TABLE "SplitterTypes" ALTER COLUMN "CompanyId" DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251125070624_AddDepartmentIdToServiceInstaller') THEN
    ALTER TABLE "Splitters" ALTER COLUMN "CompanyId" DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251125070624_AddDepartmentIdToServiceInstaller') THEN
    ALTER TABLE "SplitterPorts" ALTER COLUMN "CompanyId" DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251125070624_AddDepartmentIdToServiceInstaller') THEN
    ALTER TABLE "SiRatePlans" ALTER COLUMN "CompanyId" DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251125070624_AddDepartmentIdToServiceInstaller') THEN
    ALTER TABLE "SiLeaveRequests" ALTER COLUMN "CompanyId" DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251125070624_AddDepartmentIdToServiceInstaller') THEN
    ALTER TABLE "SiAvailabilities" ALTER COLUMN "CompanyId" DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251125070624_AddDepartmentIdToServiceInstaller') THEN
    ALTER TABLE "ServiceInstallers" ALTER COLUMN "CompanyId" DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251125070624_AddDepartmentIdToServiceInstaller') THEN
    ALTER TABLE "ServiceInstallers" ADD "DepartmentId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251125070624_AddDepartmentIdToServiceInstaller') THEN
    ALTER TABLE "SerialisedItems" ALTER COLUMN "CompanyId" DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251125070624_AddDepartmentIdToServiceInstaller') THEN
    ALTER TABLE "ScheduledSlots" ALTER COLUMN "CompanyId" DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251125070624_AddDepartmentIdToServiceInstaller') THEN
    ALTER TABLE "RmaRequests" ALTER COLUMN "CompanyId" DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251125070624_AddDepartmentIdToServiceInstaller') THEN
    ALTER TABLE "RmaRequestItems" ALTER COLUMN "CompanyId" DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251125070624_AddDepartmentIdToServiceInstaller') THEN
    ALTER TABLE "PnlPeriods" ALTER COLUMN "CompanyId" DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251125070624_AddDepartmentIdToServiceInstaller') THEN
    ALTER TABLE "PnlFacts" ALTER COLUMN "CompanyId" DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251125070624_AddDepartmentIdToServiceInstaller') THEN
    ALTER TABLE "PnlDetailPerOrders" ALTER COLUMN "CompanyId" DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251125070624_AddDepartmentIdToServiceInstaller') THEN
    ALTER TABLE "PayrollRuns" ALTER COLUMN "CompanyId" DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251125070624_AddDepartmentIdToServiceInstaller') THEN
    ALTER TABLE "PayrollPeriods" ALTER COLUMN "CompanyId" DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251125070624_AddDepartmentIdToServiceInstaller') THEN
    ALTER TABLE "PayrollLines" ALTER COLUMN "CompanyId" DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251125070624_AddDepartmentIdToServiceInstaller') THEN
    ALTER TABLE "Partners" ALTER COLUMN "CompanyId" DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251125070624_AddDepartmentIdToServiceInstaller') THEN
    ALTER TABLE "ParseSessions" ALTER COLUMN "CompanyId" DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251125070624_AddDepartmentIdToServiceInstaller') THEN
    ALTER TABLE "ParserRules" ALTER COLUMN "CompanyId" DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251125070624_AddDepartmentIdToServiceInstaller') THEN
    ALTER TABLE "ParsedOrderDrafts" ALTER COLUMN "CompanyId" DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251125070624_AddDepartmentIdToServiceInstaller') THEN
    ALTER TABLE "OverheadEntries" ALTER COLUMN "CompanyId" DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251125070624_AddDepartmentIdToServiceInstaller') THEN
    ALTER TABLE "OrderTypes" ALTER COLUMN "CompanyId" DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251125070624_AddDepartmentIdToServiceInstaller') THEN
    ALTER TABLE "OrderStatusLogs" ALTER COLUMN "CompanyId" DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251125070624_AddDepartmentIdToServiceInstaller') THEN
    ALTER TABLE "Orders" ALTER COLUMN "CompanyId" DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251125070624_AddDepartmentIdToServiceInstaller') THEN
    ALTER TABLE "OrderReschedules" ALTER COLUMN "CompanyId" DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251125070624_AddDepartmentIdToServiceInstaller') THEN
    ALTER TABLE "OrderMaterialUsage" ALTER COLUMN "CompanyId" DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251125070624_AddDepartmentIdToServiceInstaller') THEN
    ALTER TABLE "OrderDockets" ALTER COLUMN "CompanyId" DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251125070624_AddDepartmentIdToServiceInstaller') THEN
    ALTER TABLE "OrderBlockers" ALTER COLUMN "CompanyId" DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251125070624_AddDepartmentIdToServiceInstaller') THEN
    ALTER TABLE "NotificationSettings" ALTER COLUMN "CompanyId" DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251125070624_AddDepartmentIdToServiceInstaller') THEN
    ALTER TABLE "Notifications" ALTER COLUMN "CompanyId" DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251125070624_AddDepartmentIdToServiceInstaller') THEN
    ALTER TABLE "MaterialTemplates" ALTER COLUMN "CompanyId" DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251125070624_AddDepartmentIdToServiceInstaller') THEN
    ALTER TABLE "Materials" ALTER COLUMN "CompanyId" DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251125070624_AddDepartmentIdToServiceInstaller') THEN
    ALTER TABLE "MaterialAllocations" ALTER COLUMN "CompanyId" DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251125070624_AddDepartmentIdToServiceInstaller') THEN
    ALTER TABLE "KpiProfiles" ALTER COLUMN "CompanyId" DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251125070624_AddDepartmentIdToServiceInstaller') THEN
    ALTER TABLE "JobEarningRecords" ALTER COLUMN "CompanyId" DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251125070624_AddDepartmentIdToServiceInstaller') THEN
    ALTER TABLE "Invoices" ALTER COLUMN "CompanyId" DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251125070624_AddDepartmentIdToServiceInstaller') THEN
    ALTER TABLE "InvoiceLineItems" ALTER COLUMN "CompanyId" DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251125070624_AddDepartmentIdToServiceInstaller') THEN
    ALTER TABLE "InstallationTypes" ALTER COLUMN "CompanyId" DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251125070624_AddDepartmentIdToServiceInstaller') THEN
    ALTER TABLE "GeneratedDocuments" ALTER COLUMN "CompanyId" DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251125070624_AddDepartmentIdToServiceInstaller') THEN
    ALTER TABLE "Files" ALTER COLUMN "CompanyId" DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251125070624_AddDepartmentIdToServiceInstaller') THEN
    ALTER TABLE "EmailMessages" ALTER COLUMN "CompanyId" DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251125070624_AddDepartmentIdToServiceInstaller') THEN
    ALTER TABLE "DocumentTemplates" ALTER COLUMN "CompanyId" DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251125070624_AddDepartmentIdToServiceInstaller') THEN
    ALTER TABLE "Departments" ALTER COLUMN "CompanyId" DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251125070624_AddDepartmentIdToServiceInstaller') THEN
    ALTER TABLE "BuildingTypes" ALTER COLUMN "CompanyId" DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251125070624_AddDepartmentIdToServiceInstaller') THEN
    ALTER TABLE "Buildings" ALTER COLUMN "CompanyId" DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251125070624_AddDepartmentIdToServiceInstaller') THEN
    ALTER TABLE "BillingRatecards" ALTER COLUMN "CompanyId" DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251125070624_AddDepartmentIdToServiceInstaller') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251125070624_AddDepartmentIdToServiceInstaller', '10.0.3');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251125100326_AddDepartmentIdToMaterial_Update') THEN
    ALTER TABLE "Materials" ADD "DepartmentId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251125100326_AddDepartmentIdToMaterial_Update') THEN
    CREATE INDEX "IX_Materials_DepartmentId" ON "Materials" ("DepartmentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251125100326_AddDepartmentIdToMaterial_Update') THEN
    ALTER TABLE "Materials" ADD CONSTRAINT "FK_Materials_Departments_DepartmentId" FOREIGN KEY ("DepartmentId") REFERENCES "Departments" ("Id") ON DELETE SET NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251125100326_AddDepartmentIdToMaterial_Update') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251125100326_AddDepartmentIdToMaterial_Update', '10.0.3');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251125101723_AddMaterialCategory') THEN
    CREATE TABLE "MaterialCategories" (
        "Id" uuid NOT NULL,
        "Name" character varying(100) NOT NULL,
        "Description" character varying(500),
        "DisplayOrder" integer NOT NULL,
        "IsActive" boolean NOT NULL,
        "CompanyId" uuid,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_MaterialCategories" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251125101723_AddMaterialCategory') THEN
    CREATE INDEX "IX_MaterialCategories_CompanyId_IsActive" ON "MaterialCategories" ("CompanyId", "IsActive");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251125101723_AddMaterialCategory') THEN
    CREATE UNIQUE INDEX "IX_MaterialCategories_CompanyId_Name" ON "MaterialCategories" ("CompanyId", "Name");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251125101723_AddMaterialCategory') THEN
    CREATE INDEX "IX_MaterialCategories_DisplayOrder" ON "MaterialCategories" ("DisplayOrder");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251125101723_AddMaterialCategory') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251125101723_AddMaterialCategory', '10.0.3');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251125110553_AddPartnerForeignKeyToMaterial_Update') THEN
    CREATE INDEX "IX_Materials_PartnerId" ON "Materials" ("PartnerId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251125110553_AddPartnerForeignKeyToMaterial_Update') THEN
    ALTER TABLE "Materials" ADD CONSTRAINT "FK_Materials_Partners_PartnerId" FOREIGN KEY ("PartnerId") REFERENCES "Partners" ("Id") ON DELETE SET NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251125110553_AddPartnerForeignKeyToMaterial_Update') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251125110553_AddPartnerForeignKeyToMaterial_Update', '10.0.3');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251125170149_AddEmailAccounts') THEN
    CREATE TABLE "EmailAccounts" (
        "Id" uuid NOT NULL,
        "Name" character varying(200) NOT NULL,
        "Provider" character varying(50) NOT NULL,
        "Host" character varying(255),
        "Port" integer,
        "UseSsl" boolean NOT NULL,
        "Username" character varying(255) NOT NULL,
        "Password" character varying(512) NOT NULL,
        "PollIntervalSec" integer NOT NULL,
        "IsActive" boolean NOT NULL,
        "LastPolledAt" timestamp with time zone,
        "SmtpHost" character varying(255),
        "SmtpPort" integer,
        "SmtpUsername" character varying(255),
        "SmtpPassword" character varying(512),
        "SmtpUseSsl" boolean NOT NULL,
        "SmtpUseTls" boolean NOT NULL,
        "SmtpFromAddress" character varying(255),
        "SmtpFromName" character varying(255),
        "CompanyId" uuid,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_EmailAccounts" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251125170149_AddEmailAccounts') THEN
    CREATE INDEX "IX_EmailAccounts_CompanyId_Name" ON "EmailAccounts" ("CompanyId", "Name");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251125170149_AddEmailAccounts') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251125170149_AddEmailAccounts', '10.0.3');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251126150839_SyncModelChanges') THEN
    CREATE TABLE "ServiceInstallerContacts" (
        "Id" uuid NOT NULL,
        "ServiceInstallerId" uuid NOT NULL,
        "Name" text NOT NULL,
        "Phone" text,
        "Email" text,
        "ContactType" text NOT NULL,
        "IsPrimary" boolean NOT NULL,
        "CompanyId" uuid,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_ServiceInstallerContacts" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_ServiceInstallerContacts_ServiceInstallers_ServiceInstaller~" FOREIGN KEY ("ServiceInstallerId") REFERENCES "ServiceInstallers" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251126150839_SyncModelChanges') THEN
    CREATE INDEX "IX_ServiceInstallerContacts_ServiceInstallerId" ON "ServiceInstallerContacts" ("ServiceInstallerId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251126150839_SyncModelChanges') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251126150839_SyncModelChanges', '10.0.3');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251126174910_AddPnlTypesAssetsAndAccounting') THEN
    ALTER TABLE "EmailMessages" ADD "IsVip" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251126174910_AddPnlTypesAssetsAndAccounting') THEN
    ALTER TABLE "EmailMessages" ADD "MatchedRuleId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251126174910_AddPnlTypesAssetsAndAccounting') THEN
    ALTER TABLE "EmailMessages" ADD "MatchedVipEmailId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251126174910_AddPnlTypesAssetsAndAccounting') THEN
    CREATE TABLE "AssetTypes" (
        "Id" uuid NOT NULL,
        "Name" character varying(100) NOT NULL,
        "Code" character varying(20) NOT NULL,
        "Description" character varying(500),
        "DefaultDepreciationMethod" character varying(30) NOT NULL,
        "DefaultUsefulLifeMonths" integer NOT NULL DEFAULT 60,
        "DefaultSalvageValuePercent" numeric(5,2) NOT NULL DEFAULT 10.0,
        "DepreciationPnlTypeId" uuid,
        "IsActive" boolean NOT NULL DEFAULT TRUE,
        "SortOrder" integer NOT NULL DEFAULT 0,
        "CompanyId" uuid,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_AssetTypes" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251126174910_AddPnlTypesAssetsAndAccounting') THEN
    CREATE TABLE "ParserTemplates" (
        "Id" uuid NOT NULL,
        "Name" character varying(200) NOT NULL,
        "Code" character varying(50) NOT NULL,
        "PartnerPattern" character varying(500),
        "SubjectPattern" character varying(500),
        "OrderTypeId" uuid,
        "OrderTypeCode" character varying(50),
        "AutoApprove" boolean NOT NULL,
        "Priority" integer NOT NULL,
        "IsActive" boolean NOT NULL,
        "Description" character varying(1000),
        "PartnerId" uuid,
        "DefaultDepartmentId" uuid,
        "ExpectedAttachmentTypes" character varying(200),
        "CreatedByUserId" uuid NOT NULL,
        "UpdatedByUserId" uuid,
        "CompanyId" uuid,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_ParserTemplates" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251126174910_AddPnlTypesAssetsAndAccounting') THEN
    CREATE TABLE "PnlTypes" (
        "Id" uuid NOT NULL,
        "Name" character varying(100) NOT NULL,
        "Code" character varying(50) NOT NULL,
        "Description" character varying(500),
        "Category" character varying(20) NOT NULL,
        "ParentId" uuid,
        "SortOrder" integer NOT NULL DEFAULT 0,
        "IsActive" boolean NOT NULL DEFAULT TRUE,
        "IsTransactional" boolean NOT NULL DEFAULT TRUE,
        "CompanyId" uuid,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_PnlTypes" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_PnlTypes_PnlTypes_ParentId" FOREIGN KEY ("ParentId") REFERENCES "PnlTypes" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251126174910_AddPnlTypesAssetsAndAccounting') THEN
    CREATE TABLE "SupplierInvoices" (
        "Id" uuid NOT NULL,
        "InvoiceNumber" character varying(100) NOT NULL,
        "InternalReference" character varying(50),
        "SupplierName" character varying(200) NOT NULL,
        "SupplierTaxNumber" character varying(50),
        "SupplierAddress" character varying(500),
        "SupplierEmail" character varying(200),
        "InvoiceDate" timestamp with time zone NOT NULL,
        "ReceivedDate" timestamp with time zone NOT NULL,
        "DueDate" timestamp with time zone,
        "SubTotal" numeric(18,2) NOT NULL,
        "TaxAmount" numeric(18,2) NOT NULL,
        "TotalAmount" numeric(18,2) NOT NULL,
        "AmountPaid" numeric(18,2) NOT NULL DEFAULT 0.0,
        "OutstandingAmount" numeric(18,2) NOT NULL,
        "Currency" character varying(3) NOT NULL DEFAULT 'MYR',
        "Status" character varying(20) NOT NULL,
        "CostCentreId" uuid,
        "DefaultPnlTypeId" uuid,
        "Description" character varying(500),
        "Notes" character varying(2000),
        "AttachmentPath" character varying(500),
        "CreatedByUserId" uuid NOT NULL,
        "ApprovedByUserId" uuid,
        "ApprovedAt" timestamp with time zone,
        "PaidAt" timestamp with time zone,
        "CompanyId" uuid,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_SupplierInvoices" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251126174910_AddPnlTypesAssetsAndAccounting') THEN
    CREATE TABLE "VipEmails" (
        "Id" uuid NOT NULL,
        "EmailAddress" character varying(320) NOT NULL,
        "DisplayName" character varying(200),
        "Description" character varying(1000),
        "VipGroupId" uuid,
        "NotifyUserId" uuid,
        "NotifyRole" character varying(100),
        "IsActive" boolean NOT NULL,
        "CreatedByUserId" uuid NOT NULL,
        "UpdatedByUserId" uuid,
        "CompanyId" uuid,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_VipEmails" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251126174910_AddPnlTypesAssetsAndAccounting') THEN
    CREATE TABLE "VipGroups" (
        "Id" uuid NOT NULL,
        "Name" character varying(200) NOT NULL,
        "Code" character varying(50) NOT NULL,
        "Description" character varying(1000),
        "NotifyDepartmentId" uuid,
        "NotifyUserId" uuid,
        "NotifyHodUserId" uuid,
        "NotifyRole" character varying(100),
        "Priority" integer NOT NULL,
        "IsActive" boolean NOT NULL,
        "CreatedByUserId" uuid NOT NULL,
        "UpdatedByUserId" uuid,
        "CompanyId" uuid,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_VipGroups" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251126174910_AddPnlTypesAssetsAndAccounting') THEN
    CREATE TABLE "Assets" (
        "Id" uuid NOT NULL,
        "AssetTypeId" uuid NOT NULL,
        "AssetTag" character varying(50) NOT NULL,
        "Name" character varying(200) NOT NULL,
        "Description" character varying(1000),
        "SerialNumber" character varying(100),
        "ModelNumber" character varying(100),
        "Manufacturer" character varying(100),
        "Supplier" character varying(200),
        "SupplierInvoiceId" uuid,
        "PurchaseDate" timestamp with time zone NOT NULL,
        "InServiceDate" timestamp with time zone,
        "PurchaseCost" numeric(18,2) NOT NULL,
        "SalvageValue" numeric(18,2) NOT NULL,
        "DepreciationMethod" character varying(30) NOT NULL,
        "UsefulLifeMonths" integer NOT NULL,
        "CurrentBookValue" numeric(18,2) NOT NULL,
        "AccumulatedDepreciation" numeric(18,2) NOT NULL,
        "LastDepreciationDate" timestamp with time zone,
        "Status" character varying(20) NOT NULL,
        "Location" character varying(200),
        "DepartmentId" uuid,
        "AssignedToUserId" uuid,
        "CostCentreId" uuid,
        "WarrantyExpiryDate" timestamp with time zone,
        "InsurancePolicyNumber" character varying(100),
        "InsuranceExpiryDate" timestamp with time zone,
        "NextMaintenanceDate" timestamp with time zone,
        "Notes" character varying(2000),
        "IsFullyDepreciated" boolean NOT NULL DEFAULT FALSE,
        "CompanyId" uuid,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Assets" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_Assets_AssetTypes_AssetTypeId" FOREIGN KEY ("AssetTypeId") REFERENCES "AssetTypes" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251126174910_AddPnlTypesAssetsAndAccounting') THEN
    CREATE TABLE "Payments" (
        "Id" uuid NOT NULL,
        "PaymentNumber" character varying(50) NOT NULL,
        "PaymentType" character varying(10) NOT NULL,
        "PaymentMethod" character varying(20) NOT NULL,
        "PaymentDate" timestamp with time zone NOT NULL,
        "Amount" numeric(18,2) NOT NULL,
        "Currency" character varying(3) NOT NULL DEFAULT 'MYR',
        "PayerPayeeName" character varying(200) NOT NULL,
        "BankAccount" character varying(50),
        "BankReference" character varying(100),
        "ChequeNumber" character varying(50),
        "InvoiceId" uuid,
        "SupplierInvoiceId" uuid,
        "PnlTypeId" uuid,
        "CostCentreId" uuid,
        "Description" character varying(500),
        "Notes" character varying(2000),
        "AttachmentPath" character varying(500),
        "IsReconciled" boolean NOT NULL DEFAULT FALSE,
        "ReconciledAt" timestamp with time zone,
        "CreatedByUserId" uuid NOT NULL,
        "IsVoided" boolean NOT NULL DEFAULT FALSE,
        "VoidReason" character varying(500),
        "VoidedAt" timestamp with time zone,
        "CompanyId" uuid,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Payments" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_Payments_Invoices_InvoiceId" FOREIGN KEY ("InvoiceId") REFERENCES "Invoices" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_Payments_SupplierInvoices_SupplierInvoiceId" FOREIGN KEY ("SupplierInvoiceId") REFERENCES "SupplierInvoices" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251126174910_AddPnlTypesAssetsAndAccounting') THEN
    CREATE TABLE "SupplierInvoiceLineItems" (
        "Id" uuid NOT NULL,
        "SupplierInvoiceId" uuid NOT NULL,
        "LineNumber" integer NOT NULL,
        "Description" character varying(500) NOT NULL,
        "Quantity" numeric(18,4) NOT NULL DEFAULT 1.0,
        "UnitOfMeasure" character varying(20),
        "UnitPrice" numeric(18,4) NOT NULL,
        "LineTotal" numeric(18,2) NOT NULL,
        "TaxRate" numeric(5,2) NOT NULL DEFAULT 0.0,
        "TaxAmount" numeric(18,2) NOT NULL,
        "TotalWithTax" numeric(18,2) NOT NULL,
        "PnlTypeId" uuid,
        "CostCentreId" uuid,
        "AssetId" uuid,
        "Notes" character varying(500),
        "CompanyId" uuid,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_SupplierInvoiceLineItems" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_SupplierInvoiceLineItems_SupplierInvoices_SupplierInvoiceId" FOREIGN KEY ("SupplierInvoiceId") REFERENCES "SupplierInvoices" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251126174910_AddPnlTypesAssetsAndAccounting') THEN
    CREATE TABLE "AssetDepreciationEntries" (
        "Id" uuid NOT NULL,
        "AssetId" uuid NOT NULL,
        "Period" character varying(10) NOT NULL,
        "DepreciationAmount" numeric(18,2) NOT NULL,
        "OpeningBookValue" numeric(18,2) NOT NULL,
        "ClosingBookValue" numeric(18,2) NOT NULL,
        "AccumulatedDepreciation" numeric(18,2) NOT NULL,
        "PnlTypeId" uuid,
        "IsPosted" boolean NOT NULL DEFAULT FALSE,
        "CalculatedAt" timestamp with time zone NOT NULL,
        "Notes" character varying(500),
        "CompanyId" uuid,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_AssetDepreciationEntries" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_AssetDepreciationEntries_Assets_AssetId" FOREIGN KEY ("AssetId") REFERENCES "Assets" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251126174910_AddPnlTypesAssetsAndAccounting') THEN
    CREATE TABLE "AssetDisposals" (
        "Id" uuid NOT NULL,
        "AssetId" uuid NOT NULL,
        "DisposalMethod" character varying(20) NOT NULL,
        "DisposalDate" timestamp with time zone NOT NULL,
        "BookValueAtDisposal" numeric(18,2) NOT NULL,
        "DisposalProceeds" numeric(18,2) NOT NULL,
        "GainLoss" numeric(18,2) NOT NULL,
        "PnlTypeId" uuid,
        "BuyerName" character varying(200),
        "ReferenceNumber" character varying(100),
        "Reason" character varying(500),
        "Notes" character varying(2000),
        "ProcessedByUserId" uuid,
        "IsApproved" boolean NOT NULL DEFAULT FALSE,
        "ApprovedByUserId" uuid,
        "ApprovalDate" timestamp with time zone,
        "CompanyId" uuid,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_AssetDisposals" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_AssetDisposals_Assets_AssetId" FOREIGN KEY ("AssetId") REFERENCES "Assets" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251126174910_AddPnlTypesAssetsAndAccounting') THEN
    CREATE TABLE "AssetMaintenanceRecords" (
        "Id" uuid NOT NULL,
        "AssetId" uuid NOT NULL,
        "MaintenanceType" character varying(30) NOT NULL,
        "Description" character varying(1000) NOT NULL,
        "ScheduledDate" timestamp with time zone,
        "PerformedDate" timestamp with time zone,
        "NextScheduledDate" timestamp with time zone,
        "Cost" numeric(18,2) NOT NULL,
        "PnlTypeId" uuid,
        "PerformedBy" character varying(200),
        "SupplierInvoiceId" uuid,
        "ReferenceNumber" character varying(100),
        "Notes" character varying(2000),
        "IsCompleted" boolean NOT NULL DEFAULT FALSE,
        "RecordedByUserId" uuid,
        "CompanyId" uuid,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_AssetMaintenanceRecords" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_AssetMaintenanceRecords_Assets_AssetId" FOREIGN KEY ("AssetId") REFERENCES "Assets" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251126174910_AddPnlTypesAssetsAndAccounting') THEN
    CREATE INDEX "IX_AssetDepreciationEntries_AssetId" ON "AssetDepreciationEntries" ("AssetId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251126174910_AddPnlTypesAssetsAndAccounting') THEN
    CREATE UNIQUE INDEX "IX_AssetDepreciationEntries_CompanyId_AssetId_Period" ON "AssetDepreciationEntries" ("CompanyId", "AssetId", "Period");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251126174910_AddPnlTypesAssetsAndAccounting') THEN
    CREATE INDEX "IX_AssetDepreciationEntries_CompanyId_Period" ON "AssetDepreciationEntries" ("CompanyId", "Period");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251126174910_AddPnlTypesAssetsAndAccounting') THEN
    CREATE INDEX "IX_AssetDepreciationEntries_IsPosted" ON "AssetDepreciationEntries" ("IsPosted");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251126174910_AddPnlTypesAssetsAndAccounting') THEN
    CREATE UNIQUE INDEX "IX_AssetDisposals_AssetId" ON "AssetDisposals" ("AssetId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251126174910_AddPnlTypesAssetsAndAccounting') THEN
    CREATE UNIQUE INDEX "IX_AssetDisposals_CompanyId_AssetId" ON "AssetDisposals" ("CompanyId", "AssetId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251126174910_AddPnlTypesAssetsAndAccounting') THEN
    CREATE INDEX "IX_AssetDisposals_DisposalDate" ON "AssetDisposals" ("DisposalDate");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251126174910_AddPnlTypesAssetsAndAccounting') THEN
    CREATE INDEX "IX_AssetDisposals_IsApproved" ON "AssetDisposals" ("IsApproved");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251126174910_AddPnlTypesAssetsAndAccounting') THEN
    CREATE INDEX "IX_AssetMaintenanceRecords_AssetId" ON "AssetMaintenanceRecords" ("AssetId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251126174910_AddPnlTypesAssetsAndAccounting') THEN
    CREATE INDEX "IX_AssetMaintenanceRecords_CompanyId_AssetId" ON "AssetMaintenanceRecords" ("CompanyId", "AssetId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251126174910_AddPnlTypesAssetsAndAccounting') THEN
    CREATE INDEX "IX_AssetMaintenanceRecords_IsCompleted" ON "AssetMaintenanceRecords" ("IsCompleted");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251126174910_AddPnlTypesAssetsAndAccounting') THEN
    CREATE INDEX "IX_AssetMaintenanceRecords_NextScheduledDate" ON "AssetMaintenanceRecords" ("NextScheduledDate");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251126174910_AddPnlTypesAssetsAndAccounting') THEN
    CREATE INDEX "IX_AssetMaintenanceRecords_PerformedDate" ON "AssetMaintenanceRecords" ("PerformedDate");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251126174910_AddPnlTypesAssetsAndAccounting') THEN
    CREATE INDEX "IX_AssetMaintenanceRecords_ScheduledDate" ON "AssetMaintenanceRecords" ("ScheduledDate");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251126174910_AddPnlTypesAssetsAndAccounting') THEN
    CREATE INDEX "IX_Assets_AssetTypeId" ON "Assets" ("AssetTypeId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251126174910_AddPnlTypesAssetsAndAccounting') THEN
    CREATE INDEX "IX_Assets_AssignedToUserId" ON "Assets" ("AssignedToUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251126174910_AddPnlTypesAssetsAndAccounting') THEN
    CREATE UNIQUE INDEX "IX_Assets_CompanyId_AssetTag" ON "Assets" ("CompanyId", "AssetTag");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251126174910_AddPnlTypesAssetsAndAccounting') THEN
    CREATE INDEX "IX_Assets_CompanyId_AssetTypeId" ON "Assets" ("CompanyId", "AssetTypeId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251126174910_AddPnlTypesAssetsAndAccounting') THEN
    CREATE INDEX "IX_Assets_CompanyId_Status" ON "Assets" ("CompanyId", "Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251126174910_AddPnlTypesAssetsAndAccounting') THEN
    CREATE INDEX "IX_Assets_DepartmentId" ON "Assets" ("DepartmentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251126174910_AddPnlTypesAssetsAndAccounting') THEN
    CREATE INDEX "IX_Assets_SerialNumber" ON "Assets" ("SerialNumber");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251126174910_AddPnlTypesAssetsAndAccounting') THEN
    CREATE UNIQUE INDEX "IX_AssetTypes_CompanyId_Code" ON "AssetTypes" ("CompanyId", "Code");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251126174910_AddPnlTypesAssetsAndAccounting') THEN
    CREATE INDEX "IX_AssetTypes_IsActive" ON "AssetTypes" ("IsActive");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251126174910_AddPnlTypesAssetsAndAccounting') THEN
    CREATE UNIQUE INDEX "IX_ParserTemplates_CompanyId_Code" ON "ParserTemplates" ("CompanyId", "Code");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251126174910_AddPnlTypesAssetsAndAccounting') THEN
    CREATE INDEX "IX_ParserTemplates_CompanyId_Priority_IsActive" ON "ParserTemplates" ("CompanyId", "Priority", "IsActive");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251126174910_AddPnlTypesAssetsAndAccounting') THEN
    CREATE INDEX "IX_Payments_CompanyId_PaymentDate" ON "Payments" ("CompanyId", "PaymentDate");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251126174910_AddPnlTypesAssetsAndAccounting') THEN
    CREATE UNIQUE INDEX "IX_Payments_CompanyId_PaymentNumber" ON "Payments" ("CompanyId", "PaymentNumber");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251126174910_AddPnlTypesAssetsAndAccounting') THEN
    CREATE INDEX "IX_Payments_CompanyId_PaymentType" ON "Payments" ("CompanyId", "PaymentType");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251126174910_AddPnlTypesAssetsAndAccounting') THEN
    CREATE INDEX "IX_Payments_InvoiceId" ON "Payments" ("InvoiceId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251126174910_AddPnlTypesAssetsAndAccounting') THEN
    CREATE INDEX "IX_Payments_IsReconciled" ON "Payments" ("IsReconciled");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251126174910_AddPnlTypesAssetsAndAccounting') THEN
    CREATE INDEX "IX_Payments_IsVoided" ON "Payments" ("IsVoided");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251126174910_AddPnlTypesAssetsAndAccounting') THEN
    CREATE INDEX "IX_Payments_SupplierInvoiceId" ON "Payments" ("SupplierInvoiceId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251126174910_AddPnlTypesAssetsAndAccounting') THEN
    CREATE INDEX "IX_PnlTypes_CompanyId_Category" ON "PnlTypes" ("CompanyId", "Category");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251126174910_AddPnlTypesAssetsAndAccounting') THEN
    CREATE UNIQUE INDEX "IX_PnlTypes_CompanyId_Code" ON "PnlTypes" ("CompanyId", "Code");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251126174910_AddPnlTypesAssetsAndAccounting') THEN
    CREATE INDEX "IX_PnlTypes_CompanyId_ParentId" ON "PnlTypes" ("CompanyId", "ParentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251126174910_AddPnlTypesAssetsAndAccounting') THEN
    CREATE INDEX "IX_PnlTypes_IsActive" ON "PnlTypes" ("IsActive");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251126174910_AddPnlTypesAssetsAndAccounting') THEN
    CREATE INDEX "IX_PnlTypes_ParentId" ON "PnlTypes" ("ParentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251126174910_AddPnlTypesAssetsAndAccounting') THEN
    CREATE INDEX "IX_SupplierInvoiceLineItems_AssetId" ON "SupplierInvoiceLineItems" ("AssetId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251126174910_AddPnlTypesAssetsAndAccounting') THEN
    CREATE INDEX "IX_SupplierInvoiceLineItems_PnlTypeId" ON "SupplierInvoiceLineItems" ("PnlTypeId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251126174910_AddPnlTypesAssetsAndAccounting') THEN
    CREATE INDEX "IX_SupplierInvoiceLineItems_SupplierInvoiceId_LineNumber" ON "SupplierInvoiceLineItems" ("SupplierInvoiceId", "LineNumber");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251126174910_AddPnlTypesAssetsAndAccounting') THEN
    CREATE INDEX "IX_SupplierInvoices_CompanyId_InvoiceNumber" ON "SupplierInvoices" ("CompanyId", "InvoiceNumber");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251126174910_AddPnlTypesAssetsAndAccounting') THEN
    CREATE INDEX "IX_SupplierInvoices_CompanyId_Status" ON "SupplierInvoices" ("CompanyId", "Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251126174910_AddPnlTypesAssetsAndAccounting') THEN
    CREATE INDEX "IX_SupplierInvoices_CompanyId_SupplierName" ON "SupplierInvoices" ("CompanyId", "SupplierName");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251126174910_AddPnlTypesAssetsAndAccounting') THEN
    CREATE INDEX "IX_SupplierInvoices_DueDate" ON "SupplierInvoices" ("DueDate");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251126174910_AddPnlTypesAssetsAndAccounting') THEN
    CREATE INDEX "IX_SupplierInvoices_InvoiceDate" ON "SupplierInvoices" ("InvoiceDate");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251126174910_AddPnlTypesAssetsAndAccounting') THEN
    CREATE UNIQUE INDEX "IX_VipEmails_CompanyId_EmailAddress" ON "VipEmails" ("CompanyId", "EmailAddress");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251126174910_AddPnlTypesAssetsAndAccounting') THEN
    CREATE INDEX "IX_VipEmails_CompanyId_IsActive" ON "VipEmails" ("CompanyId", "IsActive");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251126174910_AddPnlTypesAssetsAndAccounting') THEN
    CREATE UNIQUE INDEX "IX_VipGroups_CompanyId_Code" ON "VipGroups" ("CompanyId", "Code");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251126174910_AddPnlTypesAssetsAndAccounting') THEN
    CREATE INDEX "IX_VipGroups_CompanyId_IsActive" ON "VipGroups" ("CompanyId", "IsActive");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251126174910_AddPnlTypesAssetsAndAccounting') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251126174910_AddPnlTypesAssetsAndAccounting', '10.0.3');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251127033039_SyncModelSnapshot') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251127033039_SyncModelSnapshot', '10.0.3');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251129161619_AddParsedMaterialsJson') THEN
    ALTER TABLE "ParsedOrderDrafts" ADD "ParsedMaterialsJson" text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251129161619_AddParsedMaterialsJson') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251129161619_AddParsedMaterialsJson', '10.0.3');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251129163409_AddDepartmentMemberships') THEN
    CREATE TABLE "DepartmentMemberships" (
        "Id" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "DepartmentId" uuid NOT NULL,
        "Role" character varying(100) NOT NULL,
        "IsDefault" boolean NOT NULL,
        "CompanyId" uuid,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_DepartmentMemberships" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_DepartmentMemberships_Departments_DepartmentId" FOREIGN KEY ("DepartmentId") REFERENCES "Departments" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_DepartmentMemberships_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251129163409_AddDepartmentMemberships') THEN
    CREATE INDEX "IX_DepartmentMemberships_DepartmentId" ON "DepartmentMemberships" ("DepartmentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251129163409_AddDepartmentMemberships') THEN
    CREATE UNIQUE INDEX "IX_DepartmentMemberships_UserId_DepartmentId" ON "DepartmentMemberships" ("UserId", "DepartmentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251129163409_AddDepartmentMemberships') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251129163409_AddDepartmentMemberships', '10.0.3');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251129164118_AddOrderDepartment') THEN
    ALTER TABLE "Orders" ADD "DepartmentId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251129164118_AddOrderDepartment') THEN
    CREATE INDEX "IX_Orders_DepartmentId" ON "Orders" ("DepartmentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251129164118_AddOrderDepartment') THEN
    ALTER TABLE "Orders" ADD CONSTRAINT "FK_Orders_Departments_DepartmentId" FOREIGN KEY ("DepartmentId") REFERENCES "Departments" ("Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251129164118_AddOrderDepartment') THEN
    UPDATE "Orders" o
                      SET "DepartmentId" = b."DepartmentId"
                      FROM "Buildings" b
                      WHERE o."BuildingId" = b."Id" AND b."DepartmentId" IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251129164118_AddOrderDepartment') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251129164118_AddOrderDepartment', '10.0.3');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251129191720_AddTimeSlots') THEN
    CREATE TABLE "TimeSlots" (
        "Id" uuid NOT NULL,
        "Time" character varying(50) NOT NULL,
        "SortOrder" integer NOT NULL,
        "IsActive" boolean NOT NULL DEFAULT TRUE,
        "CompanyId" uuid,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_TimeSlots" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251129191720_AddTimeSlots') THEN
    CREATE INDEX "IX_TimeSlots_CompanyId" ON "TimeSlots" ("CompanyId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251129191720_AddTimeSlots') THEN
    CREATE UNIQUE INDEX "IX_TimeSlots_CompanyId_Time" ON "TimeSlots" ("CompanyId", "Time");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251129191720_AddTimeSlots') THEN
    CREATE INDEX "IX_TimeSlots_SortOrder" ON "TimeSlots" ("SortOrder");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251129191720_AddTimeSlots') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251129191720_AddTimeSlots', '10.0.3');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251129193519_UpdateTimeModificationTemplates') THEN
    UPDATE "ParserTemplates"
    SET "SubjectPattern" = 'Modification-Outdoor',
        "Priority" = GREATEST("Priority", 130),
        "ExpectedAttachmentTypes" = 'xls,xlsx'
    WHERE "Code" = 'TIME_MOD_OUTDOOR';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251129193519_UpdateTimeModificationTemplates') THEN
    UPDATE "ParserTemplates"
    SET "Priority" = GREATEST("Priority", 120),
        "ExpectedAttachmentTypes" = COALESCE(NULLIF("ExpectedAttachmentTypes", ''), 'xls,xlsx')
    WHERE "Code" = 'TIME_MOD_INDOOR';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251129193519_UpdateTimeModificationTemplates') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251129193519_UpdateTimeModificationTemplates', '10.0.3');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251201043956_AddDefaultParserTemplateToEmailAccounts') THEN

                    DO $$
                    BEGIN
                        IF NOT EXISTS (
                            SELECT 1 FROM information_schema.columns 
                            WHERE table_name = 'VipEmails' 
                            AND column_name = 'DepartmentId'
                        ) THEN
                            ALTER TABLE "VipEmails" ADD COLUMN "DepartmentId" uuid NULL;
                        END IF;
                    END $$;
                
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251201043956_AddDefaultParserTemplateToEmailAccounts') THEN

                    DO $$
                    BEGIN
                        IF NOT EXISTS (
                            SELECT 1 FROM information_schema.columns 
                            WHERE table_name = 'EmailMessages' 
                            AND column_name = 'DepartmentId'
                        ) THEN
                            ALTER TABLE "EmailMessages" ADD COLUMN "DepartmentId" uuid NULL;
                        END IF;
                    END $$;
                
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251201043956_AddDefaultParserTemplateToEmailAccounts') THEN

                    DO $$
                    BEGIN
                        IF NOT EXISTS (
                            SELECT 1 FROM information_schema.columns 
                            WHERE table_name = 'EmailMessages' 
                            AND column_name = 'Direction'
                        ) THEN
                            ALTER TABLE "EmailMessages" ADD COLUMN "Direction" character varying(20) NOT NULL DEFAULT 'Inbound';
                        END IF;
                    END $$;
                
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251201043956_AddDefaultParserTemplateToEmailAccounts') THEN

                    DO $$
                    BEGIN
                        IF NOT EXISTS (
                            SELECT 1 FROM information_schema.columns 
                            WHERE table_name = 'EmailMessages' 
                            AND column_name = 'SentAt'
                        ) THEN
                            ALTER TABLE "EmailMessages" ADD COLUMN "SentAt" timestamp with time zone NULL;
                        END IF;
                    END $$;
                
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251201043956_AddDefaultParserTemplateToEmailAccounts') THEN

                    DO $$
                    BEGIN
                        -- FK for VipEmails.DepartmentId
                        IF NOT EXISTS (
                            SELECT 1 FROM information_schema.table_constraints 
                            WHERE constraint_name = 'FK_VipEmails_Departments_DepartmentId'
                        ) THEN
                            ALTER TABLE "VipEmails" 
                            ADD CONSTRAINT "FK_VipEmails_Departments_DepartmentId" 
                            FOREIGN KEY ("DepartmentId") REFERENCES "Departments"("Id") ON DELETE SET NULL;
                        END IF;

                        -- FK for EmailMessages.DepartmentId
                        IF NOT EXISTS (
                            SELECT 1 FROM information_schema.table_constraints 
                            WHERE constraint_name = 'FK_EmailMessages_Departments_DepartmentId'
                        ) THEN
                            ALTER TABLE "EmailMessages" 
                            ADD CONSTRAINT "FK_EmailMessages_Departments_DepartmentId" 
                            FOREIGN KEY ("DepartmentId") REFERENCES "Departments"("Id") ON DELETE SET NULL;
                        END IF;
                    END $$;
                
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251201043956_AddDefaultParserTemplateToEmailAccounts') THEN
    CREATE INDEX IF NOT EXISTS "IX_VipEmails_DepartmentId" ON "VipEmails" ("DepartmentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251201043956_AddDefaultParserTemplateToEmailAccounts') THEN
    CREATE INDEX IF NOT EXISTS "IX_EmailMessages_DepartmentId" ON "EmailMessages" ("DepartmentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251201043956_AddDefaultParserTemplateToEmailAccounts') THEN

                    DO $$
                    BEGIN
                        IF NOT EXISTS (
                            SELECT 1 FROM information_schema.tables 
                            WHERE table_name = 'EmailTemplates'
                        ) THEN
                            CREATE TABLE "EmailTemplates" (
                                "Id" uuid NOT NULL,
                                "Name" character varying(200) NOT NULL,
                                "Code" character varying(50) NOT NULL,
                                "EmailAccountId" uuid NULL,
                                "SubjectTemplate" character varying(500) NOT NULL,
                                "BodyTemplate" text NOT NULL,
                                "DepartmentId" uuid NULL,
                                "RelatedEntityType" character varying(50) NULL,
                                "Priority" integer NOT NULL,
                                "IsActive" boolean NOT NULL,
                                "AutoProcessReplies" boolean NOT NULL,
                                "ReplyPattern" character varying(200) NULL,
                                "Description" character varying(1000) NULL,
                                "CreatedByUserId" uuid NOT NULL,
                                "UpdatedByUserId" uuid NULL,
                                "CompanyId" uuid NULL,
                                "CreatedAt" timestamp with time zone NOT NULL,
                                "UpdatedAt" timestamp with time zone NOT NULL,
                                CONSTRAINT "PK_EmailTemplates" PRIMARY KEY ("Id")
                            );
                        END IF;
                    END $$;
                
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251201043956_AddDefaultParserTemplateToEmailAccounts') THEN

                    DO $$
                    BEGIN
                        IF NOT EXISTS (
                            SELECT 1 FROM information_schema.tables 
                            WHERE table_name = 'InvoiceSubmissionHistory'
                        ) THEN
                            CREATE TABLE "InvoiceSubmissionHistory" (
                                "Id" uuid NOT NULL,
                                "InvoiceId" uuid NOT NULL,
                                "SubmissionId" character varying(200) NOT NULL,
                                "SubmittedAt" timestamp with time zone NOT NULL,
                                "Status" character varying(50) NOT NULL,
                                "ResponseMessage" character varying(1000) NULL,
                                "ResponseCode" character varying(50) NULL,
                                "RejectionReason" character varying(500) NULL,
                                "PortalType" character varying(50) NOT NULL DEFAULT 'MyInvois',
                                "SubmittedByUserId" uuid NOT NULL,
                                "IsActive" boolean NOT NULL,
                                "PaymentStatus" character varying(50) NULL,
                                "PaymentReference" character varying(200) NULL,
                                "Notes" character varying(1000) NULL,
                                "CompanyId" uuid NULL,
                                "CreatedAt" timestamp with time zone NOT NULL,
                                "UpdatedAt" timestamp with time zone NOT NULL,
                                CONSTRAINT "PK_InvoiceSubmissionHistory" PRIMARY KEY ("Id")
                            );
                        END IF;
                    END $$;
                
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251201043956_AddDefaultParserTemplateToEmailAccounts') THEN
    CREATE INDEX IF NOT EXISTS "IX_EmailMessages_CompanyId_Direction_ReceivedAt" ON "EmailMessages" ("CompanyId", "Direction", "ReceivedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251201043956_AddDefaultParserTemplateToEmailAccounts') THEN

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
                                CREATE UNIQUE INDEX "IX_EmailTemplates_CompanyId_Code" 
                                ON "EmailTemplates" ("CompanyId", "Code");
                            END IF;

                            IF NOT EXISTS (
                                SELECT 1 FROM pg_indexes 
                                WHERE tablename = 'EmailTemplates' 
                                AND indexname = 'IX_EmailTemplates_CompanyId_DepartmentId_IsActive'
                            ) THEN
                                CREATE INDEX "IX_EmailTemplates_CompanyId_DepartmentId_IsActive" 
                                ON "EmailTemplates" ("CompanyId", "DepartmentId", "IsActive");
                            END IF;

                            IF NOT EXISTS (
                                SELECT 1 FROM pg_indexes 
                                WHERE tablename = 'EmailTemplates' 
                                AND indexname = 'IX_EmailTemplates_CompanyId_Priority_IsActive'
                            ) THEN
                                CREATE INDEX "IX_EmailTemplates_CompanyId_Priority_IsActive" 
                                ON "EmailTemplates" ("CompanyId", "Priority", "IsActive");
                            END IF;
                        END IF;
                    END $$;
                
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251201043956_AddDefaultParserTemplateToEmailAccounts') THEN

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
                                CREATE INDEX "IX_InvoiceSubmissionHistory_CompanyId_InvoiceId_IsActive" 
                                ON "InvoiceSubmissionHistory" ("CompanyId", "InvoiceId", "IsActive");
                            END IF;

                            IF NOT EXISTS (
                                SELECT 1 FROM pg_indexes 
                                WHERE tablename = 'InvoiceSubmissionHistory' 
                                AND indexname = 'IX_InvoiceSubmissionHistory_CompanyId_Status_SubmittedAt'
                            ) THEN
                                CREATE INDEX "IX_InvoiceSubmissionHistory_CompanyId_Status_SubmittedAt" 
                                ON "InvoiceSubmissionHistory" ("CompanyId", "Status", "SubmittedAt");
                            END IF;

                            IF NOT EXISTS (
                                SELECT 1 FROM pg_indexes 
                                WHERE tablename = 'InvoiceSubmissionHistory' 
                                AND indexname = 'IX_InvoiceSubmissionHistory_InvoiceId'
                            ) THEN
                                CREATE INDEX "IX_InvoiceSubmissionHistory_InvoiceId" 
                                ON "InvoiceSubmissionHistory" ("InvoiceId");
                            END IF;

                            IF NOT EXISTS (
                                SELECT 1 FROM pg_indexes 
                                WHERE tablename = 'InvoiceSubmissionHistory' 
                                AND indexname = 'IX_InvoiceSubmissionHistory_SubmissionId'
                            ) THEN
                                CREATE INDEX "IX_InvoiceSubmissionHistory_SubmissionId" 
                                ON "InvoiceSubmissionHistory" ("SubmissionId");
                            END IF;
                        END IF;
                    END $$;
                
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251201043956_AddDefaultParserTemplateToEmailAccounts') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251201043956_AddDefaultParserTemplateToEmailAccounts', '10.0.3');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202155247_AddRateEngineAndInstallationMethod') THEN
    DROP INDEX "IX_Orders_OrderTypeId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202155247_AddRateEngineAndInstallationMethod') THEN
    ALTER TABLE "Orders" ADD "InstallationMethodId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202155247_AddRateEngineAndInstallationMethod') THEN
    CREATE TABLE "CustomRates" (
        "Id" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "DepartmentId" uuid,
        "VerticalId" uuid,
        "Dimension1" character varying(100),
        "Dimension2" character varying(100),
        "Dimension3" character varying(100),
        "Dimension4" character varying(100),
        "CustomRateAmount" numeric(18,4) NOT NULL,
        "UnitOfMeasure" character varying(50) NOT NULL,
        "Currency" character varying(10) NOT NULL DEFAULT 'MYR',
        "ValidFrom" timestamp with time zone,
        "ValidTo" timestamp with time zone,
        "IsActive" boolean NOT NULL,
        "Reason" character varying(500),
        "CompanyId" uuid,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_CustomRates" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202155247_AddRateEngineAndInstallationMethod') THEN
    CREATE TABLE "GponPartnerJobRates" (
        "Id" uuid NOT NULL,
        "PartnerGroupId" uuid NOT NULL,
        "PartnerId" uuid,
        "OrderTypeId" uuid NOT NULL,
        "InstallationTypeId" uuid NOT NULL,
        "InstallationMethodId" uuid,
        "RevenueAmount" numeric(18,4) NOT NULL,
        "Currency" character varying(10) NOT NULL DEFAULT 'MYR',
        "ValidFrom" timestamp with time zone,
        "ValidTo" timestamp with time zone,
        "IsActive" boolean NOT NULL,
        "Notes" character varying(500),
        "CompanyId" uuid,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_GponPartnerJobRates" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202155247_AddRateEngineAndInstallationMethod') THEN
    CREATE TABLE "GponSiCustomRates" (
        "Id" uuid NOT NULL,
        "ServiceInstallerId" uuid NOT NULL,
        "OrderTypeId" uuid NOT NULL,
        "InstallationTypeId" uuid NOT NULL,
        "InstallationMethodId" uuid,
        "PartnerGroupId" uuid,
        "CustomPayoutAmount" numeric(18,4) NOT NULL,
        "Currency" character varying(10) NOT NULL DEFAULT 'MYR',
        "ValidFrom" timestamp with time zone,
        "ValidTo" timestamp with time zone,
        "IsActive" boolean NOT NULL,
        "Reason" character varying(500),
        "ApprovedByUserId" uuid,
        "ApprovedAt" timestamp with time zone,
        "CompanyId" uuid,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_GponSiCustomRates" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202155247_AddRateEngineAndInstallationMethod') THEN
    CREATE TABLE "GponSiJobRates" (
        "Id" uuid NOT NULL,
        "OrderTypeId" uuid NOT NULL,
        "InstallationTypeId" uuid NOT NULL,
        "InstallationMethodId" uuid,
        "SiLevel" character varying(50) NOT NULL,
        "PartnerGroupId" uuid,
        "PayoutAmount" numeric(18,4) NOT NULL,
        "Currency" character varying(10) NOT NULL DEFAULT 'MYR',
        "ValidFrom" timestamp with time zone,
        "ValidTo" timestamp with time zone,
        "IsActive" boolean NOT NULL,
        "Notes" character varying(500),
        "CompanyId" uuid,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_GponSiJobRates" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202155247_AddRateEngineAndInstallationMethod') THEN
    CREATE TABLE "RateCards" (
        "Id" uuid NOT NULL,
        "VerticalId" uuid,
        "DepartmentId" uuid,
        "RateContext" character varying(50) NOT NULL,
        "RateKind" character varying(50) NOT NULL,
        "Name" character varying(200) NOT NULL,
        "Description" character varying(1000),
        "ValidFrom" timestamp with time zone,
        "ValidTo" timestamp with time zone,
        "IsActive" boolean NOT NULL,
        "CompanyId" uuid,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_RateCards" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202155247_AddRateEngineAndInstallationMethod') THEN
    CREATE TABLE "RateCardLines" (
        "Id" uuid NOT NULL,
        "RateCardId" uuid NOT NULL,
        "Dimension1" character varying(100),
        "Dimension2" character varying(100),
        "Dimension3" character varying(100),
        "Dimension4" character varying(100),
        "PartnerGroupId" uuid,
        "PartnerId" uuid,
        "RateAmount" numeric(18,4) NOT NULL,
        "UnitOfMeasure" character varying(50) NOT NULL,
        "Currency" character varying(10) NOT NULL DEFAULT 'MYR',
        "PayoutType" character varying(50) NOT NULL,
        "ExtraJson" jsonb,
        "IsActive" boolean NOT NULL,
        "CompanyId" uuid,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_RateCardLines" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_RateCardLines_RateCards_RateCardId" FOREIGN KEY ("RateCardId") REFERENCES "RateCards" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202155247_AddRateEngineAndInstallationMethod') THEN
    CREATE INDEX "IX_Orders_InstallationMethodId" ON "Orders" ("InstallationMethodId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202155247_AddRateEngineAndInstallationMethod') THEN
    CREATE INDEX "IX_Orders_OrderTypeId_InstallationTypeId_InstallationMethodId" ON "Orders" ("OrderTypeId", "InstallationTypeId", "InstallationMethodId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202155247_AddRateEngineAndInstallationMethod') THEN
    CREATE INDEX "IX_CustomRates_UserId_DepartmentId_IsActive" ON "CustomRates" ("UserId", "DepartmentId", "IsActive");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202155247_AddRateEngineAndInstallationMethod') THEN
    CREATE INDEX "IX_CustomRates_UserId_Dimension1_Dimension2_Dimension3_Dimensi~" ON "CustomRates" ("UserId", "Dimension1", "Dimension2", "Dimension3", "Dimension4");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202155247_AddRateEngineAndInstallationMethod') THEN
    CREATE INDEX "IX_CustomRates_ValidFrom_ValidTo" ON "CustomRates" ("ValidFrom", "ValidTo");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202155247_AddRateEngineAndInstallationMethod') THEN
    CREATE INDEX "IX_GponPartnerJobRates_CompanyId_IsActive" ON "GponPartnerJobRates" ("CompanyId", "IsActive");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202155247_AddRateEngineAndInstallationMethod') THEN
    CREATE INDEX "IX_GponPartnerJobRates_PartnerGroupId_OrderTypeId_Installation~" ON "GponPartnerJobRates" ("PartnerGroupId", "OrderTypeId", "InstallationTypeId", "InstallationMethodId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202155247_AddRateEngineAndInstallationMethod') THEN
    CREATE INDEX "IX_GponPartnerJobRates_PartnerId_OrderTypeId_InstallationTypeI~" ON "GponPartnerJobRates" ("PartnerId", "OrderTypeId", "InstallationTypeId", "InstallationMethodId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202155247_AddRateEngineAndInstallationMethod') THEN
    CREATE INDEX "IX_GponPartnerJobRates_ValidFrom_ValidTo" ON "GponPartnerJobRates" ("ValidFrom", "ValidTo");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202155247_AddRateEngineAndInstallationMethod') THEN
    CREATE INDEX "IX_GponSiCustomRates_ApprovedByUserId" ON "GponSiCustomRates" ("ApprovedByUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202155247_AddRateEngineAndInstallationMethod') THEN
    CREATE INDEX "IX_GponSiCustomRates_CompanyId_IsActive" ON "GponSiCustomRates" ("CompanyId", "IsActive");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202155247_AddRateEngineAndInstallationMethod') THEN
    CREATE INDEX "IX_GponSiCustomRates_ServiceInstallerId_OrderTypeId_Installati~" ON "GponSiCustomRates" ("ServiceInstallerId", "OrderTypeId", "InstallationTypeId", "InstallationMethodId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202155247_AddRateEngineAndInstallationMethod') THEN
    CREATE INDEX "IX_GponSiCustomRates_ServiceInstallerId_PartnerGroupId" ON "GponSiCustomRates" ("ServiceInstallerId", "PartnerGroupId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202155247_AddRateEngineAndInstallationMethod') THEN
    CREATE INDEX "IX_GponSiCustomRates_ValidFrom_ValidTo" ON "GponSiCustomRates" ("ValidFrom", "ValidTo");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202155247_AddRateEngineAndInstallationMethod') THEN
    CREATE INDEX "IX_GponSiJobRates_CompanyId_IsActive" ON "GponSiJobRates" ("CompanyId", "IsActive");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202155247_AddRateEngineAndInstallationMethod') THEN
    CREATE INDEX "IX_GponSiJobRates_OrderTypeId_InstallationTypeId_InstallationM~" ON "GponSiJobRates" ("OrderTypeId", "InstallationTypeId", "InstallationMethodId", "SiLevel");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202155247_AddRateEngineAndInstallationMethod') THEN
    CREATE INDEX "IX_GponSiJobRates_PartnerGroupId_OrderTypeId_InstallationTypeId" ON "GponSiJobRates" ("PartnerGroupId", "OrderTypeId", "InstallationTypeId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202155247_AddRateEngineAndInstallationMethod') THEN
    CREATE INDEX "IX_GponSiJobRates_ValidFrom_ValidTo" ON "GponSiJobRates" ("ValidFrom", "ValidTo");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202155247_AddRateEngineAndInstallationMethod') THEN
    CREATE INDEX "IX_RateCardLines_RateCardId_Dimension1_Dimension2_Dimension3_D~" ON "RateCardLines" ("RateCardId", "Dimension1", "Dimension2", "Dimension3", "Dimension4");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202155247_AddRateEngineAndInstallationMethod') THEN
    CREATE INDEX "IX_RateCardLines_RateCardId_IsActive" ON "RateCardLines" ("RateCardId", "IsActive");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202155247_AddRateEngineAndInstallationMethod') THEN
    CREATE INDEX "IX_RateCardLines_RateCardId_PartnerGroupId_PartnerId" ON "RateCardLines" ("RateCardId", "PartnerGroupId", "PartnerId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202155247_AddRateEngineAndInstallationMethod') THEN
    CREATE INDEX "IX_RateCards_CompanyId_RateContext_RateKind_IsActive" ON "RateCards" ("CompanyId", "RateContext", "RateKind", "IsActive");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202155247_AddRateEngineAndInstallationMethod') THEN
    CREATE INDEX "IX_RateCards_CompanyId_VerticalId_DepartmentId" ON "RateCards" ("CompanyId", "VerticalId", "DepartmentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202155247_AddRateEngineAndInstallationMethod') THEN
    CREATE INDEX "IX_RateCards_ValidFrom_ValidTo" ON "RateCards" ("ValidFrom", "ValidTo");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202155247_AddRateEngineAndInstallationMethod') THEN
    ALTER TABLE "Orders" ADD CONSTRAINT "FK_Orders_InstallationMethods_InstallationMethodId" FOREIGN KEY ("InstallationMethodId") REFERENCES "InstallationMethods" ("Id") ON DELETE SET NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202155247_AddRateEngineAndInstallationMethod') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251202155247_AddRateEngineAndInstallationMethod', '10.0.3');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202155910_AddPartnerGroupIdToBillingRatecard') THEN
    ALTER TABLE "BillingRatecards" ALTER COLUMN "ServiceCategory" TYPE character varying(50);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202155910_AddPartnerGroupIdToBillingRatecard') THEN
    ALTER TABLE "BillingRatecards" ALTER COLUMN "PartnerId" DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202155910_AddPartnerGroupIdToBillingRatecard') THEN
    ALTER TABLE "BillingRatecards" ADD "PartnerGroupId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202155910_AddPartnerGroupIdToBillingRatecard') THEN
    CREATE INDEX "IX_BillingRatecards_CompanyId_PartnerGroupId_OrderTypeId_Insta~" ON "BillingRatecards" ("CompanyId", "PartnerGroupId", "OrderTypeId", "InstallationMethodId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202155910_AddPartnerGroupIdToBillingRatecard') THEN
    CREATE INDEX "IX_BillingRatecards_CompanyId_PartnerGroupId_PartnerId_OrderTy~" ON "BillingRatecards" ("CompanyId", "PartnerGroupId", "PartnerId", "OrderTypeId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202155910_AddPartnerGroupIdToBillingRatecard') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251202155910_AddPartnerGroupIdToBillingRatecard', '10.0.3');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202160130_AddRateAuditFieldsToJobEarningRecord') THEN
    ALTER TABLE "JobEarningRecords" ADD "RateId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202160130_AddRateAuditFieldsToJobEarningRecord') THEN
    ALTER TABLE "JobEarningRecords" ADD "RateSource" text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202160130_AddRateAuditFieldsToJobEarningRecord') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251202160130_AddRateAuditFieldsToJobEarningRecord', '10.0.3');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202162647_EnhancePnlDetailPerOrder') THEN
    ALTER TABLE "PnlDetailPerOrders" ADD "CalculatedAt" timestamp with time zone NOT NULL DEFAULT TIMESTAMPTZ '-infinity';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202162647_EnhancePnlDetailPerOrder') THEN
    ALTER TABLE "PnlDetailPerOrders" ADD "CompletedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202162647_EnhancePnlDetailPerOrder') THEN
    ALTER TABLE "PnlDetailPerOrders" ADD "DataQualityNotes" character varying(500);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202162647_EnhancePnlDetailPerOrder') THEN
    ALTER TABLE "PnlDetailPerOrders" ADD "DepartmentId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202162647_EnhancePnlDetailPerOrder') THEN
    ALTER TABLE "PnlDetailPerOrders" ADD "GrossProfit" numeric(18,2) NOT NULL DEFAULT 0.0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202162647_EnhancePnlDetailPerOrder') THEN
    ALTER TABLE "PnlDetailPerOrders" ADD "InstallationMethod" character varying(50);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202162647_EnhancePnlDetailPerOrder') THEN
    ALTER TABLE "PnlDetailPerOrders" ADD "InstallationType" character varying(50);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202162647_EnhancePnlDetailPerOrder') THEN
    ALTER TABLE "PnlDetailPerOrders" ADD "KpiResult" character varying(50);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202162647_EnhancePnlDetailPerOrder') THEN
    ALTER TABLE "PnlDetailPerOrders" ADD "LabourRateSource" character varying(100);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202162647_EnhancePnlDetailPerOrder') THEN
    ALTER TABLE "PnlDetailPerOrders" ADD "RescheduleCount" integer NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202162647_EnhancePnlDetailPerOrder') THEN
    ALTER TABLE "PnlDetailPerOrders" ADD "RevenueRateSource" character varying(100);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202162647_EnhancePnlDetailPerOrder') THEN
    ALTER TABLE "PnlDetailPerOrders" ADD "ServiceInstallerId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202162647_EnhancePnlDetailPerOrder') THEN
    CREATE INDEX "IX_PnlDetailPerOrders_CompanyId_Period_OrderType" ON "PnlDetailPerOrders" ("CompanyId", "Period", "OrderType");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202162647_EnhancePnlDetailPerOrder') THEN
    CREATE INDEX "IX_PnlDetailPerOrders_DepartmentId" ON "PnlDetailPerOrders" ("DepartmentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202162647_EnhancePnlDetailPerOrder') THEN
    CREATE INDEX "IX_PnlDetailPerOrders_ServiceInstallerId" ON "PnlDetailPerOrders" ("ServiceInstallerId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202162647_EnhancePnlDetailPerOrder') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251202162647_EnhancePnlDetailPerOrder', '10.0.3');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202164938_AddOrderRelocationFields') THEN
    ALTER TABLE "Orders" ADD "NewLocationNote" character varying(500);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202164938_AddOrderRelocationFields') THEN
    ALTER TABLE "Orders" ADD "OldLocationNote" character varying(500);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202164938_AddOrderRelocationFields') THEN
    ALTER TABLE "Orders" ADD "RelocationType" character varying(20);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202164938_AddOrderRelocationFields') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251202164938_AddOrderRelocationFields', '10.0.3');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202165427_AddSplitterPortStandbyApprovalFields') THEN
    ALTER TABLE "SplitterPorts" ADD "ApprovalAttachmentId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202165427_AddSplitterPortStandbyApprovalFields') THEN
    ALTER TABLE "SplitterPorts" ADD "StandbyOverrideApproved" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202165427_AddSplitterPortStandbyApprovalFields') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251202165427_AddSplitterPortStandbyApprovalFields', '10.0.3');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202173338_AddSameDayRescheduleEvidence') THEN
    ALTER TABLE "OrderReschedules" ADD "IsSameDayReschedule" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202173338_AddSameDayRescheduleEvidence') THEN
    ALTER TABLE "OrderReschedules" ADD "SameDayEvidenceAttachmentId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202173338_AddSameDayRescheduleEvidence') THEN
    ALTER TABLE "OrderReschedules" ADD "SameDayEvidenceNotes" character varying(2000);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202173338_AddSameDayRescheduleEvidence') THEN
    CREATE INDEX "IX_OrderReschedules_IsSameDayReschedule" ON "OrderReschedules" ("IsSameDayReschedule");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202173338_AddSameDayRescheduleEvidence') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251202173338_AddSameDayRescheduleEvidence', '10.0.3');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202173502_AddBlockerEvidenceFields') THEN
    ALTER TABLE "OrderBlockers" ADD "BlockerCategory" character varying(50);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202173502_AddBlockerEvidenceFields') THEN
    ALTER TABLE "OrderBlockers" ADD "EvidenceAttachmentIds" character varying(4000);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202173502_AddBlockerEvidenceFields') THEN
    ALTER TABLE "OrderBlockers" ADD "EvidenceNotes" character varying(2000);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202173502_AddBlockerEvidenceFields') THEN
    ALTER TABLE "OrderBlockers" ADD "EvidenceRequired" boolean NOT NULL DEFAULT TRUE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202173502_AddBlockerEvidenceFields') THEN
    CREATE INDEX "IX_OrderBlockers_BlockerCategory" ON "OrderBlockers" ("BlockerCategory");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202173502_AddBlockerEvidenceFields') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251202173502_AddBlockerEvidenceFields', '10.0.3');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202173612_AddDocketNumberToOrder') THEN
    ALTER TABLE "Orders" ADD "DocketNumber" character varying(100);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202173612_AddDocketNumberToOrder') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251202173612_AddDocketNumberToOrder', '10.0.3');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174507_AddAuditOverrideEntity') THEN
    CREATE TABLE "AuditOverrides" (
        "Id" uuid NOT NULL,
        "EntityType" character varying(100) NOT NULL,
        "EntityId" uuid NOT NULL,
        "OverrideType" character varying(100) NOT NULL,
        "OriginalValue" character varying(4000),
        "NewValue" character varying(4000),
        "Reason" character varying(2000) NOT NULL,
        "EvidenceAttachmentId" uuid,
        "EvidenceNotes" character varying(2000),
        "OverriddenByUserId" uuid NOT NULL,
        "OverriddenByRole" character varying(50) NOT NULL,
        "OverriddenAt" timestamp with time zone NOT NULL,
        "RequiredSecondaryApproval" boolean NOT NULL,
        "SecondaryApproverUserId" uuid,
        "SecondaryApprovedAt" timestamp with time zone,
        "CompanyId" uuid,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_AuditOverrides" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174507_AddAuditOverrideEntity') THEN
    CREATE INDEX "IX_AuditOverrides_CompanyId_EntityType_EntityId" ON "AuditOverrides" ("CompanyId", "EntityType", "EntityId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174507_AddAuditOverrideEntity') THEN
    CREATE INDEX "IX_AuditOverrides_CompanyId_OverriddenByUserId_OverriddenAt" ON "AuditOverrides" ("CompanyId", "OverriddenByUserId", "OverriddenAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174507_AddAuditOverrideEntity') THEN
    CREATE INDEX "IX_AuditOverrides_OverriddenAt" ON "AuditOverrides" ("OverriddenAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174507_AddAuditOverrideEntity') THEN
    CREATE INDEX "IX_AuditOverrides_OverrideType" ON "AuditOverrides" ("OverrideType");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174507_AddAuditOverrideEntity') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251202174507_AddAuditOverrideEntity', '10.0.3');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN

                    DO $$ 
                    BEGIN
                        -- Helper function to add column if not exists
                        CREATE OR REPLACE FUNCTION add_soft_delete_columns(table_name text) RETURNS void AS $func$
                        BEGIN
                            -- Add DeletedAt if not exists
                            IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = $1 AND column_name = 'DeletedAt') THEN
                                EXECUTE format('ALTER TABLE %I ADD COLUMN "DeletedAt" timestamp with time zone', $1);
                            END IF;
                            
                            -- Add DeletedByUserId if not exists
                            IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = $1 AND column_name = 'DeletedByUserId') THEN
                                EXECUTE format('ALTER TABLE %I ADD COLUMN "DeletedByUserId" uuid', $1);
                            END IF;
                            
                            -- Add IsDeleted if not exists
                            IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = $1 AND column_name = 'IsDeleted') THEN
                                EXECUTE format('ALTER TABLE %I ADD COLUMN "IsDeleted" boolean NOT NULL DEFAULT false', $1);
                            END IF;
                        END;
                        $func$ LANGUAGE plpgsql;
                        
                        -- Apply to all CompanyScopedEntity tables
                        PERFORM add_soft_delete_columns('WorkflowTransitions');
                        PERFORM add_soft_delete_columns('WorkflowJobs');
                        PERFORM add_soft_delete_columns('WorkflowDefinitions');
                        PERFORM add_soft_delete_columns('VipGroups');
                        PERFORM add_soft_delete_columns('VipEmails');
                        PERFORM add_soft_delete_columns('Verticals');
                        PERFORM add_soft_delete_columns('TimeSlots');
                        PERFORM add_soft_delete_columns('TaskItems');
                        PERFORM add_soft_delete_columns('SupplierInvoices');
                        PERFORM add_soft_delete_columns('SupplierInvoiceLineItems');
                        PERFORM add_soft_delete_columns('StockMovements');
                        PERFORM add_soft_delete_columns('StockLocations');
                        PERFORM add_soft_delete_columns('StockBalances');
                        PERFORM add_soft_delete_columns('SplitterTypes');
                        PERFORM add_soft_delete_columns('Splitters');
                        PERFORM add_soft_delete_columns('SplitterPorts');
                        PERFORM add_soft_delete_columns('SiRatePlans');
                        PERFORM add_soft_delete_columns('SiLeaveRequests');
                        PERFORM add_soft_delete_columns('SiAvailabilities');
                        PERFORM add_soft_delete_columns('ServiceInstallers');
                        PERFORM add_soft_delete_columns('ServiceInstallerContacts');
                        PERFORM add_soft_delete_columns('SerialisedItems');
                        PERFORM add_soft_delete_columns('ScheduledSlots');
                        PERFORM add_soft_delete_columns('RmaRequests');
                        PERFORM add_soft_delete_columns('RmaRequestItems');
                        PERFORM add_soft_delete_columns('RateCards');
                        PERFORM add_soft_delete_columns('RateCardLines');
                        PERFORM add_soft_delete_columns('PnlTypes');
                        PERFORM add_soft_delete_columns('PnlPeriods');
                        PERFORM add_soft_delete_columns('PnlFacts');
                        PERFORM add_soft_delete_columns('PnlDetailPerOrders');
                        PERFORM add_soft_delete_columns('PayrollRuns');
                        PERFORM add_soft_delete_columns('PayrollPeriods');
                        PERFORM add_soft_delete_columns('PayrollLines');
                        PERFORM add_soft_delete_columns('Payments');
                        PERFORM add_soft_delete_columns('Partners');
                        PERFORM add_soft_delete_columns('PartnerGroups');
                        PERFORM add_soft_delete_columns('ParseSessions');
                        PERFORM add_soft_delete_columns('ParserTemplates');
                        PERFORM add_soft_delete_columns('ParserRules');
                        PERFORM add_soft_delete_columns('ParsedOrderDrafts');
                        PERFORM add_soft_delete_columns('OverheadEntries');
                        PERFORM add_soft_delete_columns('OrderTypes');
                        PERFORM add_soft_delete_columns('OrderStatusLogs');
                        PERFORM add_soft_delete_columns('Orders');
                        PERFORM add_soft_delete_columns('OrderReschedules');
                        PERFORM add_soft_delete_columns('OrderMaterialUsage');
                        PERFORM add_soft_delete_columns('OrderDockets');
                        PERFORM add_soft_delete_columns('OrderBlockers');
                        PERFORM add_soft_delete_columns('NotificationSettings');
                        PERFORM add_soft_delete_columns('Notifications');
                        PERFORM add_soft_delete_columns('MaterialTemplates');
                        PERFORM add_soft_delete_columns('Materials');
                        PERFORM add_soft_delete_columns('MaterialCategories');
                        PERFORM add_soft_delete_columns('MaterialAllocations');
                        PERFORM add_soft_delete_columns('KpiProfiles');
                        PERFORM add_soft_delete_columns('JobEarningRecords');
                        PERFORM add_soft_delete_columns('InvoiceSubmissionHistory');
                        PERFORM add_soft_delete_columns('Invoices');
                        PERFORM add_soft_delete_columns('InvoiceLineItems');
                        PERFORM add_soft_delete_columns('InstallationTypes');
                        PERFORM add_soft_delete_columns('InstallationMethods');
                        PERFORM add_soft_delete_columns('HubBoxes');
                        PERFORM add_soft_delete_columns('GponSiJobRates');
                        PERFORM add_soft_delete_columns('GponSiCustomRates');
                        PERFORM add_soft_delete_columns('GponPartnerJobRates');
                        PERFORM add_soft_delete_columns('GeneratedDocuments');
                        PERFORM add_soft_delete_columns('EmailTemplates');
                        PERFORM add_soft_delete_columns('EmailMessages');
                        PERFORM add_soft_delete_columns('EmailAccounts');
                        PERFORM add_soft_delete_columns('DocumentTemplates');
                        PERFORM add_soft_delete_columns('DocumentPlaceholderDefinitions');
                        PERFORM add_soft_delete_columns('DeliveryOrders');
                        PERFORM add_soft_delete_columns('DepartmentMemberships');
                        PERFORM add_soft_delete_columns('Departments');
                        PERFORM add_soft_delete_columns('CustomRates');
                        PERFORM add_soft_delete_columns('CostCentres');
                        PERFORM add_soft_delete_columns('Companies');
                        PERFORM add_soft_delete_columns('Buildings');
                        PERFORM add_soft_delete_columns('BuildingTypes');
                        PERFORM add_soft_delete_columns('BuildingContacts');
                        PERFORM add_soft_delete_columns('BuildingBlocks');
                        PERFORM add_soft_delete_columns('BillingRatecards');
                        PERFORM add_soft_delete_columns('BackgroundJobs');
                        PERFORM add_soft_delete_columns('AuditOverrides');
                        PERFORM add_soft_delete_columns('AssetTypes');
                        PERFORM add_soft_delete_columns('Assets');
                        PERFORM add_soft_delete_columns('AssetMaintenances');
                        PERFORM add_soft_delete_columns('AssetDisposals');
                        PERFORM add_soft_delete_columns('AssetDepreciations');
                        
                        -- Drop the helper function
                        DROP FUNCTION add_soft_delete_columns(text);
                    END $$;
                
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "WorkflowTransitions" ADD "DeletedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "WorkflowTransitions" ADD "DeletedByUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "WorkflowTransitions" ADD "IsDeleted" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "WorkflowJobs" ADD "DeletedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "WorkflowJobs" ADD "DeletedByUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "WorkflowJobs" ADD "IsDeleted" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "WorkflowDefinitions" ADD "DeletedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "WorkflowDefinitions" ADD "DeletedByUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "WorkflowDefinitions" ADD "IsDeleted" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "VipGroups" ADD "DeletedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "VipGroups" ADD "DeletedByUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "VipGroups" ADD "IsDeleted" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "VipEmails" ADD "DeletedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "VipEmails" ADD "DeletedByUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "VipEmails" ADD "IsDeleted" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "Verticals" ADD "DeletedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "Verticals" ADD "DeletedByUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "Verticals" ADD "IsDeleted" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "TimeSlots" ADD "DeletedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "TimeSlots" ADD "DeletedByUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "TimeSlots" ADD "IsDeleted" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "TaskItems" ADD "DeletedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "TaskItems" ADD "DeletedByUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "TaskItems" ADD "IsDeleted" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "SupplierInvoices" ADD "DeletedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "SupplierInvoices" ADD "DeletedByUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "SupplierInvoices" ADD "IsDeleted" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "SupplierInvoiceLineItems" ADD "DeletedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "SupplierInvoiceLineItems" ADD "DeletedByUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "SupplierInvoiceLineItems" ADD "IsDeleted" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "StockMovements" ADD "DeletedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "StockMovements" ADD "DeletedByUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "StockMovements" ADD "IsDeleted" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "StockLocations" ADD "DeletedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "StockLocations" ADD "DeletedByUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "StockLocations" ADD "IsDeleted" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "StockBalances" ADD "DeletedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "StockBalances" ADD "DeletedByUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "StockBalances" ADD "IsDeleted" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "SplitterTypes" ADD "DeletedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "SplitterTypes" ADD "DeletedByUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "SplitterTypes" ADD "IsDeleted" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "Splitters" ADD "DeletedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "Splitters" ADD "DeletedByUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "Splitters" ADD "IsDeleted" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "SplitterPorts" ADD "DeletedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "SplitterPorts" ADD "DeletedByUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "SplitterPorts" ADD "IsDeleted" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "SiRatePlans" ADD "DeletedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "SiRatePlans" ADD "DeletedByUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "SiRatePlans" ADD "IsDeleted" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "SiLeaveRequests" ADD "DeletedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "SiLeaveRequests" ADD "DeletedByUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "SiLeaveRequests" ADD "IsDeleted" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "SiAvailabilities" ADD "DeletedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "SiAvailabilities" ADD "DeletedByUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "SiAvailabilities" ADD "IsDeleted" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "ServiceInstallers" ADD "DeletedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "ServiceInstallers" ADD "DeletedByUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "ServiceInstallers" ADD "IsDeleted" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "ServiceInstallerContacts" ADD "DeletedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "ServiceInstallerContacts" ADD "DeletedByUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "ServiceInstallerContacts" ADD "IsDeleted" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "SerialisedItems" ADD "DeletedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "SerialisedItems" ADD "DeletedByUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "SerialisedItems" ADD "IsDeleted" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "ScheduledSlots" ADD "DeletedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "ScheduledSlots" ADD "DeletedByUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "ScheduledSlots" ADD "IsDeleted" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "RmaRequests" ADD "DeletedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "RmaRequests" ADD "DeletedByUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "RmaRequests" ADD "IsDeleted" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "RmaRequestItems" ADD "DeletedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "RmaRequestItems" ADD "DeletedByUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "RmaRequestItems" ADD "IsDeleted" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "RateCards" ADD "DeletedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "RateCards" ADD "DeletedByUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "RateCards" ADD "IsDeleted" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "RateCardLines" ADD "DeletedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "RateCardLines" ADD "DeletedByUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "RateCardLines" ADD "IsDeleted" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "PnlTypes" ADD "DeletedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "PnlTypes" ADD "DeletedByUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "PnlTypes" ADD "IsDeleted" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "PnlPeriods" ADD "DeletedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "PnlPeriods" ADD "DeletedByUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "PnlPeriods" ADD "IsDeleted" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "PnlFacts" ADD "DeletedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "PnlFacts" ADD "DeletedByUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "PnlFacts" ADD "IsDeleted" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "PnlDetailPerOrders" ADD "DeletedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "PnlDetailPerOrders" ADD "DeletedByUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "PnlDetailPerOrders" ADD "IsDeleted" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "PayrollRuns" ADD "DeletedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "PayrollRuns" ADD "DeletedByUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "PayrollRuns" ADD "IsDeleted" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "PayrollPeriods" ADD "DeletedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "PayrollPeriods" ADD "DeletedByUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "PayrollPeriods" ADD "IsDeleted" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "PayrollLines" ADD "DeletedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "PayrollLines" ADD "DeletedByUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "PayrollLines" ADD "IsDeleted" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "Payments" ADD "DeletedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "Payments" ADD "DeletedByUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "Payments" ADD "IsDeleted" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "Partners" ADD "DeletedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "Partners" ADD "DeletedByUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "Partners" ADD "IsDeleted" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "PartnerGroups" ADD "DeletedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "PartnerGroups" ADD "DeletedByUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "PartnerGroups" ADD "IsDeleted" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "ParseSessions" ADD "DeletedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "ParseSessions" ADD "DeletedByUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "ParseSessions" ADD "IsDeleted" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "ParserTemplates" ADD "DeletedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "ParserTemplates" ADD "DeletedByUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "ParserTemplates" ADD "IsDeleted" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "ParserRules" ADD "DeletedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "ParserRules" ADD "DeletedByUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "ParserRules" ADD "IsDeleted" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "ParsedOrderDrafts" ADD "DeletedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "ParsedOrderDrafts" ADD "DeletedByUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "ParsedOrderDrafts" ADD "IsDeleted" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "OverheadEntries" ADD "DeletedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "OverheadEntries" ADD "DeletedByUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "OverheadEntries" ADD "IsDeleted" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "OrderTypes" ADD "DeletedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "OrderTypes" ADD "DeletedByUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "OrderTypes" ADD "IsDeleted" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "OrderStatusLogs" ADD "DeletedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "OrderStatusLogs" ADD "DeletedByUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "OrderStatusLogs" ADD "IsDeleted" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "Orders" ADD "DeletedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "Orders" ADD "DeletedByUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "Orders" ADD "IsDeleted" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "OrderReschedules" ADD "DeletedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "OrderReschedules" ADD "DeletedByUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "OrderReschedules" ADD "IsDeleted" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "OrderMaterialUsage" ADD "DeletedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "OrderMaterialUsage" ADD "DeletedByUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "OrderMaterialUsage" ADD "IsDeleted" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "OrderDockets" ADD "DeletedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "OrderDockets" ADD "DeletedByUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "OrderDockets" ADD "IsDeleted" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "OrderBlockers" ADD "DeletedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "OrderBlockers" ADD "DeletedByUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "OrderBlockers" ADD "IsDeleted" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "NotificationSettings" ADD "DeletedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "NotificationSettings" ADD "DeletedByUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "NotificationSettings" ADD "IsDeleted" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "Notifications" ADD "DeletedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "Notifications" ADD "DeletedByUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "Notifications" ADD "IsDeleted" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "MaterialTemplates" ADD "DeletedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "MaterialTemplates" ADD "DeletedByUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "MaterialTemplates" ADD "IsDeleted" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "Materials" ADD "DeletedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "Materials" ADD "DeletedByUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "Materials" ADD "IsDeleted" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "MaterialCategories" ADD "DeletedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "MaterialCategories" ADD "DeletedByUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "MaterialCategories" ADD "IsDeleted" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "MaterialAllocations" ADD "DeletedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "MaterialAllocations" ADD "DeletedByUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "MaterialAllocations" ADD "IsDeleted" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "KpiProfiles" ADD "DeletedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "KpiProfiles" ADD "DeletedByUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "KpiProfiles" ADD "IsDeleted" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "JobEarningRecords" ADD "DeletedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "JobEarningRecords" ADD "DeletedByUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "JobEarningRecords" ADD "IsDeleted" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "InvoiceSubmissionHistory" ADD "DeletedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "InvoiceSubmissionHistory" ADD "DeletedByUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "InvoiceSubmissionHistory" ADD "IsDeleted" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "Invoices" ADD "DeletedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "Invoices" ADD "DeletedByUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "Invoices" ADD "IsDeleted" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "InvoiceLineItems" ADD "DeletedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "InvoiceLineItems" ADD "DeletedByUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "InvoiceLineItems" ADD "IsDeleted" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "InstallationTypes" ADD "DeletedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "InstallationTypes" ADD "DeletedByUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "InstallationTypes" ADD "IsDeleted" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "InstallationMethods" ADD "DeletedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "InstallationMethods" ADD "DeletedByUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "InstallationMethods" ADD "IsDeleted" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "GponSiJobRates" ADD "DeletedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "GponSiJobRates" ADD "DeletedByUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "GponSiJobRates" ADD "IsDeleted" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "GponSiCustomRates" ADD "DeletedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "GponSiCustomRates" ADD "DeletedByUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "GponSiCustomRates" ADD "IsDeleted" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "GponPartnerJobRates" ADD "DeletedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "GponPartnerJobRates" ADD "DeletedByUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "GponPartnerJobRates" ADD "IsDeleted" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "GeneratedDocuments" ADD "DeletedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "GeneratedDocuments" ADD "DeletedByUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "GeneratedDocuments" ADD "IsDeleted" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "Files" ADD "DeletedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "Files" ADD "DeletedByUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "Files" ADD "IsDeleted" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "EmailTemplates" ADD "DeletedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "EmailTemplates" ADD "DeletedByUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "EmailTemplates" ADD "IsDeleted" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "EmailMessages" ADD "DeletedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "EmailMessages" ADD "DeletedByUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "EmailMessages" ADD "IsDeleted" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "EmailAccounts" ADD "DeletedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "EmailAccounts" ADD "DeletedByUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "EmailAccounts" ADD "IsDeleted" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "DocumentTemplates" ADD "DeletedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "DocumentTemplates" ADD "DeletedByUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "DocumentTemplates" ADD "IsDeleted" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "Departments" ADD "DeletedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "Departments" ADD "DeletedByUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "Departments" ADD "IsDeleted" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "DepartmentMemberships" ADD "DeletedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "DepartmentMemberships" ADD "DeletedByUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "DepartmentMemberships" ADD "IsDeleted" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "CustomRates" ADD "DeletedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "CustomRates" ADD "DeletedByUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "CustomRates" ADD "IsDeleted" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "CostCentres" ADD "DeletedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "CostCentres" ADD "DeletedByUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "CostCentres" ADD "IsDeleted" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "CompanyDocuments" ADD "DeletedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "CompanyDocuments" ADD "DeletedByUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "CompanyDocuments" ADD "IsDeleted" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "BuildingTypes" ADD "DeletedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "BuildingTypes" ADD "DeletedByUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "BuildingTypes" ADD "IsDeleted" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "Buildings" ADD "DeletedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "Buildings" ADD "DeletedByUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "Buildings" ADD "IsDeleted" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "BillingRatecards" ADD "DeletedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "BillingRatecards" ADD "DeletedByUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "BillingRatecards" ADD "IsDeleted" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "AuditOverrides" ADD "DeletedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "AuditOverrides" ADD "DeletedByUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "AuditOverrides" ADD "IsDeleted" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "AssetTypes" ADD "DeletedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "AssetTypes" ADD "DeletedByUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "AssetTypes" ADD "IsDeleted" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "Assets" ADD "DeletedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "Assets" ADD "DeletedByUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "Assets" ADD "IsDeleted" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "AssetMaintenanceRecords" ADD "DeletedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "AssetMaintenanceRecords" ADD "DeletedByUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "AssetMaintenanceRecords" ADD "IsDeleted" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "AssetDisposals" ADD "DeletedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "AssetDisposals" ADD "DeletedByUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "AssetDisposals" ADD "IsDeleted" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "AssetDepreciationEntries" ADD "DeletedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "AssetDepreciationEntries" ADD "DeletedByUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    ALTER TABLE "AssetDepreciationEntries" ADD "IsDeleted" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174653_AddSoftDeleteToCompanyScopedEntities') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251202174653_AddSoftDeleteToCompanyScopedEntities', '10.0.3');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174940_AddRowVersionConcurrencyTokens') THEN
    ALTER TABLE "WorkflowTransitions" ADD "RowVersion" bytea;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174940_AddRowVersionConcurrencyTokens') THEN
    ALTER TABLE "WorkflowJobs" ADD "RowVersion" bytea;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174940_AddRowVersionConcurrencyTokens') THEN
    ALTER TABLE "WorkflowDefinitions" ADD "RowVersion" bytea;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174940_AddRowVersionConcurrencyTokens') THEN
    ALTER TABLE "VipGroups" ADD "RowVersion" bytea;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174940_AddRowVersionConcurrencyTokens') THEN
    ALTER TABLE "VipEmails" ADD "RowVersion" bytea;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174940_AddRowVersionConcurrencyTokens') THEN
    ALTER TABLE "Verticals" ADD "RowVersion" bytea;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174940_AddRowVersionConcurrencyTokens') THEN
    ALTER TABLE "TimeSlots" ADD "RowVersion" bytea;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174940_AddRowVersionConcurrencyTokens') THEN
    ALTER TABLE "TaskItems" ADD "RowVersion" bytea;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174940_AddRowVersionConcurrencyTokens') THEN
    ALTER TABLE "SupplierInvoices" ADD "RowVersion" bytea;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174940_AddRowVersionConcurrencyTokens') THEN
    ALTER TABLE "SupplierInvoiceLineItems" ADD "RowVersion" bytea;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174940_AddRowVersionConcurrencyTokens') THEN
    ALTER TABLE "StockMovements" ADD "RowVersion" bytea;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174940_AddRowVersionConcurrencyTokens') THEN
    ALTER TABLE "StockLocations" ADD "RowVersion" bytea;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174940_AddRowVersionConcurrencyTokens') THEN
    ALTER TABLE "StockBalances" ADD "RowVersion" bytea;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174940_AddRowVersionConcurrencyTokens') THEN
    ALTER TABLE "SplitterTypes" ADD "RowVersion" bytea;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174940_AddRowVersionConcurrencyTokens') THEN
    ALTER TABLE "Splitters" ADD "RowVersion" bytea;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174940_AddRowVersionConcurrencyTokens') THEN
    ALTER TABLE "SplitterPorts" ADD "RowVersion" bytea;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174940_AddRowVersionConcurrencyTokens') THEN
    ALTER TABLE "SiRatePlans" ADD "RowVersion" bytea;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174940_AddRowVersionConcurrencyTokens') THEN
    ALTER TABLE "SiLeaveRequests" ADD "RowVersion" bytea;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174940_AddRowVersionConcurrencyTokens') THEN
    ALTER TABLE "SiAvailabilities" ADD "RowVersion" bytea;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174940_AddRowVersionConcurrencyTokens') THEN
    ALTER TABLE "ServiceInstallers" ADD "RowVersion" bytea;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174940_AddRowVersionConcurrencyTokens') THEN
    ALTER TABLE "ServiceInstallerContacts" ADD "RowVersion" bytea;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174940_AddRowVersionConcurrencyTokens') THEN
    ALTER TABLE "SerialisedItems" ADD "RowVersion" bytea;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174940_AddRowVersionConcurrencyTokens') THEN
    ALTER TABLE "ScheduledSlots" ADD "RowVersion" bytea;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174940_AddRowVersionConcurrencyTokens') THEN
    ALTER TABLE "RmaRequests" ADD "RowVersion" bytea;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174940_AddRowVersionConcurrencyTokens') THEN
    ALTER TABLE "RmaRequestItems" ADD "RowVersion" bytea;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174940_AddRowVersionConcurrencyTokens') THEN
    ALTER TABLE "RateCards" ADD "RowVersion" bytea;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174940_AddRowVersionConcurrencyTokens') THEN
    ALTER TABLE "RateCardLines" ADD "RowVersion" bytea;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174940_AddRowVersionConcurrencyTokens') THEN
    ALTER TABLE "PnlTypes" ADD "RowVersion" bytea;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174940_AddRowVersionConcurrencyTokens') THEN
    ALTER TABLE "PnlPeriods" ADD "RowVersion" bytea;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174940_AddRowVersionConcurrencyTokens') THEN
    ALTER TABLE "PnlFacts" ADD "RowVersion" bytea;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174940_AddRowVersionConcurrencyTokens') THEN
    ALTER TABLE "PnlDetailPerOrders" ADD "RowVersion" bytea;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174940_AddRowVersionConcurrencyTokens') THEN
    ALTER TABLE "PayrollRuns" ADD "RowVersion" bytea;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174940_AddRowVersionConcurrencyTokens') THEN
    ALTER TABLE "PayrollPeriods" ADD "RowVersion" bytea;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174940_AddRowVersionConcurrencyTokens') THEN
    ALTER TABLE "PayrollLines" ADD "RowVersion" bytea;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174940_AddRowVersionConcurrencyTokens') THEN
    ALTER TABLE "Payments" ADD "RowVersion" bytea;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174940_AddRowVersionConcurrencyTokens') THEN
    ALTER TABLE "Partners" ADD "RowVersion" bytea;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174940_AddRowVersionConcurrencyTokens') THEN
    ALTER TABLE "PartnerGroups" ADD "RowVersion" bytea;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174940_AddRowVersionConcurrencyTokens') THEN
    ALTER TABLE "ParseSessions" ADD "RowVersion" bytea;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174940_AddRowVersionConcurrencyTokens') THEN
    ALTER TABLE "ParserTemplates" ADD "RowVersion" bytea;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174940_AddRowVersionConcurrencyTokens') THEN
    ALTER TABLE "ParserRules" ADD "RowVersion" bytea;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174940_AddRowVersionConcurrencyTokens') THEN
    ALTER TABLE "ParsedOrderDrafts" ADD "RowVersion" bytea;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174940_AddRowVersionConcurrencyTokens') THEN
    ALTER TABLE "OverheadEntries" ADD "RowVersion" bytea;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174940_AddRowVersionConcurrencyTokens') THEN
    ALTER TABLE "OrderTypes" ADD "RowVersion" bytea;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174940_AddRowVersionConcurrencyTokens') THEN
    ALTER TABLE "OrderStatusLogs" ADD "RowVersion" bytea;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174940_AddRowVersionConcurrencyTokens') THEN
    ALTER TABLE "Orders" ADD "RowVersion" bytea;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174940_AddRowVersionConcurrencyTokens') THEN
    ALTER TABLE "OrderReschedules" ADD "RowVersion" bytea;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174940_AddRowVersionConcurrencyTokens') THEN
    ALTER TABLE "OrderMaterialUsage" ADD "RowVersion" bytea;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174940_AddRowVersionConcurrencyTokens') THEN
    ALTER TABLE "OrderDockets" ADD "RowVersion" bytea;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174940_AddRowVersionConcurrencyTokens') THEN
    ALTER TABLE "OrderBlockers" ADD "RowVersion" bytea;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174940_AddRowVersionConcurrencyTokens') THEN
    ALTER TABLE "NotificationSettings" ADD "RowVersion" bytea;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174940_AddRowVersionConcurrencyTokens') THEN
    ALTER TABLE "Notifications" ADD "RowVersion" bytea;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174940_AddRowVersionConcurrencyTokens') THEN
    ALTER TABLE "MaterialTemplates" ADD "RowVersion" bytea;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174940_AddRowVersionConcurrencyTokens') THEN
    ALTER TABLE "Materials" ADD "RowVersion" bytea;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174940_AddRowVersionConcurrencyTokens') THEN
    ALTER TABLE "MaterialCategories" ADD "RowVersion" bytea;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174940_AddRowVersionConcurrencyTokens') THEN
    ALTER TABLE "MaterialAllocations" ADD "RowVersion" bytea;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174940_AddRowVersionConcurrencyTokens') THEN
    ALTER TABLE "KpiProfiles" ADD "RowVersion" bytea;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174940_AddRowVersionConcurrencyTokens') THEN
    ALTER TABLE "JobEarningRecords" ADD "RowVersion" bytea;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174940_AddRowVersionConcurrencyTokens') THEN
    ALTER TABLE "InvoiceSubmissionHistory" ADD "RowVersion" bytea;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174940_AddRowVersionConcurrencyTokens') THEN
    ALTER TABLE "Invoices" ADD "RowVersion" bytea;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174940_AddRowVersionConcurrencyTokens') THEN
    ALTER TABLE "InvoiceLineItems" ADD "RowVersion" bytea;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174940_AddRowVersionConcurrencyTokens') THEN
    ALTER TABLE "InstallationTypes" ADD "RowVersion" bytea;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174940_AddRowVersionConcurrencyTokens') THEN
    ALTER TABLE "InstallationMethods" ADD "RowVersion" bytea;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174940_AddRowVersionConcurrencyTokens') THEN
    ALTER TABLE "GponSiJobRates" ADD "RowVersion" bytea;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174940_AddRowVersionConcurrencyTokens') THEN
    ALTER TABLE "GponSiCustomRates" ADD "RowVersion" bytea;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174940_AddRowVersionConcurrencyTokens') THEN
    ALTER TABLE "GponPartnerJobRates" ADD "RowVersion" bytea;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174940_AddRowVersionConcurrencyTokens') THEN
    ALTER TABLE "GeneratedDocuments" ADD "RowVersion" bytea;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174940_AddRowVersionConcurrencyTokens') THEN
    ALTER TABLE "Files" ADD "RowVersion" bytea;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174940_AddRowVersionConcurrencyTokens') THEN
    ALTER TABLE "EmailTemplates" ADD "RowVersion" bytea;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174940_AddRowVersionConcurrencyTokens') THEN
    ALTER TABLE "EmailMessages" ADD "RowVersion" bytea;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174940_AddRowVersionConcurrencyTokens') THEN
    ALTER TABLE "EmailAccounts" ADD "RowVersion" bytea;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174940_AddRowVersionConcurrencyTokens') THEN
    ALTER TABLE "DocumentTemplates" ADD "RowVersion" bytea;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174940_AddRowVersionConcurrencyTokens') THEN
    ALTER TABLE "Departments" ADD "RowVersion" bytea;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174940_AddRowVersionConcurrencyTokens') THEN
    ALTER TABLE "DepartmentMemberships" ADD "RowVersion" bytea;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174940_AddRowVersionConcurrencyTokens') THEN
    ALTER TABLE "CustomRates" ADD "RowVersion" bytea;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174940_AddRowVersionConcurrencyTokens') THEN
    ALTER TABLE "CostCentres" ADD "RowVersion" bytea;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174940_AddRowVersionConcurrencyTokens') THEN
    ALTER TABLE "CompanyDocuments" ADD "RowVersion" bytea;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174940_AddRowVersionConcurrencyTokens') THEN
    ALTER TABLE "BuildingTypes" ADD "RowVersion" bytea;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174940_AddRowVersionConcurrencyTokens') THEN
    ALTER TABLE "Buildings" ADD "RowVersion" bytea;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174940_AddRowVersionConcurrencyTokens') THEN
    ALTER TABLE "BillingRatecards" ADD "RowVersion" bytea;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174940_AddRowVersionConcurrencyTokens') THEN
    ALTER TABLE "AuditOverrides" ADD "RowVersion" bytea;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174940_AddRowVersionConcurrencyTokens') THEN
    ALTER TABLE "AssetTypes" ADD "RowVersion" bytea;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174940_AddRowVersionConcurrencyTokens') THEN
    ALTER TABLE "Assets" ADD "RowVersion" bytea;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174940_AddRowVersionConcurrencyTokens') THEN
    ALTER TABLE "AssetMaintenanceRecords" ADD "RowVersion" bytea;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174940_AddRowVersionConcurrencyTokens') THEN
    ALTER TABLE "AssetDisposals" ADD "RowVersion" bytea;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174940_AddRowVersionConcurrencyTokens') THEN
    ALTER TABLE "AssetDepreciationEntries" ADD "RowVersion" bytea;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174940_AddRowVersionConcurrencyTokens') THEN
    CREATE INDEX "IX_OrderReschedules_OrderId" ON "OrderReschedules" ("OrderId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174940_AddRowVersionConcurrencyTokens') THEN
    ALTER TABLE "OrderBlockers" ADD CONSTRAINT "FK_OrderBlockers_Orders_OrderId" FOREIGN KEY ("OrderId") REFERENCES "Orders" ("Id") ON DELETE CASCADE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174940_AddRowVersionConcurrencyTokens') THEN
    ALTER TABLE "OrderReschedules" ADD CONSTRAINT "FK_OrderReschedules_Orders_OrderId" FOREIGN KEY ("OrderId") REFERENCES "Orders" ("Id") ON DELETE CASCADE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174940_AddRowVersionConcurrencyTokens') THEN
    ALTER TABLE "OrderStatusLogs" ADD CONSTRAINT "FK_OrderStatusLogs_Orders_OrderId" FOREIGN KEY ("OrderId") REFERENCES "Orders" ("Id") ON DELETE CASCADE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202174940_AddRowVersionConcurrencyTokens') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251202174940_AddRowVersionConcurrencyTokens', '10.0.3');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251203001308_AddProcurementSalesProjectEntities') THEN
    CREATE TABLE delivery_orders (
        "Id" uuid NOT NULL,
        "DoNumber" character varying(50) NOT NULL,
        "DoDate" timestamp with time zone NOT NULL,
        "DoType" character varying(50) NOT NULL,
        "Status" character varying(50) NOT NULL,
        "SourceLocationId" uuid,
        "DestinationLocationId" uuid,
        "OrderId" uuid,
        "PurchaseOrderId" uuid,
        "ProjectId" uuid,
        "RecipientName" character varying(200) NOT NULL,
        "RecipientPhone" character varying(50),
        "RecipientEmail" character varying(200),
        "DeliveryAddress" character varying(500) NOT NULL,
        "City" character varying(100),
        "State" character varying(100),
        "Postcode" character varying(20),
        "ExpectedDeliveryDate" timestamp with time zone,
        "ActualDeliveryDate" timestamp with time zone,
        "DeliveryPerson" character varying(100),
        "VehicleNumber" character varying(50),
        "Notes" character varying(2000),
        "InternalNotes" character varying(2000),
        "RecipientSignature" text,
        "ReceivedByName" character varying(200),
        "ReceivedAt" timestamp with time zone,
        "CreatedByUserId" uuid NOT NULL,
        "CompanyId" uuid,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "IsDeleted" boolean NOT NULL,
        "DeletedAt" timestamp with time zone,
        "DeletedByUserId" uuid,
        "RowVersion" bytea,
        CONSTRAINT "PK_delivery_orders" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251203001308_AddProcurementSalesProjectEntities') THEN
    CREATE TABLE projects (
        "Id" uuid NOT NULL,
        "ProjectCode" character varying(50) NOT NULL,
        "Name" character varying(200) NOT NULL,
        "Description" character varying(2000),
        "PartnerId" uuid,
        "DepartmentId" uuid,
        "CostCentreId" uuid,
        "ProjectType" character varying(50) NOT NULL,
        "Status" character varying(50) NOT NULL,
        "CustomerName" character varying(200),
        "CustomerPhone" character varying(50),
        "CustomerEmail" character varying(200),
        "SiteAddress" character varying(500),
        "City" character varying(100),
        "State" character varying(100),
        "Postcode" character varying(20),
        "GpsCoordinates" character varying(100),
        "StartDate" timestamp with time zone,
        "EndDate" timestamp with time zone,
        "ActualStartDate" timestamp with time zone,
        "ActualEndDate" timestamp with time zone,
        "BudgetAmount" numeric(18,2),
        "ContractValue" numeric(18,2),
        "Currency" character varying(10) NOT NULL,
        "ProjectManagerId" uuid,
        "Notes" character varying(4000),
        "CreatedByUserId" uuid NOT NULL,
        "CompanyId" uuid,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "IsDeleted" boolean NOT NULL,
        "DeletedAt" timestamp with time zone,
        "DeletedByUserId" uuid,
        "RowVersion" bytea,
        CONSTRAINT "PK_projects" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251203001308_AddProcurementSalesProjectEntities') THEN
    CREATE TABLE quotations (
        "Id" uuid NOT NULL,
        "QuotationNumber" character varying(50) NOT NULL,
        "PartnerId" uuid,
        "DepartmentId" uuid,
        "ProjectId" uuid,
        "CustomerName" character varying(200) NOT NULL,
        "CustomerPhone" character varying(50),
        "CustomerEmail" character varying(200),
        "CustomerAddress" character varying(500),
        "QuotationDate" timestamp with time zone NOT NULL,
        "ValidUntil" timestamp with time zone,
        "Status" character varying(50) NOT NULL,
        "Subject" character varying(200),
        "Introduction" character varying(4000),
        "SubTotal" numeric(18,2) NOT NULL,
        "TaxAmount" numeric(18,2) NOT NULL,
        "DiscountAmount" numeric(18,2) NOT NULL,
        "TotalAmount" numeric(18,2) NOT NULL,
        "Currency" character varying(10) NOT NULL,
        "PaymentTerms" character varying(200),
        "DeliveryTerms" character varying(200),
        "TermsAndConditions" character varying(4000),
        "Notes" character varying(2000),
        "InternalNotes" character varying(2000),
        "CreatedByUserId" uuid NOT NULL,
        "ApprovedByUserId" uuid,
        "ApprovedAt" timestamp with time zone,
        "SentAt" timestamp with time zone,
        "AcceptedAt" timestamp with time zone,
        "RejectedAt" timestamp with time zone,
        "RejectionReason" character varying(1000),
        "ConvertedToOrderId" uuid,
        "ConvertedToPurchaseOrderId" uuid,
        "CompanyId" uuid,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "IsDeleted" boolean NOT NULL,
        "DeletedAt" timestamp with time zone,
        "DeletedByUserId" uuid,
        "RowVersion" bytea,
        CONSTRAINT "PK_quotations" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251203001308_AddProcurementSalesProjectEntities') THEN
    CREATE TABLE suppliers (
        "Id" uuid NOT NULL,
        "Code" character varying(50) NOT NULL,
        "Name" character varying(200) NOT NULL,
        "RegistrationNumber" character varying(100),
        "TaxNumber" character varying(100),
        "ContactPerson" character varying(100),
        "Email" character varying(200),
        "Phone" character varying(50),
        "Fax" character varying(50),
        "Address" character varying(500),
        "City" character varying(100),
        "State" character varying(100),
        "Postcode" character varying(20),
        "Country" character varying(100) NOT NULL,
        "BankName" character varying(200),
        "BankAccountNumber" character varying(50),
        "BankAccountName" character varying(200),
        "PaymentTerms" character varying(100),
        "CreditLimit" numeric(18,2),
        "Currency" character varying(10) NOT NULL,
        "IsActive" boolean NOT NULL,
        "Notes" text,
        "CreatedByUserId" uuid NOT NULL,
        "CompanyId" uuid,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "IsDeleted" boolean NOT NULL,
        "DeletedAt" timestamp with time zone,
        "DeletedByUserId" uuid,
        "RowVersion" bytea,
        CONSTRAINT "PK_suppliers" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251203001308_AddProcurementSalesProjectEntities') THEN
    CREATE TABLE delivery_order_items (
        "Id" uuid NOT NULL,
        "DeliveryOrderId" uuid NOT NULL,
        "MaterialId" uuid NOT NULL,
        "LineNumber" integer NOT NULL,
        "Description" character varying(500) NOT NULL,
        "Sku" character varying(100),
        "Unit" character varying(20) NOT NULL,
        "Quantity" numeric(18,4) NOT NULL,
        "QuantityDelivered" numeric(18,4) NOT NULL,
        "SerialNumbers" character varying(4000),
        "Notes" character varying(1000),
        "CompanyId" uuid,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "IsDeleted" boolean NOT NULL,
        "DeletedAt" timestamp with time zone,
        "DeletedByUserId" uuid,
        "RowVersion" bytea,
        CONSTRAINT "PK_delivery_order_items" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_delivery_order_items_Materials_MaterialId" FOREIGN KEY ("MaterialId") REFERENCES "Materials" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_delivery_order_items_delivery_orders_DeliveryOrderId" FOREIGN KEY ("DeliveryOrderId") REFERENCES delivery_orders ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251203001308_AddProcurementSalesProjectEntities') THEN
    CREATE TABLE boq_items (
        "Id" uuid NOT NULL,
        "ProjectId" uuid NOT NULL,
        "MaterialId" uuid,
        "LineNumber" integer NOT NULL,
        "Section" character varying(100),
        "ItemType" character varying(50) NOT NULL,
        "Description" character varying(500) NOT NULL,
        "Sku" character varying(100),
        "Unit" character varying(20) NOT NULL,
        "Quantity" numeric(18,4) NOT NULL,
        "UnitRate" numeric(18,4) NOT NULL,
        "Total" numeric(18,2) NOT NULL,
        "MarkupPercent" numeric(5,2) NOT NULL,
        "SellingPrice" numeric(18,2) NOT NULL,
        "Notes" character varying(1000),
        "IsOptional" boolean NOT NULL,
        "CompanyId" uuid,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "IsDeleted" boolean NOT NULL,
        "DeletedAt" timestamp with time zone,
        "DeletedByUserId" uuid,
        "RowVersion" bytea,
        CONSTRAINT "PK_boq_items" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_boq_items_projects_ProjectId" FOREIGN KEY ("ProjectId") REFERENCES projects ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251203001308_AddProcurementSalesProjectEntities') THEN
    CREATE TABLE quotation_items (
        "Id" uuid NOT NULL,
        "QuotationId" uuid NOT NULL,
        "MaterialId" uuid,
        "LineNumber" integer NOT NULL,
        "ItemType" character varying(50) NOT NULL,
        "Description" character varying(500) NOT NULL,
        "Sku" character varying(100),
        "Unit" character varying(20) NOT NULL,
        "Quantity" numeric(18,4) NOT NULL,
        "UnitPrice" numeric(18,4) NOT NULL,
        "DiscountPercent" numeric(5,2) NOT NULL,
        "TaxPercent" numeric(5,2) NOT NULL,
        "Total" numeric(18,2) NOT NULL,
        "Notes" character varying(1000),
        "CompanyId" uuid,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "IsDeleted" boolean NOT NULL,
        "DeletedAt" timestamp with time zone,
        "DeletedByUserId" uuid,
        "RowVersion" bytea,
        CONSTRAINT "PK_quotation_items" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_quotation_items_quotations_QuotationId" FOREIGN KEY ("QuotationId") REFERENCES quotations ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251203001308_AddProcurementSalesProjectEntities') THEN
    CREATE TABLE purchase_orders (
        "Id" uuid NOT NULL,
        "PoNumber" character varying(50) NOT NULL,
        "SupplierId" uuid NOT NULL,
        "DepartmentId" uuid,
        "CostCentreId" uuid,
        "PoDate" timestamp with time zone NOT NULL,
        "ExpectedDeliveryDate" timestamp with time zone,
        "DeliveryAddress" character varying(500),
        "Status" character varying(50) NOT NULL,
        "SubTotal" numeric(18,2) NOT NULL,
        "TaxAmount" numeric(18,2) NOT NULL,
        "DiscountAmount" numeric(18,2) NOT NULL,
        "TotalAmount" numeric(18,2) NOT NULL,
        "Currency" character varying(10) NOT NULL,
        "PaymentTerms" character varying(100),
        "TermsAndConditions" character varying(4000),
        "Notes" character varying(2000),
        "InternalNotes" character varying(2000),
        "CreatedByUserId" uuid NOT NULL,
        "ApprovedByUserId" uuid,
        "ApprovedAt" timestamp with time zone,
        "QuotationId" uuid,
        "ProjectId" uuid,
        "CompanyId" uuid,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "IsDeleted" boolean NOT NULL,
        "DeletedAt" timestamp with time zone,
        "DeletedByUserId" uuid,
        "RowVersion" bytea,
        CONSTRAINT "PK_purchase_orders" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_purchase_orders_suppliers_SupplierId" FOREIGN KEY ("SupplierId") REFERENCES suppliers ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251203001308_AddProcurementSalesProjectEntities') THEN
    CREATE TABLE purchase_order_items (
        "Id" uuid NOT NULL,
        "PurchaseOrderId" uuid NOT NULL,
        "MaterialId" uuid,
        "LineNumber" integer NOT NULL,
        "Description" character varying(500) NOT NULL,
        "Sku" character varying(100),
        "Unit" character varying(20) NOT NULL,
        "Quantity" numeric(18,4) NOT NULL,
        "UnitPrice" numeric(18,4) NOT NULL,
        "DiscountPercent" numeric(5,2) NOT NULL,
        "TaxPercent" numeric(5,2) NOT NULL,
        "Total" numeric(18,2) NOT NULL,
        "QuantityReceived" numeric(18,4) NOT NULL,
        "Notes" character varying(1000),
        "CompanyId" uuid,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "IsDeleted" boolean NOT NULL,
        "DeletedAt" timestamp with time zone,
        "DeletedByUserId" uuid,
        "RowVersion" bytea,
        CONSTRAINT "PK_purchase_order_items" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_purchase_order_items_purchase_orders_PurchaseOrderId" FOREIGN KEY ("PurchaseOrderId") REFERENCES purchase_orders ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251203001308_AddProcurementSalesProjectEntities') THEN
    CREATE INDEX "IX_boq_items_ProjectId_LineNumber" ON boq_items ("ProjectId", "LineNumber");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251203001308_AddProcurementSalesProjectEntities') THEN
    CREATE INDEX "IX_boq_items_ProjectId_Section" ON boq_items ("ProjectId", "Section");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251203001308_AddProcurementSalesProjectEntities') THEN
    CREATE INDEX "IX_delivery_order_items_DeliveryOrderId_LineNumber" ON delivery_order_items ("DeliveryOrderId", "LineNumber");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251203001308_AddProcurementSalesProjectEntities') THEN
    CREATE INDEX "IX_delivery_order_items_MaterialId" ON delivery_order_items ("MaterialId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251203001308_AddProcurementSalesProjectEntities') THEN
    CREATE INDEX "IX_delivery_orders_CompanyId_DoDate" ON delivery_orders ("CompanyId", "DoDate");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251203001308_AddProcurementSalesProjectEntities') THEN
    CREATE UNIQUE INDEX "IX_delivery_orders_CompanyId_DoNumber" ON delivery_orders ("CompanyId", "DoNumber");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251203001308_AddProcurementSalesProjectEntities') THEN
    CREATE INDEX "IX_delivery_orders_CompanyId_DoType" ON delivery_orders ("CompanyId", "DoType");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251203001308_AddProcurementSalesProjectEntities') THEN
    CREATE INDEX "IX_delivery_orders_CompanyId_Status" ON delivery_orders ("CompanyId", "Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251203001308_AddProcurementSalesProjectEntities') THEN
    CREATE INDEX "IX_projects_CompanyId_PartnerId" ON projects ("CompanyId", "PartnerId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251203001308_AddProcurementSalesProjectEntities') THEN
    CREATE UNIQUE INDEX "IX_projects_CompanyId_ProjectCode" ON projects ("CompanyId", "ProjectCode");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251203001308_AddProcurementSalesProjectEntities') THEN
    CREATE INDEX "IX_projects_CompanyId_ProjectType" ON projects ("CompanyId", "ProjectType");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251203001308_AddProcurementSalesProjectEntities') THEN
    CREATE INDEX "IX_projects_CompanyId_Status" ON projects ("CompanyId", "Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251203001308_AddProcurementSalesProjectEntities') THEN
    CREATE INDEX "IX_purchase_order_items_PurchaseOrderId_LineNumber" ON purchase_order_items ("PurchaseOrderId", "LineNumber");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251203001308_AddProcurementSalesProjectEntities') THEN
    CREATE INDEX "IX_purchase_orders_CompanyId_PoDate" ON purchase_orders ("CompanyId", "PoDate");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251203001308_AddProcurementSalesProjectEntities') THEN
    CREATE UNIQUE INDEX "IX_purchase_orders_CompanyId_PoNumber" ON purchase_orders ("CompanyId", "PoNumber");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251203001308_AddProcurementSalesProjectEntities') THEN
    CREATE INDEX "IX_purchase_orders_CompanyId_Status" ON purchase_orders ("CompanyId", "Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251203001308_AddProcurementSalesProjectEntities') THEN
    CREATE INDEX "IX_purchase_orders_CompanyId_SupplierId" ON purchase_orders ("CompanyId", "SupplierId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251203001308_AddProcurementSalesProjectEntities') THEN
    CREATE INDEX "IX_purchase_orders_SupplierId" ON purchase_orders ("SupplierId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251203001308_AddProcurementSalesProjectEntities') THEN
    CREATE INDEX "IX_quotation_items_QuotationId_LineNumber" ON quotation_items ("QuotationId", "LineNumber");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251203001308_AddProcurementSalesProjectEntities') THEN
    CREATE INDEX "IX_quotations_CompanyId_PartnerId" ON quotations ("CompanyId", "PartnerId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251203001308_AddProcurementSalesProjectEntities') THEN
    CREATE INDEX "IX_quotations_CompanyId_QuotationDate" ON quotations ("CompanyId", "QuotationDate");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251203001308_AddProcurementSalesProjectEntities') THEN
    CREATE UNIQUE INDEX "IX_quotations_CompanyId_QuotationNumber" ON quotations ("CompanyId", "QuotationNumber");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251203001308_AddProcurementSalesProjectEntities') THEN
    CREATE INDEX "IX_quotations_CompanyId_Status" ON quotations ("CompanyId", "Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251203001308_AddProcurementSalesProjectEntities') THEN
    CREATE UNIQUE INDEX "IX_suppliers_CompanyId_Code" ON suppliers ("CompanyId", "Code");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251203001308_AddProcurementSalesProjectEntities') THEN
    CREATE INDEX "IX_suppliers_CompanyId_Name" ON suppliers ("CompanyId", "Name");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251203001308_AddProcurementSalesProjectEntities') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251203001308_AddProcurementSalesProjectEntities', '10.0.3');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251203004645_AddTemplateFileIdToDocumentTemplate') THEN
    ALTER TABLE "DocumentTemplates" ADD "TemplateFileId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251203004645_AddTemplateFileIdToDocumentTemplate') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251203004645_AddTemplateFileIdToDocumentTemplate', '10.0.3');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251203082425_AddBuildingNameAndStatusToOrderDraft') THEN
    ALTER TABLE "ParseSessions" ALTER COLUMN "RowVersion" SET DEFAULT (gen_random_bytes(8));
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251203082425_AddBuildingNameAndStatusToOrderDraft') THEN
    ALTER TABLE "ParsedOrderDrafts" ALTER COLUMN "RowVersion" SET DEFAULT (gen_random_bytes(8));
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251203082425_AddBuildingNameAndStatusToOrderDraft') THEN
    ALTER TABLE "ParsedOrderDrafts" ADD "BuildingName" character varying(500);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251203082425_AddBuildingNameAndStatusToOrderDraft') THEN
    ALTER TABLE "ParsedOrderDrafts" ADD "BuildingStatus" character varying(50);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251203082425_AddBuildingNameAndStatusToOrderDraft') THEN
    ALTER TABLE "EmailMessages" ALTER COLUMN "RowVersion" SET DEFAULT (gen_random_bytes(8));
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251203082425_AddBuildingNameAndStatusToOrderDraft') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251203082425_AddBuildingNameAndStatusToOrderDraft', '10.0.3');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251204111940_AddSmsAndWhatsAppTemplates') THEN
    CREATE TABLE sms_templates (
        "Id" uuid NOT NULL,
        code character varying(50) NOT NULL,
        name character varying(200) NOT NULL,
        description character varying(500),
        category character varying(100) NOT NULL,
        message_text character varying(1000) NOT NULL,
        char_count integer NOT NULL,
        is_active boolean NOT NULL,
        created_by_user_id uuid,
        updated_by_user_id uuid,
        notes character varying(1000),
        company_id uuid,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        is_deleted boolean NOT NULL,
        deleted_at timestamp with time zone,
        "DeletedByUserId" uuid,
        "RowVersion" bytea,
        CONSTRAINT "PK_sms_templates" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251204111940_AddSmsAndWhatsAppTemplates') THEN
    CREATE TABLE whatsapp_templates (
        "Id" uuid NOT NULL,
        code character varying(50) NOT NULL,
        name character varying(200) NOT NULL,
        description character varying(500),
        category character varying(100) NOT NULL,
        template_id character varying(200),
        approval_status character varying(50) NOT NULL,
        message_body character varying(2000),
        language character varying(10),
        is_active boolean NOT NULL,
        created_by_user_id uuid,
        updated_by_user_id uuid,
        notes character varying(1000),
        submitted_at timestamp with time zone,
        approved_at timestamp with time zone,
        company_id uuid,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        is_deleted boolean NOT NULL,
        deleted_at timestamp with time zone,
        "DeletedByUserId" uuid,
        "RowVersion" bytea,
        CONSTRAINT "PK_whatsapp_templates" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251204111940_AddSmsAndWhatsAppTemplates') THEN
    CREATE INDEX "IX_sms_templates_company_id_category_is_active" ON sms_templates (company_id, category, is_active);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251204111940_AddSmsAndWhatsAppTemplates') THEN
    CREATE UNIQUE INDEX "IX_sms_templates_company_id_code" ON sms_templates (company_id, code) WHERE is_deleted = false;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251204111940_AddSmsAndWhatsAppTemplates') THEN
    CREATE INDEX "IX_sms_templates_company_id_is_active" ON sms_templates (company_id, is_active);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251204111940_AddSmsAndWhatsAppTemplates') THEN
    CREATE INDEX "IX_whatsapp_templates_company_id_approval_status_is_active" ON whatsapp_templates (company_id, approval_status, is_active);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251204111940_AddSmsAndWhatsAppTemplates') THEN
    CREATE INDEX "IX_whatsapp_templates_company_id_category_approval_status_is_a~" ON whatsapp_templates (company_id, category, approval_status, is_active);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251204111940_AddSmsAndWhatsAppTemplates') THEN
    CREATE UNIQUE INDEX "IX_whatsapp_templates_company_id_code" ON whatsapp_templates (company_id, code) WHERE is_deleted = false;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251204111940_AddSmsAndWhatsAppTemplates') THEN
    CREATE INDEX "IX_whatsapp_templates_template_id" ON whatsapp_templates (template_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251204111940_AddSmsAndWhatsAppTemplates') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251204111940_AddSmsAndWhatsAppTemplates', '10.0.3');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251204122813_AddAllSettingsTables') THEN
    CREATE TABLE "Bins" (
        "Id" uuid NOT NULL,
        "CompanyId" uuid NOT NULL,
        "Code" text NOT NULL,
        "Name" text NOT NULL,
        "WarehouseId" uuid NOT NULL,
        "WarehouseName" text,
        "Section" text NOT NULL,
        "Row" integer NOT NULL,
        "Level" integer NOT NULL,
        "Capacity" numeric NOT NULL,
        "CurrentStock" numeric NOT NULL,
        "IsActive" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        "IsDeleted" boolean NOT NULL,
        "DeletedAt" timestamp with time zone,
        "RowVersion" bytea,
        CONSTRAINT "PK_Bins" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251204122813_AddAllSettingsTables') THEN
    CREATE TABLE "Brands" (
        "Id" uuid NOT NULL,
        "CompanyId" uuid NOT NULL,
        "Code" text NOT NULL,
        "Name" text NOT NULL,
        "Description" text,
        "Country" text,
        "Website" text,
        "LogoUrl" text,
        "MaterialCount" integer NOT NULL,
        "IsActive" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        "CreatedBy" text,
        "UpdatedBy" text,
        "IsDeleted" boolean NOT NULL,
        "DeletedAt" timestamp with time zone,
        "RowVersion" bytea,
        CONSTRAINT "PK_Brands" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251204122813_AddAllSettingsTables') THEN
    CREATE TABLE payment_terms (
        "Id" uuid NOT NULL,
        company_id uuid NOT NULL,
        code character varying(50) NOT NULL,
        name character varying(200) NOT NULL,
        description character varying(500),
        due_days integer NOT NULL,
        discount_percent numeric(5,2) NOT NULL,
        discount_days integer NOT NULL,
        is_active boolean NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone,
        is_deleted boolean NOT NULL,
        "DeletedAt" timestamp with time zone,
        "RowVersion" bytea,
        CONSTRAINT "PK_payment_terms" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251204122813_AddAllSettingsTables') THEN
    CREATE TABLE "ProductTypes" (
        "Id" uuid NOT NULL,
        "CompanyId" uuid NOT NULL,
        "Code" text NOT NULL,
        "Name" text NOT NULL,
        "Description" text,
        "Category" text,
        "RequiresInstallation" boolean NOT NULL,
        "PlanCount" integer NOT NULL,
        "IsActive" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        "IsDeleted" boolean NOT NULL,
        "DeletedAt" timestamp with time zone,
        "RowVersion" bytea,
        CONSTRAINT "PK_ProductTypes" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251204122813_AddAllSettingsTables') THEN
    CREATE TABLE "ServicePlans" (
        "Id" uuid NOT NULL,
        "CompanyId" uuid NOT NULL,
        "Code" text NOT NULL,
        "Name" text NOT NULL,
        "Description" text,
        "ProductTypeId" uuid,
        "ProductTypeName" text,
        "SpeedMbps" integer NOT NULL,
        "MonthlyPrice" numeric NOT NULL,
        "SetupFee" numeric NOT NULL,
        "ContractMonths" integer NOT NULL,
        "IsActive" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        "IsDeleted" boolean NOT NULL,
        "DeletedAt" timestamp with time zone,
        "RowVersion" bytea,
        CONSTRAINT "PK_ServicePlans" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251204122813_AddAllSettingsTables') THEN
    CREATE TABLE tax_codes (
        "Id" uuid NOT NULL,
        company_id uuid NOT NULL,
        code character varying(50) NOT NULL,
        name character varying(200) NOT NULL,
        description character varying(500),
        tax_rate numeric(5,2) NOT NULL,
        is_default boolean NOT NULL,
        is_active boolean NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone,
        is_deleted boolean NOT NULL,
        "DeletedAt" timestamp with time zone,
        "RowVersion" bytea,
        CONSTRAINT "PK_tax_codes" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251204122813_AddAllSettingsTables') THEN
    CREATE TABLE "Teams" (
        "Id" uuid NOT NULL,
        "CompanyId" uuid NOT NULL,
        "Code" text NOT NULL,
        "Name" text NOT NULL,
        "Description" text,
        "DepartmentId" uuid,
        "DepartmentName" text,
        "TeamLeaderId" uuid,
        "TeamLeaderName" text,
        "MemberCount" integer NOT NULL,
        "ActiveJobsCount" integer NOT NULL,
        "IsActive" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        "CreatedBy" text,
        "UpdatedBy" text,
        "IsDeleted" boolean NOT NULL,
        "DeletedAt" timestamp with time zone,
        "RowVersion" bytea,
        CONSTRAINT "PK_Teams" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251204122813_AddAllSettingsTables') THEN
    CREATE TABLE vendors (
        "Id" uuid NOT NULL,
        company_id uuid NOT NULL,
        code character varying(50) NOT NULL,
        name character varying(200) NOT NULL,
        description character varying(500),
        contact_person character varying(200),
        contact_phone character varying(50),
        contact_email character varying(200),
        address character varying(500),
        city character varying(100),
        state character varying(100),
        post_code character varying(20),
        country character varying(100),
        payment_terms character varying(100),
        payment_due_days integer,
        is_active boolean NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone,
        created_by character varying(200),
        updated_by character varying(200),
        is_deleted boolean NOT NULL,
        deleted_at timestamp with time zone,
        "RowVersion" bytea,
        CONSTRAINT "PK_vendors" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251204122813_AddAllSettingsTables') THEN
    CREATE UNIQUE INDEX "IX_payment_terms_company_id_code" ON payment_terms (company_id, code) WHERE is_deleted = false;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251204122813_AddAllSettingsTables') THEN
    CREATE INDEX "IX_payment_terms_company_id_is_active" ON payment_terms (company_id, is_active);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251204122813_AddAllSettingsTables') THEN
    CREATE UNIQUE INDEX "IX_tax_codes_company_id_code" ON tax_codes (company_id, code) WHERE is_deleted = false;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251204122813_AddAllSettingsTables') THEN
    CREATE INDEX "IX_tax_codes_company_id_is_active_is_default" ON tax_codes (company_id, is_active, is_default);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251204122813_AddAllSettingsTables') THEN
    CREATE UNIQUE INDEX "IX_vendors_company_id_code" ON vendors (company_id, code) WHERE is_deleted = false;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251204122813_AddAllSettingsTables') THEN
    CREATE INDEX "IX_vendors_company_id_is_active" ON vendors (company_id, is_active);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251204122813_AddAllSettingsTables') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251204122813_AddAllSettingsTables', '10.0.3');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251204124148_FixBuildingRelationshipsQueryFilter') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251204124148_FixBuildingRelationshipsQueryFilter', '10.0.3');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251205020231_IncreaseParsedOrderDraftFieldLengths') THEN
    ALTER TABLE "ParsedOrderDrafts" ALTER COLUMN "VoipServiceId" TYPE character varying(200);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251205020231_IncreaseParsedOrderDraftFieldLengths') THEN
    ALTER TABLE "ParsedOrderDrafts" ALTER COLUMN "ValidationNotes" TYPE character varying(4000);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251205020231_IncreaseParsedOrderDraftFieldLengths') THEN
    ALTER TABLE "ParsedOrderDrafts" ALTER COLUMN "TicketId" TYPE character varying(500);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251205020231_IncreaseParsedOrderDraftFieldLengths') THEN
    ALTER TABLE "ParsedOrderDrafts" ALTER COLUMN "SourceFileName" TYPE character varying(1000);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251205020231_IncreaseParsedOrderDraftFieldLengths') THEN
    ALTER TABLE "ParsedOrderDrafts" ALTER COLUMN "ServiceId" TYPE character varying(500);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251205020231_IncreaseParsedOrderDraftFieldLengths') THEN
    ALTER TABLE "ParsedOrderDrafts" ALTER COLUMN "Remarks" TYPE character varying(4000);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251205020231_IncreaseParsedOrderDraftFieldLengths') THEN
    ALTER TABLE "ParsedOrderDrafts" ALTER COLUMN "PackageName" TYPE character varying(500);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251205020231_IncreaseParsedOrderDraftFieldLengths') THEN
    ALTER TABLE "ParsedOrderDrafts" ALTER COLUMN "OrderTypeHint" TYPE character varying(200);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251205020231_IncreaseParsedOrderDraftFieldLengths') THEN
    ALTER TABLE "ParsedOrderDrafts" ALTER COLUMN "OrderTypeCode" TYPE character varying(100);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251205020231_IncreaseParsedOrderDraftFieldLengths') THEN
    ALTER TABLE "ParsedOrderDrafts" ALTER COLUMN "OnuSerialNumber" TYPE character varying(200);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251205020231_IncreaseParsedOrderDraftFieldLengths') THEN
    ALTER TABLE "ParsedOrderDrafts" ALTER COLUMN "OldAddress" TYPE character varying(2000);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251205020231_IncreaseParsedOrderDraftFieldLengths') THEN
    ALTER TABLE "ParsedOrderDrafts" ALTER COLUMN "CustomerPhone" TYPE character varying(100);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251205020231_IncreaseParsedOrderDraftFieldLengths') THEN
    ALTER TABLE "ParsedOrderDrafts" ALTER COLUMN "CustomerName" TYPE character varying(500);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251205020231_IncreaseParsedOrderDraftFieldLengths') THEN
    ALTER TABLE "ParsedOrderDrafts" ALTER COLUMN "CustomerEmail" TYPE character varying(500);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251205020231_IncreaseParsedOrderDraftFieldLengths') THEN
    ALTER TABLE "ParsedOrderDrafts" ALTER COLUMN "Bandwidth" TYPE character varying(100);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251205020231_IncreaseParsedOrderDraftFieldLengths') THEN
    ALTER TABLE "ParsedOrderDrafts" ALTER COLUMN "AppointmentWindow" TYPE character varying(100);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251205020231_IncreaseParsedOrderDraftFieldLengths') THEN
    ALTER TABLE "ParsedOrderDrafts" ALTER COLUMN "AddressText" TYPE character varying(2000);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251205020231_IncreaseParsedOrderDraftFieldLengths') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251205020231_IncreaseParsedOrderDraftFieldLengths', '10.0.3');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251205023922_AddCompanyLocaleSettings') THEN
    ALTER TABLE "Companies" ADD "DefaultCurrency" text NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251205023922_AddCompanyLocaleSettings') THEN
    ALTER TABLE "Companies" ADD "DefaultDateFormat" text NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251205023922_AddCompanyLocaleSettings') THEN
    ALTER TABLE "Companies" ADD "DefaultLocale" text NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251205023922_AddCompanyLocaleSettings') THEN
    ALTER TABLE "Companies" ADD "DefaultTimeFormat" text NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251205023922_AddCompanyLocaleSettings') THEN
    ALTER TABLE "Companies" ADD "DefaultTimezone" text NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251205023922_AddCompanyLocaleSettings') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251205023922_AddCompanyLocaleSettings', '10.0.3');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251206140750_AddNetworkInfoAndVoipFields') THEN
    ALTER TABLE "Orders" ADD "AwoNumber" character varying(200);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251206140750_AddNetworkInfoAndVoipFields') THEN
    ALTER TABLE "Orders" ADD "CustomerPhone2" character varying(50);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251206140750_AddNetworkInfoAndVoipFields') THEN
    ALTER TABLE "Orders" ADD "NetworkBandwidth" character varying(100);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251206140750_AddNetworkInfoAndVoipFields') THEN
    ALTER TABLE "Orders" ADD "NetworkGateway" character varying(50);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251206140750_AddNetworkInfoAndVoipFields') THEN
    ALTER TABLE "Orders" ADD "NetworkLanIp" character varying(50);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251206140750_AddNetworkInfoAndVoipFields') THEN
    ALTER TABLE "Orders" ADD "NetworkLoginId" character varying(200);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251206140750_AddNetworkInfoAndVoipFields') THEN
    ALTER TABLE "Orders" ADD "NetworkPackage" character varying(1000);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251206140750_AddNetworkInfoAndVoipFields') THEN
    ALTER TABLE "Orders" ADD "NetworkPassword" character varying(200);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251206140750_AddNetworkInfoAndVoipFields') THEN
    ALTER TABLE "Orders" ADD "NetworkSubnetMask" character varying(50);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251206140750_AddNetworkInfoAndVoipFields') THEN
    ALTER TABLE "Orders" ADD "NetworkWanIp" character varying(50);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251206140750_AddNetworkInfoAndVoipFields') THEN
    ALTER TABLE "Orders" ADD "ServiceIdType" integer;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251206140750_AddNetworkInfoAndVoipFields') THEN
    ALTER TABLE "Orders" ADD "SplitterId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251206140750_AddNetworkInfoAndVoipFields') THEN
    ALTER TABLE "Orders" ADD "SplitterLocation" character varying(200);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251206140750_AddNetworkInfoAndVoipFields') THEN
    ALTER TABLE "Orders" ADD "SplitterNumber" character varying(100);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251206140750_AddNetworkInfoAndVoipFields') THEN
    ALTER TABLE "Orders" ADD "SplitterPort" character varying(50);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251206140750_AddNetworkInfoAndVoipFields') THEN
    ALTER TABLE "Orders" ADD "VoipGatewayOnu" character varying(50);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251206140750_AddNetworkInfoAndVoipFields') THEN
    ALTER TABLE "Orders" ADD "VoipIpAddressOnu" character varying(50);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251206140750_AddNetworkInfoAndVoipFields') THEN
    ALTER TABLE "Orders" ADD "VoipIpAddressSrp" character varying(50);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251206140750_AddNetworkInfoAndVoipFields') THEN
    ALTER TABLE "Orders" ADD "VoipPassword" character varying(200);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251206140750_AddNetworkInfoAndVoipFields') THEN
    ALTER TABLE "Orders" ADD "VoipRemarks" character varying(1000);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251206140750_AddNetworkInfoAndVoipFields') THEN
    ALTER TABLE "Orders" ADD "VoipSubnetMaskOnu" character varying(50);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251206140750_AddNetworkInfoAndVoipFields') THEN
    CREATE TABLE "OrderMaterialReplacements" (
        "Id" uuid NOT NULL,
        "OrderId" uuid NOT NULL,
        "OldMaterialId" uuid NOT NULL,
        "OldSerialNumber" character varying(100) NOT NULL,
        "OldSerialisedItemId" uuid,
        "NewMaterialId" uuid NOT NULL,
        "NewSerialNumber" character varying(100) NOT NULL,
        "NewSerialisedItemId" uuid,
        "ApprovedBy" character varying(200),
        "ApprovalNotes" character varying(1000),
        "ApprovedAt" timestamp with time zone,
        "ReplacementReason" character varying(500),
        "ReplacedBySiId" uuid,
        "RecordedByUserId" uuid,
        "RecordedAt" timestamp with time zone NOT NULL,
        "RmaRequestId" uuid,
        "Notes" character varying(1000),
        "CompanyId" uuid,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "IsDeleted" boolean NOT NULL,
        "DeletedAt" timestamp with time zone,
        "DeletedByUserId" uuid,
        "RowVersion" bytea,
        CONSTRAINT "PK_OrderMaterialReplacements" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_OrderMaterialReplacements_Orders_OrderId" FOREIGN KEY ("OrderId") REFERENCES "Orders" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251206140750_AddNetworkInfoAndVoipFields') THEN
    CREATE TABLE "OrderNonSerialisedReplacements" (
        "Id" uuid NOT NULL,
        "OrderId" uuid NOT NULL,
        "MaterialId" uuid NOT NULL,
        "QuantityReplaced" numeric(18,4) NOT NULL,
        "Unit" character varying(20),
        "ReplacementReason" character varying(500),
        "Remark" character varying(1000),
        "ReplacedBySiId" uuid,
        "RecordedByUserId" uuid,
        "RecordedAt" timestamp with time zone NOT NULL,
        "CompanyId" uuid,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "IsDeleted" boolean NOT NULL,
        "DeletedAt" timestamp with time zone,
        "DeletedByUserId" uuid,
        "RowVersion" bytea,
        CONSTRAINT "PK_OrderNonSerialisedReplacements" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_OrderNonSerialisedReplacements_Orders_OrderId" FOREIGN KEY ("OrderId") REFERENCES "Orders" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251206140750_AddNetworkInfoAndVoipFields') THEN
    CREATE INDEX "IX_OrderMaterialReplacements_CompanyId_OrderId" ON "OrderMaterialReplacements" ("CompanyId", "OrderId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251206140750_AddNetworkInfoAndVoipFields') THEN
    CREATE INDEX "IX_OrderMaterialReplacements_NewMaterialId" ON "OrderMaterialReplacements" ("NewMaterialId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251206140750_AddNetworkInfoAndVoipFields') THEN
    CREATE INDEX "IX_OrderMaterialReplacements_NewSerialisedItemId" ON "OrderMaterialReplacements" ("NewSerialisedItemId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251206140750_AddNetworkInfoAndVoipFields') THEN
    CREATE INDEX "IX_OrderMaterialReplacements_OldMaterialId" ON "OrderMaterialReplacements" ("OldMaterialId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251206140750_AddNetworkInfoAndVoipFields') THEN
    CREATE INDEX "IX_OrderMaterialReplacements_OldSerialisedItemId" ON "OrderMaterialReplacements" ("OldSerialisedItemId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251206140750_AddNetworkInfoAndVoipFields') THEN
    CREATE INDEX "IX_OrderMaterialReplacements_OrderId" ON "OrderMaterialReplacements" ("OrderId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251206140750_AddNetworkInfoAndVoipFields') THEN
    CREATE INDEX "IX_OrderMaterialReplacements_RmaRequestId" ON "OrderMaterialReplacements" ("RmaRequestId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251206140750_AddNetworkInfoAndVoipFields') THEN
    CREATE INDEX "IX_OrderNonSerialisedReplacements_CompanyId_OrderId" ON "OrderNonSerialisedReplacements" ("CompanyId", "OrderId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251206140750_AddNetworkInfoAndVoipFields') THEN
    CREATE INDEX "IX_OrderNonSerialisedReplacements_MaterialId" ON "OrderNonSerialisedReplacements" ("MaterialId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251206140750_AddNetworkInfoAndVoipFields') THEN
    CREATE INDEX "IX_OrderNonSerialisedReplacements_OrderId" ON "OrderNonSerialisedReplacements" ("OrderId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251206140750_AddNetworkInfoAndVoipFields') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251206140750_AddNetworkInfoAndVoipFields', '10.0.3');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251206141109_AddServiceIdTypeAndNetworkInfoFields') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251206141109_AddServiceIdTypeAndNetworkInfoFields', '10.0.3');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251207095918_AddMaterialPartnersTable') THEN

                    DO $$
                    BEGIN
                        IF NOT EXISTS (SELECT FROM pg_tables WHERE schemaname = 'public' AND tablename = 'MaterialPartners') THEN
                            CREATE TABLE "MaterialPartners" (
                                "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                                "MaterialId" UUID NOT NULL,
                                "PartnerId" UUID NOT NULL,
                                "CompanyId" UUID NOT NULL,
                                "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
                                "UpdatedAt" TIMESTAMP WITH TIME ZONE,
                                "IsDeleted" BOOLEAN NOT NULL DEFAULT FALSE,
                                "DeletedAt" TIMESTAMP WITH TIME ZONE,
                                "DeletedByUserId" UUID,
                                "RowVersion" BYTEA,
                                CONSTRAINT "FK_MaterialPartners_Materials_MaterialId" FOREIGN KEY ("MaterialId")
                                    REFERENCES "Materials"("Id") ON DELETE CASCADE,
                                CONSTRAINT "FK_MaterialPartners_Partners_PartnerId" FOREIGN KEY ("PartnerId")
                                    REFERENCES "Partners"("Id") ON DELETE RESTRICT,
                                CONSTRAINT "UQ_MaterialPartners_CompanyId_MaterialId_PartnerId" 
                                    UNIQUE ("CompanyId", "MaterialId", "PartnerId")
                            );

                            CREATE INDEX IF NOT EXISTS "IX_MaterialPartners_CompanyId" ON "MaterialPartners" ("CompanyId");
                            CREATE INDEX IF NOT EXISTS "IX_MaterialPartners_MaterialId" ON "MaterialPartners" ("MaterialId");
                            CREATE INDEX IF NOT EXISTS "IX_MaterialPartners_PartnerId" ON "MaterialPartners" ("PartnerId");
                            CREATE UNIQUE INDEX IF NOT EXISTS "IX_MaterialPartners_CompanyId_MaterialId_PartnerId" 
                                ON "MaterialPartners" ("CompanyId", "MaterialId", "PartnerId");
                        END IF;
                    END $$;
                
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251207095918_AddMaterialPartnersTable') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251207095918_AddMaterialPartnersTable', '10.0.3');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251207114421_AddGuardConditionAndSideEffectDefinitions') THEN
    CREATE TABLE guard_condition_definitions (
        "Id" uuid NOT NULL,
        company_id uuid NOT NULL,
        key character varying(100) NOT NULL,
        name character varying(200) NOT NULL,
        description character varying(1000),
        entity_type character varying(100) NOT NULL,
        validator_type character varying(200) NOT NULL,
        validator_config_json jsonb,
        is_active boolean NOT NULL,
        display_order integer NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone,
        is_deleted boolean NOT NULL,
        deleted_at timestamp with time zone,
        row_version bytea,
        CONSTRAINT "PK_guard_condition_definitions" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251207114421_AddGuardConditionAndSideEffectDefinitions') THEN
    CREATE TABLE side_effect_definitions (
        "Id" uuid NOT NULL,
        company_id uuid NOT NULL,
        key character varying(100) NOT NULL,
        name character varying(200) NOT NULL,
        description character varying(1000),
        entity_type character varying(100) NOT NULL,
        executor_type character varying(200) NOT NULL,
        executor_config_json jsonb,
        is_active boolean NOT NULL,
        display_order integer NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone,
        is_deleted boolean NOT NULL,
        deleted_at timestamp with time zone,
        row_version bytea,
        CONSTRAINT "PK_side_effect_definitions" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251207114421_AddGuardConditionAndSideEffectDefinitions') THEN
    CREATE INDEX "IX_guard_condition_definitions_company_id_entity_type_is_active" ON guard_condition_definitions (company_id, entity_type, is_active);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251207114421_AddGuardConditionAndSideEffectDefinitions') THEN
    CREATE UNIQUE INDEX "IX_guard_condition_definitions_company_id_entity_type_key" ON guard_condition_definitions (company_id, entity_type, key) WHERE is_deleted = false;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251207114421_AddGuardConditionAndSideEffectDefinitions') THEN
    CREATE INDEX "IX_side_effect_definitions_company_id_entity_type_is_active" ON side_effect_definitions (company_id, entity_type, is_active);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251207114421_AddGuardConditionAndSideEffectDefinitions') THEN
    CREATE UNIQUE INDEX "IX_side_effect_definitions_company_id_entity_type_key" ON side_effect_definitions (company_id, entity_type, key) WHERE is_deleted = false;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251207114421_AddGuardConditionAndSideEffectDefinitions') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251207114421_AddGuardConditionAndSideEffectDefinitions', '10.0.3');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251207154424_AddOrderStatusChecklist') THEN
    CREATE TABLE "OrderStatusChecklistItems" (
        "Id" uuid NOT NULL,
        "StatusCode" character varying(50) NOT NULL,
        "ParentChecklistItemId" uuid,
        "Name" character varying(200) NOT NULL,
        "Description" character varying(1000),
        "OrderIndex" integer NOT NULL,
        "IsRequired" boolean NOT NULL,
        "IsActive" boolean NOT NULL,
        "CreatedByUserId" uuid,
        "UpdatedByUserId" uuid,
        "CompanyId" uuid,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "IsDeleted" boolean NOT NULL,
        "DeletedAt" timestamp with time zone,
        "DeletedByUserId" uuid,
        "RowVersion" bytea,
        CONSTRAINT "PK_OrderStatusChecklistItems" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_OrderStatusChecklistItems_OrderStatusChecklistItems_ParentC~" FOREIGN KEY ("ParentChecklistItemId") REFERENCES "OrderStatusChecklistItems" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251207154424_AddOrderStatusChecklist') THEN
    CREATE TABLE "OrderStatusChecklistAnswers" (
        "Id" uuid NOT NULL,
        "OrderId" uuid NOT NULL,
        "ChecklistItemId" uuid NOT NULL,
        "Answer" boolean NOT NULL,
        "AnsweredAt" timestamp with time zone NOT NULL,
        "AnsweredByUserId" uuid NOT NULL,
        "Remarks" character varying(1000),
        "CompanyId" uuid,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "IsDeleted" boolean NOT NULL,
        "DeletedAt" timestamp with time zone,
        "DeletedByUserId" uuid,
        "RowVersion" bytea,
        CONSTRAINT "PK_OrderStatusChecklistAnswers" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_OrderStatusChecklistAnswers_OrderStatusChecklistItems_Check~" FOREIGN KEY ("ChecklistItemId") REFERENCES "OrderStatusChecklistItems" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_OrderStatusChecklistAnswers_Orders_OrderId" FOREIGN KEY ("OrderId") REFERENCES "Orders" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251207154424_AddOrderStatusChecklist') THEN
    CREATE INDEX "IX_OrderStatusChecklistAnswers_ChecklistItemId" ON "OrderStatusChecklistAnswers" ("ChecklistItemId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251207154424_AddOrderStatusChecklist') THEN
    CREATE INDEX "IX_OrderStatusChecklistAnswers_CompanyId_OrderId" ON "OrderStatusChecklistAnswers" ("CompanyId", "OrderId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251207154424_AddOrderStatusChecklist') THEN
    CREATE UNIQUE INDEX "IX_OrderStatusChecklistAnswers_OrderId_ChecklistItemId" ON "OrderStatusChecklistAnswers" ("OrderId", "ChecklistItemId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251207154424_AddOrderStatusChecklist') THEN
    CREATE INDEX "IX_OrderStatusChecklistItems_CompanyId_StatusCode_IsActive" ON "OrderStatusChecklistItems" ("CompanyId", "StatusCode", "IsActive");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251207154424_AddOrderStatusChecklist') THEN
    CREATE INDEX "IX_OrderStatusChecklistItems_ParentChecklistItemId" ON "OrderStatusChecklistItems" ("ParentChecklistItemId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251207154424_AddOrderStatusChecklist') THEN
    CREATE INDEX "IX_OrderStatusChecklistItems_StatusCode_OrderIndex" ON "OrderStatusChecklistItems" ("StatusCode", "OrderIndex");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251207154424_AddOrderStatusChecklist') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251207154424_AddOrderStatusChecklist', '10.0.3');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251207221618_AddMaterialUsageAndRMAFeatures') THEN
    CREATE INDEX "IX_OrderMaterialUsage_MaterialId" ON "OrderMaterialUsage" ("MaterialId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251207221618_AddMaterialUsageAndRMAFeatures') THEN
    ALTER TABLE "OrderMaterialUsage" ADD CONSTRAINT "FK_OrderMaterialUsage_Materials_MaterialId" FOREIGN KEY ("MaterialId") REFERENCES "Materials" ("Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251207221618_AddMaterialUsageAndRMAFeatures') THEN
    ALTER TABLE "OrderMaterialUsage" ADD CONSTRAINT "FK_OrderMaterialUsage_SerialisedItems_SerialisedItemId" FOREIGN KEY ("SerialisedItemId") REFERENCES "SerialisedItems" ("Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251207221618_AddMaterialUsageAndRMAFeatures') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251207221618_AddMaterialUsageAndRMAFeatures', '10.0.3');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251208072755_AddScheduledSlotWorkflowFields') THEN

                    DO $$
                    BEGIN
                        IF NOT EXISTS (
                            SELECT 1 FROM information_schema.columns 
                            WHERE table_name = 'ScheduledSlots' AND column_name = 'ConfirmedByUserId'
                        ) THEN
                            ALTER TABLE "ScheduledSlots" ADD COLUMN "ConfirmedByUserId" uuid NULL;
                        END IF;
                    END $$;
                
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251208072755_AddScheduledSlotWorkflowFields') THEN

                    DO $$
                    BEGIN
                        IF NOT EXISTS (
                            SELECT 1 FROM information_schema.columns 
                            WHERE table_name = 'ScheduledSlots' AND column_name = 'ConfirmedAt'
                        ) THEN
                            ALTER TABLE "ScheduledSlots" ADD COLUMN "ConfirmedAt" timestamp with time zone NULL;
                        END IF;
                    END $$;
                
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251208072755_AddScheduledSlotWorkflowFields') THEN

                    DO $$
                    BEGIN
                        IF NOT EXISTS (
                            SELECT 1 FROM information_schema.columns 
                            WHERE table_name = 'ScheduledSlots' AND column_name = 'PostedByUserId'
                        ) THEN
                            ALTER TABLE "ScheduledSlots" ADD COLUMN "PostedByUserId" uuid NULL;
                        END IF;
                    END $$;
                
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251208072755_AddScheduledSlotWorkflowFields') THEN

                    DO $$
                    BEGIN
                        IF NOT EXISTS (
                            SELECT 1 FROM information_schema.columns 
                            WHERE table_name = 'ScheduledSlots' AND column_name = 'PostedAt'
                        ) THEN
                            ALTER TABLE "ScheduledSlots" ADD COLUMN "PostedAt" timestamp with time zone NULL;
                        END IF;
                    END $$;
                
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251208072755_AddScheduledSlotWorkflowFields') THEN

                    DO $$
                    BEGIN
                        IF NOT EXISTS (
                            SELECT 1 FROM information_schema.columns 
                            WHERE table_name = 'ScheduledSlots' AND column_name = 'RescheduleRequestedDate'
                        ) THEN
                            ALTER TABLE "ScheduledSlots" ADD COLUMN "RescheduleRequestedDate" timestamp with time zone NULL;
                        END IF;
                    END $$;
                
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251208072755_AddScheduledSlotWorkflowFields') THEN

                    DO $$
                    BEGIN
                        IF NOT EXISTS (
                            SELECT 1 FROM information_schema.columns 
                            WHERE table_name = 'ScheduledSlots' AND column_name = 'RescheduleRequestedTime'
                        ) THEN
                            ALTER TABLE "ScheduledSlots" ADD COLUMN "RescheduleRequestedTime" interval NULL;
                        END IF;
                    END $$;
                
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251208072755_AddScheduledSlotWorkflowFields') THEN

                    DO $$
                    BEGIN
                        IF NOT EXISTS (
                            SELECT 1 FROM information_schema.columns 
                            WHERE table_name = 'ScheduledSlots' AND column_name = 'RescheduleReason'
                        ) THEN
                            ALTER TABLE "ScheduledSlots" ADD COLUMN "RescheduleReason" character varying(500) NULL;
                        END IF;
                    END $$;
                
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251208072755_AddScheduledSlotWorkflowFields') THEN

                    DO $$
                    BEGIN
                        IF NOT EXISTS (
                            SELECT 1 FROM information_schema.columns 
                            WHERE table_name = 'ScheduledSlots' AND column_name = 'RescheduleNotes'
                        ) THEN
                            ALTER TABLE "ScheduledSlots" ADD COLUMN "RescheduleNotes" character varying(1000) NULL;
                        END IF;
                    END $$;
                
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251208072755_AddScheduledSlotWorkflowFields') THEN

                    DO $$
                    BEGIN
                        IF NOT EXISTS (
                            SELECT 1 FROM information_schema.columns 
                            WHERE table_name = 'ScheduledSlots' AND column_name = 'RescheduleRequestedBySiId'
                        ) THEN
                            ALTER TABLE "ScheduledSlots" ADD COLUMN "RescheduleRequestedBySiId" uuid NULL;
                        END IF;
                    END $$;
                
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251208072755_AddScheduledSlotWorkflowFields') THEN

                    DO $$
                    BEGIN
                        IF NOT EXISTS (
                            SELECT 1 FROM information_schema.columns 
                            WHERE table_name = 'ScheduledSlots' AND column_name = 'RescheduleRequestedAt'
                        ) THEN
                            ALTER TABLE "ScheduledSlots" ADD COLUMN "RescheduleRequestedAt" timestamp with time zone NULL;
                        END IF;
                    END $$;
                
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251208072755_AddScheduledSlotWorkflowFields') THEN

                    UPDATE "ScheduledSlots"
                    SET "Status" = 'Draft'
                    WHERE "Status" = 'Planned' OR "Status" IS NULL;
                
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251208072755_AddScheduledSlotWorkflowFields') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251208072755_AddScheduledSlotWorkflowFields', '10.0.3');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251208075939_AddBarcodeToMaterial') THEN

                    DO $$
                    BEGIN
                        IF NOT EXISTS (
                            SELECT 1 FROM information_schema.columns 
                            WHERE table_name = 'Materials' AND column_name = 'Barcode'
                        ) THEN
                            ALTER TABLE "Materials" ADD COLUMN "Barcode" character varying(200) NULL;
                        END IF;
                    END $$;
                
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251208075939_AddBarcodeToMaterial') THEN

                    DO $$
                    BEGIN
                        IF NOT EXISTS (
                            SELECT 1 FROM pg_indexes 
                            WHERE indexname = 'IX_Materials_CompanyId_Barcode'
                        ) THEN
                            CREATE UNIQUE INDEX "IX_Materials_CompanyId_Barcode" 
                            ON "Materials" ("CompanyId", "Barcode") 
                            WHERE "Barcode" IS NOT NULL;
                        END IF;
                    END $$;
                
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251208075939_AddBarcodeToMaterial') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251208075939_AddBarcodeToMaterial', '10.0.3');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251208083105_AddMovementTypeAndLocationType') THEN
    ALTER TABLE "StockMovements" ADD "MovementTypeId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251208083105_AddMovementTypeAndLocationType') THEN
    ALTER TABLE "StockLocations" ADD "LocationTypeId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251208083105_AddMovementTypeAndLocationType') THEN
    ALTER TABLE "StockLocations" ADD "WarehouseId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251208083105_AddMovementTypeAndLocationType') THEN
    CREATE TABLE "LocationTypes" (
        "Id" uuid NOT NULL,
        "Code" character varying(50) NOT NULL,
        "Name" character varying(200) NOT NULL,
        "Description" text,
        "RequiresServiceInstallerId" boolean NOT NULL,
        "RequiresBuildingId" boolean NOT NULL,
        "RequiresWarehouseId" boolean NOT NULL,
        "AutoCreate" boolean NOT NULL,
        "AutoCreateTrigger" character varying(50),
        "IsActive" boolean NOT NULL,
        "SortOrder" integer NOT NULL,
        "CompanyId" uuid,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "IsDeleted" boolean NOT NULL,
        "DeletedAt" timestamp with time zone,
        "DeletedByUserId" uuid,
        "RowVersion" bytea,
        CONSTRAINT "PK_LocationTypes" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251208083105_AddMovementTypeAndLocationType') THEN
    CREATE TABLE "MovementTypes" (
        "Id" uuid NOT NULL,
        "Code" character varying(50) NOT NULL,
        "Name" character varying(200) NOT NULL,
        "Description" text,
        "Direction" character varying(20) NOT NULL,
        "RequiresFromLocation" boolean NOT NULL,
        "RequiresToLocation" boolean NOT NULL,
        "RequiresOrderId" boolean NOT NULL,
        "RequiresServiceInstallerId" boolean NOT NULL,
        "RequiresPartnerId" boolean NOT NULL,
        "AffectsStockBalance" boolean NOT NULL,
        "StockImpact" character varying(20) NOT NULL,
        "IsActive" boolean NOT NULL,
        "SortOrder" integer NOT NULL,
        "CompanyId" uuid,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "IsDeleted" boolean NOT NULL,
        "DeletedAt" timestamp with time zone,
        "DeletedByUserId" uuid,
        "RowVersion" bytea,
        CONSTRAINT "PK_MovementTypes" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251208083105_AddMovementTypeAndLocationType') THEN
    CREATE INDEX "IX_StockMovements_MovementTypeId" ON "StockMovements" ("MovementTypeId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251208083105_AddMovementTypeAndLocationType') THEN
    CREATE INDEX "IX_StockLocations_CompanyId_LocationTypeId" ON "StockLocations" ("CompanyId", "LocationTypeId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251208083105_AddMovementTypeAndLocationType') THEN
    CREATE INDEX "IX_StockLocations_LocationTypeId" ON "StockLocations" ("LocationTypeId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251208083105_AddMovementTypeAndLocationType') THEN
    CREATE INDEX "IX_StockLocations_WarehouseId" ON "StockLocations" ("WarehouseId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251208083105_AddMovementTypeAndLocationType') THEN
    CREATE UNIQUE INDEX "IX_LocationTypes_CompanyId_Code" ON "LocationTypes" ("CompanyId", "Code");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251208083105_AddMovementTypeAndLocationType') THEN
    CREATE INDEX "IX_LocationTypes_CompanyId_IsActive" ON "LocationTypes" ("CompanyId", "IsActive");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251208083105_AddMovementTypeAndLocationType') THEN
    CREATE UNIQUE INDEX "IX_MovementTypes_CompanyId_Code" ON "MovementTypes" ("CompanyId", "Code");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251208083105_AddMovementTypeAndLocationType') THEN
    CREATE INDEX "IX_MovementTypes_CompanyId_IsActive" ON "MovementTypes" ("CompanyId", "IsActive");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251208083105_AddMovementTypeAndLocationType') THEN
    ALTER TABLE "StockLocations" ADD CONSTRAINT "FK_StockLocations_LocationTypes_LocationTypeId" FOREIGN KEY ("LocationTypeId") REFERENCES "LocationTypes" ("Id") ON DELETE SET NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251208083105_AddMovementTypeAndLocationType') THEN
    ALTER TABLE "StockMovements" ADD CONSTRAINT "FK_StockMovements_MovementTypes_MovementTypeId" FOREIGN KEY ("MovementTypeId") REFERENCES "MovementTypes" ("Id") ON DELETE SET NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251208083105_AddMovementTypeAndLocationType') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251208083105_AddMovementTypeAndLocationType', '10.0.3');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251208084246_AddMaterialTaggingSystem') THEN
    ALTER TABLE "Materials" ADD "MaterialCategoryId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251208084246_AddMaterialTaggingSystem') THEN
    CREATE TABLE "MaterialAttributes" (
        "Id" uuid NOT NULL,
        "MaterialId" uuid NOT NULL,
        "Key" character varying(100) NOT NULL,
        "Value" character varying(500) NOT NULL,
        "DataType" character varying(20) NOT NULL DEFAULT 'String',
        "DisplayOrder" integer NOT NULL,
        "CompanyId" uuid,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "IsDeleted" boolean NOT NULL,
        "DeletedAt" timestamp with time zone,
        "DeletedByUserId" uuid,
        "RowVersion" bytea,
        CONSTRAINT "PK_MaterialAttributes" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_MaterialAttributes_Materials_MaterialId" FOREIGN KEY ("MaterialId") REFERENCES "Materials" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251208084246_AddMaterialTaggingSystem') THEN
    CREATE TABLE "MaterialTags" (
        "Id" uuid NOT NULL,
        "Name" character varying(100) NOT NULL,
        "Description" text,
        "Color" character varying(7),
        "DisplayOrder" integer NOT NULL,
        "IsActive" boolean NOT NULL,
        "CompanyId" uuid,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "IsDeleted" boolean NOT NULL,
        "DeletedAt" timestamp with time zone,
        "DeletedByUserId" uuid,
        "RowVersion" bytea,
        CONSTRAINT "PK_MaterialTags" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251208084246_AddMaterialTaggingSystem') THEN
    CREATE TABLE "MaterialVerticals" (
        "Id" uuid NOT NULL,
        "Code" character varying(50) NOT NULL,
        "Name" character varying(200) NOT NULL,
        "Description" text,
        "DisplayOrder" integer NOT NULL,
        "IsActive" boolean NOT NULL,
        "CompanyId" uuid,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "IsDeleted" boolean NOT NULL,
        "DeletedAt" timestamp with time zone,
        "DeletedByUserId" uuid,
        "RowVersion" bytea,
        CONSTRAINT "PK_MaterialVerticals" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251208084246_AddMaterialTaggingSystem') THEN
    CREATE TABLE "MaterialMaterialTags" (
        "MaterialId" uuid NOT NULL,
        "MaterialTagId" uuid NOT NULL,
        CONSTRAINT "PK_MaterialMaterialTags" PRIMARY KEY ("MaterialId", "MaterialTagId"),
        CONSTRAINT "FK_MaterialMaterialTags_MaterialTags_MaterialTagId" FOREIGN KEY ("MaterialTagId") REFERENCES "MaterialTags" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_MaterialMaterialTags_Materials_MaterialId" FOREIGN KEY ("MaterialId") REFERENCES "Materials" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251208084246_AddMaterialTaggingSystem') THEN
    CREATE TABLE "MaterialMaterialVerticals" (
        "MaterialId" uuid NOT NULL,
        "MaterialVerticalId" uuid NOT NULL,
        CONSTRAINT "PK_MaterialMaterialVerticals" PRIMARY KEY ("MaterialId", "MaterialVerticalId"),
        CONSTRAINT "FK_MaterialMaterialVerticals_MaterialVerticals_MaterialVertica~" FOREIGN KEY ("MaterialVerticalId") REFERENCES "MaterialVerticals" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_MaterialMaterialVerticals_Materials_MaterialId" FOREIGN KEY ("MaterialId") REFERENCES "Materials" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251208084246_AddMaterialTaggingSystem') THEN
    CREATE INDEX "IX_Materials_MaterialCategoryId" ON "Materials" ("MaterialCategoryId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251208084246_AddMaterialTaggingSystem') THEN
    CREATE INDEX "IX_MaterialAttributes_MaterialId" ON "MaterialAttributes" ("MaterialId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251208084246_AddMaterialTaggingSystem') THEN
    CREATE INDEX "IX_MaterialAttributes_MaterialId_Key" ON "MaterialAttributes" ("MaterialId", "Key");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251208084246_AddMaterialTaggingSystem') THEN
    CREATE INDEX "IX_MaterialMaterialTags_MaterialTagId" ON "MaterialMaterialTags" ("MaterialTagId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251208084246_AddMaterialTaggingSystem') THEN
    CREATE INDEX "IX_MaterialMaterialVerticals_MaterialVerticalId" ON "MaterialMaterialVerticals" ("MaterialVerticalId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251208084246_AddMaterialTaggingSystem') THEN
    CREATE INDEX "IX_MaterialTags_CompanyId_IsActive" ON "MaterialTags" ("CompanyId", "IsActive");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251208084246_AddMaterialTaggingSystem') THEN
    CREATE UNIQUE INDEX "IX_MaterialTags_CompanyId_Name" ON "MaterialTags" ("CompanyId", "Name");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251208084246_AddMaterialTaggingSystem') THEN
    CREATE UNIQUE INDEX "IX_MaterialVerticals_CompanyId_Code" ON "MaterialVerticals" ("CompanyId", "Code");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251208084246_AddMaterialTaggingSystem') THEN
    CREATE INDEX "IX_MaterialVerticals_CompanyId_IsActive" ON "MaterialVerticals" ("CompanyId", "IsActive");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251208084246_AddMaterialTaggingSystem') THEN
    ALTER TABLE "Materials" ADD CONSTRAINT "FK_Materials_MaterialCategories_MaterialCategoryId" FOREIGN KEY ("MaterialCategoryId") REFERENCES "MaterialCategories" ("Id") ON DELETE SET NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251208084246_AddMaterialTaggingSystem') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251208084246_AddMaterialTaggingSystem', '10.0.3');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251209071111_AddCustomerPreferenceTable') THEN
    CREATE TABLE customer_preferences (
        "Id" uuid NOT NULL,
        customer_phone character varying(50) NOT NULL,
        uses_whatsapp boolean,
        last_whatsapp_check timestamp with time zone,
        last_whatsapp_success timestamp with time zone,
        last_whatsapp_failure timestamp with time zone,
        consecutive_whatsapp_failures integer NOT NULL,
        preferred_channel character varying(50),
        notes character varying(1000),
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        CONSTRAINT "PK_customer_preferences" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251209071111_AddCustomerPreferenceTable') THEN
    CREATE TABLE sms_gateways (
        "Id" uuid NOT NULL,
        device_name character varying(200) NOT NULL,
        base_url character varying(500) NOT NULL,
        api_key character varying(200) NOT NULL,
        last_seen_at_utc timestamp with time zone NOT NULL,
        is_active boolean NOT NULL,
        additional_info character varying(1000),
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        CONSTRAINT "PK_sms_gateways" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251209071111_AddCustomerPreferenceTable') THEN
    CREATE UNIQUE INDEX "IX_customer_preferences_customer_phone" ON customer_preferences (customer_phone);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251209071111_AddCustomerPreferenceTable') THEN
    CREATE INDEX "IX_customer_preferences_last_whatsapp_check" ON customer_preferences (last_whatsapp_check);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251209071111_AddCustomerPreferenceTable') THEN
    CREATE INDEX "IX_customer_preferences_uses_whatsapp" ON customer_preferences (uses_whatsapp);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251209071111_AddCustomerPreferenceTable') THEN
    CREATE INDEX "IX_sms_gateways_is_active" ON sms_gateways (is_active) WHERE is_active = true;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251209071111_AddCustomerPreferenceTable') THEN
    CREATE INDEX "IX_sms_gateways_last_seen_at_utc" ON sms_gateways (last_seen_at_utc);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251209071111_AddCustomerPreferenceTable') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251209071111_AddCustomerPreferenceTable', '10.0.3');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251209080853_AddOneDriveFieldsToFile') THEN
    ALTER TABLE "Files" ADD "OneDriveFileId" character varying(500);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251209080853_AddOneDriveFieldsToFile') THEN
    ALTER TABLE "Files" ADD "OneDriveWebUrl" character varying(1000);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251209080853_AddOneDriveFieldsToFile') THEN
    ALTER TABLE "Files" ADD "OneDriveSyncStatus" character varying(50) NOT NULL DEFAULT 'NotSynced';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251209080853_AddOneDriveFieldsToFile') THEN
    ALTER TABLE "Files" ADD "OneDriveSyncedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251209080853_AddOneDriveFieldsToFile') THEN
    ALTER TABLE "Files" ADD "OneDriveSyncError" character varying(2000);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251209080853_AddOneDriveFieldsToFile') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251209080853_AddOneDriveFieldsToFile', '10.0.3');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251209090436_CheckPendingChanges') THEN
    ALTER TABLE "Files" ALTER COLUMN "OneDriveWebUrl" TYPE text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251209090436_CheckPendingChanges') THEN
    ALTER TABLE "Files" ALTER COLUMN "OneDriveSyncStatus" TYPE text;
    ALTER TABLE "Files" ALTER COLUMN "OneDriveSyncStatus" DROP DEFAULT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251209090436_CheckPendingChanges') THEN
    ALTER TABLE "Files" ALTER COLUMN "OneDriveSyncError" TYPE text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251209090436_CheckPendingChanges') THEN
    ALTER TABLE "Files" ALTER COLUMN "OneDriveFileId" TYPE text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251209090436_CheckPendingChanges') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251209090436_CheckPendingChanges', '10.0.3');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251210234157_FixModelSnapshot') THEN
    ALTER TABLE "ParsedOrderDrafts" ADD "OnuPassword" character varying(200);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251210234157_FixModelSnapshot') THEN
    ALTER TABLE "Orders" ADD "OnuPasswordEncrypted" character varying(500);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251210234157_FixModelSnapshot') THEN
    ALTER TABLE "JobEarningRecords" ALTER COLUMN "JobType" DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251210234157_FixModelSnapshot') THEN
    ALTER TABLE "JobEarningRecords" ADD "OrderTypeCode" character varying(50) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251210234157_FixModelSnapshot') THEN
    ALTER TABLE "JobEarningRecords" ADD "OrderTypeId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251210234157_FixModelSnapshot') THEN
    ALTER TABLE "JobEarningRecords" ADD "OrderTypeName" character varying(100) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251210234157_FixModelSnapshot') THEN
    CREATE INDEX "IX_JobEarningRecords_OrderTypeId" ON "JobEarningRecords" ("OrderTypeId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251210234157_FixModelSnapshot') THEN
    ALTER TABLE "JobEarningRecords" ADD CONSTRAINT "FK_JobEarningRecords_OrderTypes_OrderTypeId" FOREIGN KEY ("OrderTypeId") REFERENCES "OrderTypes" ("Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251210234157_FixModelSnapshot') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251210234157_FixModelSnapshot', '10.0.3');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251212031140_FixBuildErrors') THEN
    ALTER TABLE "ParsedOrderDrafts" ADD "FileHash" text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251212031140_FixBuildErrors') THEN
    CREATE TABLE "RefreshTokens" (
        "Id" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "TokenHash" text NOT NULL,
        "ExpiresAt" timestamp with time zone NOT NULL,
        "IsRevoked" boolean NOT NULL,
        "RevokedAt" timestamp with time zone,
        "CreatedFromIp" text,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_RefreshTokens" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_RefreshTokens_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251212031140_FixBuildErrors') THEN
    CREATE INDEX "IX_RefreshTokens_UserId" ON "RefreshTokens" ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251212031140_FixBuildErrors') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251212031140_FixBuildErrors', '10.0.3');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251212033704_AddUpdatedAtToCostCentres') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251212033704_AddUpdatedAtToCostCentres', '10.0.3');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251217101555_AddDeletedByUserIdToEmailMessages') THEN
    ALTER TABLE "EmailAttachments" ADD "DeletedByUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251217101555_AddDeletedByUserIdToEmailMessages') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251217101555_AddDeletedByUserIdToEmailMessages', '10.0.3');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251218055129_AddAwoNumberToParsedOrderDraft') THEN
    ALTER TABLE "ParsedOrderDrafts" ADD "AwoNumber" character varying(100);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251218055129_AddAwoNumberToParsedOrderDraft') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251218055129_AddAwoNumberToParsedOrderDraft', '10.0.3');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251218074652_AddNetworkFieldsToParsedOrderDraft') THEN
    ALTER TABLE "ParsedOrderDrafts" ADD "InternetGateway" character varying(50);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251218074652_AddNetworkFieldsToParsedOrderDraft') THEN
    ALTER TABLE "ParsedOrderDrafts" ADD "InternetLanIp" character varying(50);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251218074652_AddNetworkFieldsToParsedOrderDraft') THEN
    ALTER TABLE "ParsedOrderDrafts" ADD "InternetSubnetMask" character varying(50);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251218074652_AddNetworkFieldsToParsedOrderDraft') THEN
    ALTER TABLE "ParsedOrderDrafts" ADD "InternetWanIp" character varying(50);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251218074652_AddNetworkFieldsToParsedOrderDraft') THEN
    ALTER TABLE "ParsedOrderDrafts" ADD "Password" character varying(200);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251218074652_AddNetworkFieldsToParsedOrderDraft') THEN
    ALTER TABLE "ParsedOrderDrafts" ADD "Username" character varying(200);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251218074652_AddNetworkFieldsToParsedOrderDraft') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251218074652_AddNetworkFieldsToParsedOrderDraft', '10.0.3');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251219020647_RenameInstallationTypeToOrderCategory') THEN

                    CREATE TABLE IF NOT EXISTS "OrderCategories" (
                        "Id" uuid NOT NULL,
                        "DepartmentId" uuid NULL,
                        "Name" character varying(100) NOT NULL,
                        "Code" character varying(50) NOT NULL,
                        "Description" character varying(500) NULL,
                        "IsActive" boolean NOT NULL,
                        "DisplayOrder" integer NOT NULL,
                        "CompanyId" uuid NULL,
                        "CreatedAt" timestamp with time zone NOT NULL,
                        "UpdatedAt" timestamp with time zone NOT NULL,
                        "IsDeleted" boolean NOT NULL,
                        "DeletedAt" timestamp with time zone NULL,
                        "DeletedByUserId" uuid NULL,
                        "RowVersion" bytea NULL,
                        CONSTRAINT "PK_OrderCategories" PRIMARY KEY ("Id"),
                        CONSTRAINT "FK_OrderCategories_Departments_DepartmentId" FOREIGN KEY ("DepartmentId") 
                            REFERENCES "Departments" ("Id") ON DELETE SET NULL
                    );

                    -- Copy data from InstallationTypes to OrderCategories
                    INSERT INTO "OrderCategories" (
                        "Id", "DepartmentId", "Name", "Code", "Description", "IsActive", 
                        "DisplayOrder", "CompanyId", "CreatedAt", "UpdatedAt", "IsDeleted", 
                        "DeletedAt", "DeletedByUserId", "RowVersion"
                    )
                    SELECT 
                        "Id", "DepartmentId", "Name", "Code", "Description", "IsActive", 
                        "DisplayOrder", "CompanyId", "CreatedAt", "UpdatedAt", "IsDeleted", 
                        "DeletedAt", "DeletedByUserId", "RowVersion"
                    FROM "InstallationTypes";
                
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251219020647_RenameInstallationTypeToOrderCategory') THEN
    CREATE INDEX "IX_OrderCategories_CompanyId_Code" ON "OrderCategories" ("CompanyId", "Code");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251219020647_RenameInstallationTypeToOrderCategory') THEN
    CREATE INDEX "IX_OrderCategories_CompanyId_DepartmentId" ON "OrderCategories" ("CompanyId", "DepartmentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251219020647_RenameInstallationTypeToOrderCategory') THEN
    CREATE INDEX "IX_OrderCategories_CompanyId_IsActive" ON "OrderCategories" ("CompanyId", "IsActive");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251219020647_RenameInstallationTypeToOrderCategory') THEN
    CREATE INDEX "IX_OrderCategories_DepartmentId" ON "OrderCategories" ("DepartmentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251219020647_RenameInstallationTypeToOrderCategory') THEN
    ALTER TABLE "Buildings" DROP CONSTRAINT "FK_Buildings_BuildingTypes_BuildingTypeId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251219020647_RenameInstallationTypeToOrderCategory') THEN
    ALTER TABLE "Orders" DROP CONSTRAINT "FK_Orders_InstallationTypes_InstallationTypeId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251219020647_RenameInstallationTypeToOrderCategory') THEN
    DROP INDEX "IX_Buildings_BuildingTypeId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251219020647_RenameInstallationTypeToOrderCategory') THEN
    ALTER TABLE "Buildings" DROP COLUMN "BuildingTypeId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251219020647_RenameInstallationTypeToOrderCategory') THEN
    ALTER TABLE "PnlDetailPerOrders" RENAME COLUMN "InstallationType" TO "OrderCategory";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251219020647_RenameInstallationTypeToOrderCategory') THEN
    ALTER TABLE "Orders" RENAME COLUMN "InstallationTypeId" TO "OrderCategoryId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251219020647_RenameInstallationTypeToOrderCategory') THEN
    ALTER INDEX "IX_Orders_OrderTypeId_InstallationTypeId_InstallationMethodId" RENAME TO "IX_Orders_OrderTypeId_OrderCategoryId_InstallationMethodId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251219020647_RenameInstallationTypeToOrderCategory') THEN
    ALTER INDEX "IX_Orders_InstallationTypeId" RENAME TO "IX_Orders_OrderCategoryId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251219020647_RenameInstallationTypeToOrderCategory') THEN
    ALTER TABLE "GponSiJobRates" RENAME COLUMN "InstallationTypeId" TO "OrderCategoryId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251219020647_RenameInstallationTypeToOrderCategory') THEN
    ALTER INDEX "IX_GponSiJobRates_PartnerGroupId_OrderTypeId_InstallationTypeId" RENAME TO "IX_GponSiJobRates_PartnerGroupId_OrderTypeId_OrderCategoryId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251219020647_RenameInstallationTypeToOrderCategory') THEN
    ALTER INDEX "IX_GponSiJobRates_OrderTypeId_InstallationTypeId_InstallationM~" RENAME TO "IX_GponSiJobRates_OrderTypeId_OrderCategoryId_InstallationMeth~";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251219020647_RenameInstallationTypeToOrderCategory') THEN
    ALTER TABLE "GponSiCustomRates" RENAME COLUMN "InstallationTypeId" TO "OrderCategoryId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251219020647_RenameInstallationTypeToOrderCategory') THEN
    ALTER INDEX "IX_GponSiCustomRates_ServiceInstallerId_OrderTypeId_Installati~" RENAME TO "IX_GponSiCustomRates_ServiceInstallerId_OrderTypeId_OrderCateg~";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251219020647_RenameInstallationTypeToOrderCategory') THEN
    ALTER TABLE "GponPartnerJobRates" RENAME COLUMN "InstallationTypeId" TO "OrderCategoryId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251219020647_RenameInstallationTypeToOrderCategory') THEN
    ALTER INDEX "IX_GponPartnerJobRates_PartnerId_OrderTypeId_InstallationTypeI~" RENAME TO "IX_GponPartnerJobRates_PartnerId_OrderTypeId_OrderCategoryId_I~";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251219020647_RenameInstallationTypeToOrderCategory') THEN
    ALTER INDEX "IX_GponPartnerJobRates_PartnerGroupId_OrderTypeId_Installation~" RENAME TO "IX_GponPartnerJobRates_PartnerGroupId_OrderTypeId_OrderCategor~";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251219020647_RenameInstallationTypeToOrderCategory') THEN

                    DO $$
                    BEGIN
                        -- Add AdditionalContactNumber to ParsedOrderDrafts if it doesn't exist
                        IF NOT EXISTS (
                            SELECT 1 FROM information_schema.columns 
                            WHERE table_name = 'ParsedOrderDrafts' AND column_name = 'AdditionalContactNumber'
                        ) THEN
                            ALTER TABLE "ParsedOrderDrafts" 
                            ADD COLUMN "AdditionalContactNumber" character varying(100) NULL;
                        END IF;

                        -- Add Issue to ParsedOrderDrafts if it doesn't exist
                        IF NOT EXISTS (
                            SELECT 1 FROM information_schema.columns 
                            WHERE table_name = 'ParsedOrderDrafts' AND column_name = 'Issue'
                        ) THEN
                            ALTER TABLE "ParsedOrderDrafts" 
                            ADD COLUMN "Issue" character varying(1000) NULL;
                        END IF;

                        -- Add Issue to Orders if it doesn't exist
                        IF NOT EXISTS (
                            SELECT 1 FROM information_schema.columns 
                            WHERE table_name = 'Orders' AND column_name = 'Issue'
                        ) THEN
                            ALTER TABLE "Orders" 
                            ADD COLUMN "Issue" character varying(1000) NULL;
                        END IF;

                        -- Add Solution to Orders if it doesn't exist
                        IF NOT EXISTS (
                            SELECT 1 FROM information_schema.columns 
                            WHERE table_name = 'Orders' AND column_name = 'Solution'
                        ) THEN
                            ALTER TABLE "Orders" 
                            ADD COLUMN "Solution" character varying(2000) NULL;
                        END IF;
                    END $$;
                
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251219020647_RenameInstallationTypeToOrderCategory') THEN

                    DO $$
                    BEGIN
                        IF NOT EXISTS (
                            SELECT 1 FROM pg_indexes 
                            WHERE schemaname = 'public' 
                            AND tablename = 'Buildings' 
                            AND indexname = 'IX_Buildings_InstallationMethodId'
                        ) THEN
                            CREATE INDEX "IX_Buildings_InstallationMethodId" ON "Buildings" ("InstallationMethodId");
                        END IF;
                    END $$;
                
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251219020647_RenameInstallationTypeToOrderCategory') THEN

                    DO $$
                    BEGIN
                        -- Add FK_Orders_OrderCategories_OrderCategoryId if it doesn't exist
                        IF NOT EXISTS (
                            SELECT 1 FROM information_schema.table_constraints 
                            WHERE constraint_name = 'FK_Orders_OrderCategories_OrderCategoryId'
                            AND table_name = 'Orders'
                        ) THEN
                            ALTER TABLE "Orders"
                            ADD CONSTRAINT "FK_Orders_OrderCategories_OrderCategoryId"
                            FOREIGN KEY ("OrderCategoryId")
                            REFERENCES "OrderCategories" ("Id")
                            ON DELETE SET NULL;
                        END IF;

                        -- Add FK_Buildings_InstallationMethods_InstallationMethodId if it doesn't exist
                        IF NOT EXISTS (
                            SELECT 1 FROM information_schema.table_constraints 
                            WHERE constraint_name = 'FK_Buildings_InstallationMethods_InstallationMethodId'
                            AND table_name = 'Buildings'
                        ) THEN
                            ALTER TABLE "Buildings"
                            ADD CONSTRAINT "FK_Buildings_InstallationMethods_InstallationMethodId"
                            FOREIGN KEY ("InstallationMethodId")
                            REFERENCES "InstallationMethods" ("Id")
                            ON DELETE SET NULL;
                        END IF;
                    END $$;
                
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251219020647_RenameInstallationTypeToOrderCategory') THEN
    DROP TABLE "InstallationTypes";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251219020647_RenameInstallationTypeToOrderCategory') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251219020647_RenameInstallationTypeToOrderCategory', '10.0.3');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260105101702_AddBuildingTypeIdToBuildings') THEN
    ALTER TABLE "Buildings" ADD "BuildingTypeId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260105101702_AddBuildingTypeIdToBuildings') THEN
    CREATE INDEX "IX_Buildings_BuildingTypeId" ON "Buildings" ("BuildingTypeId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260105101702_AddBuildingTypeIdToBuildings') THEN
    ALTER TABLE "Buildings" ADD CONSTRAINT "FK_Buildings_BuildingTypes_BuildingTypeId" FOREIGN KEY ("BuildingTypeId") REFERENCES "BuildingTypes" ("Id") ON DELETE SET NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260105101702_AddBuildingTypeIdToBuildings') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260105101702_AddBuildingTypeIdToBuildings', '10.0.3');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260105104100_RemoveServiceInstallerData') THEN

                    UPDATE "Orders" 
                    SET "AssignedSiId" = NULL 
                    WHERE "AssignedSiId" IS NOT NULL;
                
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260105104100_RemoveServiceInstallerData') THEN

                    UPDATE "StockMovements" 
                    SET "ServiceInstallerId" = NULL 
                    WHERE "ServiceInstallerId" IS NOT NULL;
                
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260105104100_RemoveServiceInstallerData') THEN

                    UPDATE "StockLocations" 
                    SET "LinkedServiceInstallerId" = NULL 
                    WHERE "LinkedServiceInstallerId" IS NOT NULL;
                
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260105104100_RemoveServiceInstallerData') THEN

                    DO $$ 
                    BEGIN
                        IF EXISTS (SELECT FROM information_schema.tables WHERE table_name = 'ScheduledSlots') THEN
                            DELETE FROM "ScheduledSlots";
                        END IF;
                        
                        IF EXISTS (SELECT FROM information_schema.tables WHERE table_name = 'SiAvailabilities') THEN
                            DELETE FROM "SiAvailabilities";
                        END IF;
                        
                        IF EXISTS (SELECT FROM information_schema.tables WHERE table_name = 'SiLeaveRequests') THEN
                            DELETE FROM "SiLeaveRequests";
                        END IF;
                        
                        IF EXISTS (SELECT FROM information_schema.tables WHERE table_name = 'SiRatePlans') THEN
                            DELETE FROM "SiRatePlans";
                        END IF;
                        
                        IF EXISTS (SELECT FROM information_schema.tables WHERE table_name = 'GponSiCustomRates') THEN
                            DELETE FROM "GponSiCustomRates";
                        END IF;
                        
                        IF EXISTS (SELECT FROM information_schema.tables WHERE table_name = 'JobEarningRecords') THEN
                            DELETE FROM "JobEarningRecords";
                        END IF;
                        
                        IF EXISTS (SELECT FROM information_schema.tables WHERE table_name = 'PayrollLines') THEN
                            DELETE FROM "PayrollLines";
                        END IF;
                        
                        IF EXISTS (SELECT FROM information_schema.tables WHERE table_name = 'PnlDetailPerOrders') THEN
                            DELETE FROM "PnlDetailPerOrders";
                        END IF;
                    END $$;
                
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260105104100_RemoveServiceInstallerData') THEN
    DELETE FROM "ServiceInstallerContacts";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260105104100_RemoveServiceInstallerData') THEN
    DELETE FROM "ServiceInstallers";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260105104100_RemoveServiceInstallerData') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260105104100_RemoveServiceInstallerData', '10.0.3');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260105111631_AddAdditionalFieldsToServiceInstallers') THEN
    ALTER TABLE "ServiceInstallers" ADD "Address" character varying(500);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260105111631_AddAdditionalFieldsToServiceInstallers') THEN
    ALTER TABLE "ServiceInstallers" ADD "BankAccountNumber" character varying(50);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260105111631_AddAdditionalFieldsToServiceInstallers') THEN
    ALTER TABLE "ServiceInstallers" ADD "BankName" character varying(200);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260105111631_AddAdditionalFieldsToServiceInstallers') THEN
    ALTER TABLE "ServiceInstallers" ADD "EmergencyContact" character varying(200);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260105111631_AddAdditionalFieldsToServiceInstallers') THEN
    ALTER TABLE "ServiceInstallers" ADD "IcNumber" character varying(50);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260105111631_AddAdditionalFieldsToServiceInstallers') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260105111631_AddAdditionalFieldsToServiceInstallers', '10.0.3');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260105122021_AddInstallerTypeToServiceInstallers') THEN
    ALTER TABLE "ServiceInstallers" ADD "InstallerType" character varying(50);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260105122021_AddInstallerTypeToServiceInstallers') THEN

                    UPDATE "ServiceInstallers"
                    SET "InstallerType" = CASE 
                        WHEN "IsSubcontractor" = true THEN 'Subcontractor' 
                        ELSE 'InHouse' 
                    END
                    WHERE "InstallerType" IS NULL;
                
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260105122021_AddInstallerTypeToServiceInstallers') THEN
    ALTER TABLE "ServiceInstallers" ALTER COLUMN "InstallerType" TYPE character varying(50);
    ALTER TABLE "ServiceInstallers" ALTER COLUMN "InstallerType" SET DEFAULT 'InHouse';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260105122021_AddInstallerTypeToServiceInstallers') THEN

                    ALTER TABLE "ServiceInstallers"
                    ADD CONSTRAINT "CK_ServiceInstallers_InstallerType" 
                    CHECK ("InstallerType" IN ('InHouse', 'Subcontractor'));
                
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260105122021_AddInstallerTypeToServiceInstallers') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260105122021_AddInstallerTypeToServiceInstallers', '10.0.3');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260106014539_CapturePendingModelChanges') THEN
    ALTER TABLE "ServiceInstallers" ALTER COLUMN "SiLevel" SET DEFAULT 'Junior';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260106014539_CapturePendingModelChanges') THEN
    ALTER TABLE "ServiceInstallers" ADD "AvailabilityStatus" character varying(50);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260106014539_CapturePendingModelChanges') THEN
    ALTER TABLE "ServiceInstallers" ADD "ContractEndDate" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260106014539_CapturePendingModelChanges') THEN
    ALTER TABLE "ServiceInstallers" ADD "ContractStartDate" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260106014539_CapturePendingModelChanges') THEN
    ALTER TABLE "ServiceInstallers" ADD "ContractorCompany" character varying(200);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260106014539_CapturePendingModelChanges') THEN
    ALTER TABLE "ServiceInstallers" ADD "ContractorId" character varying(100);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260106014539_CapturePendingModelChanges') THEN
    ALTER TABLE "ServiceInstallers" ADD "EmploymentStatus" character varying(50);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260106014539_CapturePendingModelChanges') THEN
    ALTER TABLE "ServiceInstallers" ADD "HireDate" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260106014539_CapturePendingModelChanges') THEN
    CREATE TABLE "Skills" (
        "Id" uuid NOT NULL,
        "Name" character varying(200) NOT NULL,
        "Code" character varying(100) NOT NULL,
        "Category" character varying(100) NOT NULL,
        "Description" character varying(1000),
        "IsActive" boolean NOT NULL,
        "DisplayOrder" integer NOT NULL,
        "CompanyId" uuid,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "IsDeleted" boolean NOT NULL,
        "DeletedAt" timestamp with time zone,
        "DeletedByUserId" uuid,
        "RowVersion" bytea,
        CONSTRAINT "PK_Skills" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260106014539_CapturePendingModelChanges') THEN
    CREATE TABLE "ServiceInstallerSkills" (
        "Id" uuid NOT NULL,
        "ServiceInstallerId" uuid NOT NULL,
        "SkillId" uuid NOT NULL,
        "AcquiredAt" timestamp with time zone,
        "VerifiedAt" timestamp with time zone,
        "VerifiedByUserId" uuid,
        "Notes" character varying(1000),
        "IsActive" boolean NOT NULL,
        "CompanyId" uuid,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "IsDeleted" boolean NOT NULL,
        "DeletedAt" timestamp with time zone,
        "DeletedByUserId" uuid,
        "RowVersion" bytea,
        CONSTRAINT "PK_ServiceInstallerSkills" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_ServiceInstallerSkills_ServiceInstallers_ServiceInstallerId" FOREIGN KEY ("ServiceInstallerId") REFERENCES "ServiceInstallers" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_ServiceInstallerSkills_Skills_SkillId" FOREIGN KEY ("SkillId") REFERENCES "Skills" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260106014539_CapturePendingModelChanges') THEN
    ALTER TABLE "ServiceInstallers" ADD CONSTRAINT "CK_ServiceInstallers_SiLevel" CHECK ("SiLevel" IN ('Junior', 'Senior'));
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260106014539_CapturePendingModelChanges') THEN
    CREATE INDEX "IX_ServiceInstallerSkills_ServiceInstallerId" ON "ServiceInstallerSkills" ("ServiceInstallerId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260106014539_CapturePendingModelChanges') THEN
    CREATE INDEX "IX_ServiceInstallerSkills_ServiceInstallerId_IsActive" ON "ServiceInstallerSkills" ("ServiceInstallerId", "IsActive");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260106014539_CapturePendingModelChanges') THEN
    CREATE UNIQUE INDEX "IX_ServiceInstallerSkills_ServiceInstallerId_SkillId_IsActive" ON "ServiceInstallerSkills" ("ServiceInstallerId", "SkillId", "IsActive") WHERE "IsActive" = true AND "IsDeleted" = false;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260106014539_CapturePendingModelChanges') THEN
    CREATE INDEX "IX_ServiceInstallerSkills_SkillId" ON "ServiceInstallerSkills" ("SkillId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260106014539_CapturePendingModelChanges') THEN
    CREATE INDEX "IX_Skills_CompanyId_Category_IsActive" ON "Skills" ("CompanyId", "Category", "IsActive");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260106014539_CapturePendingModelChanges') THEN
    CREATE UNIQUE INDEX "IX_Skills_CompanyId_Code" ON "Skills" ("CompanyId", "Code") WHERE "IsDeleted" = false;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260106014539_CapturePendingModelChanges') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260106014539_CapturePendingModelChanges', '10.0.3');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260106014834_SeedAllReferenceData') THEN

    DO $$
    DECLARE
        v_company_id UUID;
        v_gpon_department_id UUID;
        v_super_admin_role_id UUID;
        v_director_role_id UUID;
        v_hod_role_id UUID;
        v_supervisor_role_id UUID;
        v_finance_role_id UUID;
        v_admin_user_id UUID;
        v_finance_user_id UUID;
        v_admin_password_hash TEXT;
        v_finance_password_hash TEXT;
    BEGIN
        -- Pre-calculated password hashes (SHA256 with salt, Base64 encoded)
        -- Admin: J@saw007 + CephasOps_Salt_2024
        v_admin_password_hash := 'DPoZR4yEm+hNKLt05409XYJPWGJC0KisAMQHVIOHp2Q=';
        -- Finance: E5pr!tg@L + CephasOps_Salt_2024
        v_finance_password_hash := 'M3YObIZ4+LOYNmkCSEIK8+kr64rQmW7x28HBNr3ZfoE=';
        
        -- Seed Default Company
        -- Check if IsDeleted column exists before using it
        IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Companies' AND column_name = 'IsDeleted') THEN
            SELECT "Id" INTO v_company_id 
            FROM "Companies" 
            WHERE "IsDeleted" = false 
            ORDER BY "CreatedAt" ASC 
            LIMIT 1;
        ELSE
            SELECT "Id" INTO v_company_id 
            FROM "Companies" 
            ORDER BY "CreatedAt" ASC 
            LIMIT 1;
        END IF;
        
        IF v_company_id IS NULL THEN
            v_company_id := gen_random_uuid();
            INSERT INTO "Companies" ("Id", "LegalName", "ShortName", "Vertical", "IsActive", "CreatedAt", "IsDeleted")
            VALUES (v_company_id, 'Cephas', 'Cephas', 'General', true, NOW(), false) ON CONFLICT DO NOTHING;
        END IF;
        
        -- Seed Roles
        INSERT INTO "Roles" ("Id", "Name", "Scope")
        SELECT gen_random_uuid(), 'SuperAdmin', 'Global'
        WHERE NOT EXISTS (SELECT 1 FROM "Roles" WHERE "Name" = 'SuperAdmin' AND "Scope" = 'Global')
        RETURNING "Id" INTO v_super_admin_role_id;
        IF v_super_admin_role_id IS NULL THEN
            SELECT "Id" INTO v_super_admin_role_id FROM "Roles" WHERE "Name" = 'SuperAdmin' AND "Scope" = 'Global';
        END IF;
        
        INSERT INTO "Roles" ("Id", "Name", "Scope")
        SELECT gen_random_uuid(), 'Director', 'Global'
        WHERE NOT EXISTS (SELECT 1 FROM "Roles" WHERE "Name" = 'Director' AND "Scope" = 'Global')
        RETURNING "Id" INTO v_director_role_id;
        IF v_director_role_id IS NULL THEN
            SELECT "Id" INTO v_director_role_id FROM "Roles" WHERE "Name" = 'Director' AND "Scope" = 'Global';
        END IF;
        
        INSERT INTO "Roles" ("Id", "Name", "Scope")
        SELECT gen_random_uuid(), 'HeadOfDepartment', 'Global'
        WHERE NOT EXISTS (SELECT 1 FROM "Roles" WHERE "Name" = 'HeadOfDepartment' AND "Scope" = 'Global')
        RETURNING "Id" INTO v_hod_role_id;
        IF v_hod_role_id IS NULL THEN
            SELECT "Id" INTO v_hod_role_id FROM "Roles" WHERE "Name" = 'HeadOfDepartment' AND "Scope" = 'Global';
        END IF;
        
        INSERT INTO "Roles" ("Id", "Name", "Scope")
        SELECT gen_random_uuid(), 'Supervisor', 'Global'
        WHERE NOT EXISTS (SELECT 1 FROM "Roles" WHERE "Name" = 'Supervisor' AND "Scope" = 'Global')
        RETURNING "Id" INTO v_supervisor_role_id;
        IF v_supervisor_role_id IS NULL THEN
            SELECT "Id" INTO v_supervisor_role_id FROM "Roles" WHERE "Name" = 'Supervisor' AND "Scope" = 'Global';
        END IF;
        
        INSERT INTO "Roles" ("Id", "Name", "Scope")
        SELECT gen_random_uuid(), 'FinanceManager', 'Global'
        WHERE NOT EXISTS (SELECT 1 FROM "Roles" WHERE "Name" = 'FinanceManager' AND "Scope" = 'Global')
        RETURNING "Id" INTO v_finance_role_id;
        IF v_finance_role_id IS NULL THEN
            SELECT "Id" INTO v_finance_role_id FROM "Roles" WHERE "Name" = 'FinanceManager' AND "Scope" = 'Global';
        END IF;
        
        -- Seed Default Admin User
        IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Users' AND column_name = 'IsDeleted') THEN
            SELECT "Id" INTO v_admin_user_id FROM "Users" WHERE "Email" = 'simon@cephas.com.my' AND "IsDeleted" = false;
        ELSE
            SELECT "Id" INTO v_admin_user_id FROM "Users" WHERE "Email" = 'simon@cephas.com.my';
        END IF;
        IF v_admin_user_id IS NULL THEN
            v_admin_user_id := gen_random_uuid();
            INSERT INTO "Users" ("Id", "Name", "Email", "PasswordHash", "IsActive", "CreatedAt", "IsDeleted")
            VALUES (v_admin_user_id, 'Simon', 'simon@cephas.com.my', v_admin_password_hash, true, NOW(), false);
        ELSE
            UPDATE "Users" SET "PasswordHash" = v_admin_password_hash, "IsActive" = true
            WHERE "Id" = v_admin_user_id AND "PasswordHash" != v_admin_password_hash;
        END IF;
        
        INSERT INTO "UserRoles" ("UserId", "CompanyId", "RoleId", "CreatedAt")
        SELECT v_admin_user_id, NULL, v_super_admin_role_id, NOW()
        WHERE NOT EXISTS (SELECT 1 FROM "UserRoles" WHERE "UserId" = v_admin_user_id AND "RoleId" = v_super_admin_role_id);
        
        -- Seed GPON Department
        IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Departments' AND column_name = 'IsDeleted') THEN
            SELECT "Id" INTO v_gpon_department_id 
            FROM "Departments" 
            WHERE ("Code" = 'GPON' OR "Name" ILIKE '%GPON%') 
              AND ("CompanyId" = v_company_id OR "CompanyId" IS NULL)
              AND "IsDeleted" = false
            LIMIT 1;
        ELSE
            SELECT "Id" INTO v_gpon_department_id 
            FROM "Departments" 
            WHERE ("Code" = 'GPON' OR "Name" ILIKE '%GPON%') 
              AND ("CompanyId" = v_company_id OR "CompanyId" IS NULL)
            LIMIT 1;
        END IF;
        
        IF v_gpon_department_id IS NULL THEN
            v_gpon_department_id := gen_random_uuid();
            INSERT INTO "Departments" ("Id", "CompanyId", "Name", "Code", "Description", "IsActive", "CreatedAt", "UpdatedAt", "IsDeleted")
            VALUES (v_gpon_department_id, v_company_id, 'GPON', 'GPON', 'GPON Operations Department', true, NOW(), NOW(), false);
        END IF;
        
        -- Seed Finance HOD User
        IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Users' AND column_name = 'IsDeleted') THEN
            SELECT "Id" INTO v_finance_user_id FROM "Users" WHERE "Email" = 'finance@cephas.com.my' AND "IsDeleted" = false;
        ELSE
            SELECT "Id" INTO v_finance_user_id FROM "Users" WHERE "Email" = 'finance@cephas.com.my';
        END IF;
        IF v_finance_user_id IS NULL THEN
            v_finance_user_id := gen_random_uuid();
            INSERT INTO "Users" ("Id", "Name", "Email", "PasswordHash", "IsActive", "CreatedAt", "IsDeleted")
            VALUES (v_finance_user_id, 'Samyu Kavitha', 'finance@cephas.com.my', v_finance_password_hash, true, NOW(), false);
        END IF;
        
        INSERT INTO "UserRoles" ("UserId", "CompanyId", "RoleId", "CreatedAt")
        SELECT v_finance_user_id, v_company_id, v_finance_role_id, NOW()
        WHERE NOT EXISTS (SELECT 1 FROM "UserRoles" WHERE "UserId" = v_finance_user_id AND "RoleId" = v_finance_role_id);
        
        -- Insert DepartmentMembership with conditional IsDeleted check
        IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'DepartmentMemberships' AND column_name = 'IsDeleted') THEN
            INSERT INTO "DepartmentMemberships" ("Id", "CompanyId", "DepartmentId", "UserId", "Role", "IsDefault", "CreatedAt", "UpdatedAt", "IsDeleted")
            SELECT gen_random_uuid(), v_company_id, v_gpon_department_id, v_finance_user_id, 'HOD', true, NOW(), NOW(), false
            WHERE NOT EXISTS (SELECT 1 FROM "DepartmentMemberships" WHERE "UserId" = v_finance_user_id AND "DepartmentId" = v_gpon_department_id AND "IsDeleted" = false);
        ELSE
            INSERT INTO "DepartmentMemberships" ("Id", "CompanyId", "DepartmentId", "UserId", "Role", "IsDefault", "CreatedAt", "UpdatedAt")
            SELECT gen_random_uuid(), v_company_id, v_gpon_department_id, v_finance_user_id, 'HOD', true, NOW(), NOW()
            WHERE NOT EXISTS (SELECT 1 FROM "DepartmentMemberships" WHERE "UserId" = v_finance_user_id AND "DepartmentId" = v_gpon_department_id);
        END IF;
        
        -- Seed Order Types, Categories, Building Types, Splitter Types, Skills, Parser Templates, etc.
        -- (Full seed data continues - see 20250106_SeedAllReferenceData.sql for complete SQL)
        -- Note: Skills seeding is conditional - only if table exists (created in later migration)
        
        IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'Skills') THEN
            INSERT INTO "Skills" ("Id", "CompanyId", "Name", "Code", "Category", "Description", "DisplayOrder", "IsActive", "CreatedAt", "UpdatedAt", "IsDeleted") VALUES
            (gen_random_uuid(), v_company_id, 'Fiber cable installation (indoor)', 'FIBER_CABLE_INDOOR', 'FiberSkills', 'Installation of fiber cables in indoor environments', 1, true, NOW(), NOW(), false),
            (gen_random_uuid(), v_company_id, 'Fiber cable installation (outdoor/aerial)', 'FIBER_CABLE_OUTDOOR', 'FiberSkills', 'Installation of fiber cables in outdoor/aerial environments', 2, true, NOW(), NOW(), false),
            (gen_random_uuid(), v_company_id, 'Fiber splicing (mechanical)', 'FIBER_SPLICE_MECHANICAL', 'FiberSkills', 'Mechanical fiber splicing techniques', 3, true, NOW(), NOW(), false),
            (gen_random_uuid(), v_company_id, 'Fiber splicing (fusion)', 'FIBER_SPLICE_FUSION', 'FiberSkills', 'Fusion fiber splicing techniques', 4, true, NOW(), NOW(), false),
            (gen_random_uuid(), v_company_id, 'Fiber connector termination (SC/LC)', 'FIBER_CONNECTOR_TERMINATION', 'FiberSkills', 'Termination of SC/LC fiber connectors', 5, true, NOW(), NOW(), false),
            (gen_random_uuid(), v_company_id, 'OTDR testing', 'OTDR_TESTING', 'FiberSkills', 'Optical Time Domain Reflectometer testing', 6, true, NOW(), NOW(), false),
            (gen_random_uuid(), v_company_id, 'Optical power meter usage', 'OPTICAL_POWER_METER', 'FiberSkills', 'Using optical power meters for signal measurement', 7, true, NOW(), NOW(), false),
            (gen_random_uuid(), v_company_id, 'Visual fault locator (VFL)', 'VFL_USAGE', 'FiberSkills', 'Using Visual Fault Locator for fiber troubleshooting', 8, true, NOW(), NOW(), false),
            (gen_random_uuid(), v_company_id, 'Drop cable installation', 'DROP_CABLE_INSTALL', 'FiberSkills', 'Installation of drop cables from distribution point to customer premises', 9, true, NOW(), NOW(), false),
            (gen_random_uuid(), v_company_id, 'ONT installation and configuration', 'ONT_INSTALL_CONFIG', 'NetworkEquipment', 'Installation and configuration of Optical Network Terminals', 10, true, NOW(), NOW(), false),
            (gen_random_uuid(), v_company_id, 'Router setup and configuration', 'ROUTER_SETUP', 'NetworkEquipment', 'Setting up and configuring routers', 11, true, NOW(), NOW(), false),
            (gen_random_uuid(), v_company_id, 'Wi-Fi optimization', 'WIFI_OPTIMIZATION', 'NetworkEquipment', 'Optimizing Wi-Fi networks for performance', 12, true, NOW(), NOW(), false),
            (gen_random_uuid(), v_company_id, 'IPTV setup', 'IPTV_SETUP', 'NetworkEquipment', 'Setting up IPTV services', 13, true, NOW(), NOW(), false),
            (gen_random_uuid(), v_company_id, 'Mesh network installation', 'MESH_NETWORK', 'NetworkEquipment', 'Installation of mesh Wi-Fi networks', 14, true, NOW(), NOW(), false),
            (gen_random_uuid(), v_company_id, 'Basic network troubleshooting', 'NETWORK_TROUBLESHOOTING', 'NetworkEquipment', 'Basic troubleshooting of network issues', 15, true, NOW(), NOW(), false),
            (gen_random_uuid(), v_company_id, 'Speed test and verification', 'SPEED_TEST', 'NetworkEquipment', 'Performing speed tests and verifying service quality', 16, true, NOW(), NOW(), false),
            (gen_random_uuid(), v_company_id, 'Aerial installation (pole-to-building)', 'AERIAL_INSTALL', 'InstallationMethods', 'Aerial fiber installation from pole to building', 17, true, NOW(), NOW(), false),
            (gen_random_uuid(), v_company_id, 'Underground/conduit installation', 'UNDERGROUND_INSTALL', 'InstallationMethods', 'Underground and conduit-based fiber installation', 18, true, NOW(), NOW(), false),
            (gen_random_uuid(), v_company_id, 'Indoor cable routing', 'INDOOR_ROUTING', 'InstallationMethods', 'Routing fiber cables within buildings', 19, true, NOW(), NOW(), false),
            (gen_random_uuid(), v_company_id, 'Wall penetration and patching', 'WALL_PENETRATION', 'InstallationMethods', 'Penetrating walls and patching holes for cable routing', 20, true, NOW(), NOW(), false),
            (gen_random_uuid(), v_company_id, 'Cable management and labeling', 'CABLE_MANAGEMENT', 'InstallationMethods', 'Proper cable management and labeling practices', 21, true, NOW(), NOW(), false),
            (gen_random_uuid(), v_company_id, 'Weatherproofing', 'WEATHERPROOFING', 'InstallationMethods', 'Weatherproofing outdoor installations', 22, true, NOW(), NOW(), false),
            (gen_random_uuid(), v_company_id, 'Working at heights certified', 'HEIGHTS_CERTIFIED', 'SafetyCompliance', 'Certification for working at heights', 23, true, NOW(), NOW(), false),
            (gen_random_uuid(), v_company_id, 'Electrical safety awareness', 'ELECTRICAL_SAFETY', 'SafetyCompliance', 'Awareness of electrical safety procedures', 24, true, NOW(), NOW(), false),
            (gen_random_uuid(), v_company_id, 'TNB clearance procedures', 'TNB_CLEARANCE', 'SafetyCompliance', 'Understanding TNB (Tenaga Nasional Berhad) clearance procedures', 25, true, NOW(), NOW(), false),
            (gen_random_uuid(), v_company_id, 'Confined space entry', 'CONFINED_SPACE', 'SafetyCompliance', 'Certification for confined space entry', 26, true, NOW(), NOW(), false),
            (gen_random_uuid(), v_company_id, 'PPE usage', 'PPE_USAGE', 'SafetyCompliance', 'Proper use of Personal Protective Equipment', 27, true, NOW(), NOW(), false),
            (gen_random_uuid(), v_company_id, 'First Aid certified', 'FIRST_AID', 'SafetyCompliance', 'First Aid certification', 28, true, NOW(), NOW(), false),
            (gen_random_uuid(), v_company_id, 'Customer communication', 'CUSTOMER_COMMUNICATION', 'CustomerService', 'Effective communication with customers', 29, true, NOW(), NOW(), false),
            (gen_random_uuid(), v_company_id, 'Service demonstration', 'SERVICE_DEMO', 'CustomerService', 'Demonstrating services to customers', 30, true, NOW(), NOW(), false),
            (gen_random_uuid(), v_company_id, 'Technical explanation to customers', 'TECH_EXPLANATION', 'CustomerService', 'Explaining technical concepts to non-technical customers', 31, true, NOW(), NOW(), false),
            (gen_random_uuid(), v_company_id, 'Professional conduct', 'PROFESSIONAL_CONDUCT', 'CustomerService', 'Maintaining professional conduct during installations', 32, true, NOW(), NOW(), false),
            (gen_random_uuid(), v_company_id, 'Site cleanliness', 'SITE_CLEANLINESS', 'CustomerService', 'Maintaining cleanliness at installation sites', 33, true, NOW(), NOW(), false)
            ON CONFLICT DO NOTHING;
        END IF;
        
        -- Seed Order Types
        INSERT INTO "OrderTypes" ("Id", "CompanyId", "DepartmentId", "Name", "Code", "Description", "DisplayOrder", "IsActive", "CreatedAt", "UpdatedAt", "IsDeleted") VALUES
            (gen_random_uuid(), v_company_id, v_gpon_department_id, 'Activation', 'ACTIVATION', 'New installation + activation of service', 1, true, NOW(), NOW(), false),
            (gen_random_uuid(), v_company_id, v_gpon_department_id, 'Modification Indoor', 'MODIFICATION_INDOOR', 'Indoor modification of existing service', 2, true, NOW(), NOW(), false),
            (gen_random_uuid(), v_company_id, v_gpon_department_id, 'Modification Outdoor', 'MODIFICATION_OUTDOOR', 'Outdoor modification of existing service', 3, true, NOW(), NOW(), false),
            (gen_random_uuid(), v_company_id, v_gpon_department_id, 'Assurance', 'ASSURANCE', 'Fault repair and troubleshooting', 4, true, NOW(), NOW(), false),
            (gen_random_uuid(), v_company_id, v_gpon_department_id, 'Value Added Service', 'VALUE_ADDED_SERVICE', 'Additional services beyond standard installation/repair', 5, true, NOW(), NOW(), false)
        ON CONFLICT DO NOTHING;
        
        -- Seed Order Categories
        INSERT INTO "OrderCategories" ("Id", "CompanyId", "DepartmentId", "Name", "Code", "Description", "DisplayOrder", "IsActive", "CreatedAt", "UpdatedAt", "IsDeleted") VALUES
            (gen_random_uuid(), v_company_id, v_gpon_department_id, 'FTTH', 'FTTH', 'Fibre to the Home', 1, true, NOW(), NOW(), false),
            (gen_random_uuid(), v_company_id, v_gpon_department_id, 'FTTO', 'FTTO', 'Fibre to the Office', 2, true, NOW(), NOW(), false),
            (gen_random_uuid(), v_company_id, v_gpon_department_id, 'FTTR', 'FTTR', 'Fibre to the Room', 3, true, NOW(), NOW(), false),
            (gen_random_uuid(), v_company_id, v_gpon_department_id, 'FTTC', 'FTTC', 'Fibre to the Curb', 4, true, NOW(), NOW(), false)
        ON CONFLICT DO NOTHING;
        
        -- Seed Building Types (19 types)
        INSERT INTO "BuildingTypes" ("Id", "CompanyId", "DepartmentId", "Name", "Code", "Description", "DisplayOrder", "IsActive", "CreatedAt", "UpdatedAt", "IsDeleted") VALUES
            (gen_random_uuid(), v_company_id, v_gpon_department_id, 'Condominium', 'CONDO', 'High-rise residential building', 1, true, NOW(), NOW(), false),
            (gen_random_uuid(), v_company_id, v_gpon_department_id, 'Apartment', 'APARTMENT', 'Multi-unit residential building', 2, true, NOW(), NOW(), false),
            (gen_random_uuid(), v_company_id, v_gpon_department_id, 'Service Apartment', 'SERVICE_APT', 'Serviced residential units', 3, true, NOW(), NOW(), false),
            (gen_random_uuid(), v_company_id, v_gpon_department_id, 'Flat', 'FLAT', 'Low-rise residential units', 4, true, NOW(), NOW(), false),
            (gen_random_uuid(), v_company_id, v_gpon_department_id, 'Terrace House', 'TERRACE', 'Row houses', 5, true, NOW(), NOW(), false),
            (gen_random_uuid(), v_company_id, v_gpon_department_id, 'Semi-Detached', 'SEMI_DETACHED', 'Semi-detached houses', 6, true, NOW(), NOW(), false),
            (gen_random_uuid(), v_company_id, v_gpon_department_id, 'Bungalow', 'BUNGALOW', 'Single-story detached house', 7, true, NOW(), NOW(), false),
            (gen_random_uuid(), v_company_id, v_gpon_department_id, 'Townhouse', 'TOWNHOUSE', 'Multi-story attached houses', 8, true, NOW(), NOW(), false),
            (gen_random_uuid(), v_company_id, v_gpon_department_id, 'Office Tower', 'OFFICE_TOWER', 'High-rise office building', 10, true, NOW(), NOW(), false),
            (gen_random_uuid(), v_company_id, v_gpon_department_id, 'Office Building', 'OFFICE', 'Low to mid-rise office building', 11, true, NOW(), NOW(), false),
            (gen_random_uuid(), v_company_id, v_gpon_department_id, 'Shop Office', 'SHOP_OFFICE', 'Mixed shop and office building', 12, true, NOW(), NOW(), false),
            (gen_random_uuid(), v_company_id, v_gpon_department_id, 'Shopping Mall', 'MALL', 'Retail shopping complex', 13, true, NOW(), NOW(), false),
            (gen_random_uuid(), v_company_id, v_gpon_department_id, 'Hotel', 'HOTEL', 'Hotel or resort building', 14, true, NOW(), NOW(), false),
            (gen_random_uuid(), v_company_id, v_gpon_department_id, 'Mixed Development', 'MIXED', 'Mixed residential and commercial', 20, true, NOW(), NOW(), false),
            (gen_random_uuid(), v_company_id, v_gpon_department_id, 'Industrial', 'INDUSTRIAL', 'Industrial or warehouse building', 30, true, NOW(), NOW(), false),
            (gen_random_uuid(), v_company_id, v_gpon_department_id, 'Warehouse', 'WAREHOUSE', 'Storage or warehouse facility', 31, true, NOW(), NOW(), false),
            (gen_random_uuid(), v_company_id, v_gpon_department_id, 'Educational', 'EDUCATIONAL', 'School or educational institution', 32, true, NOW(), NOW(), false),
            (gen_random_uuid(), v_company_id, v_gpon_department_id, 'Government', 'GOVERNMENT', 'Government building', 33, true, NOW(), NOW(), false),
            (gen_random_uuid(), v_company_id, v_gpon_department_id, 'Other', 'OTHER', 'Other building type', 99, true, NOW(), NOW(), false)
        ON CONFLICT DO NOTHING;
        
        -- Seed Splitter Types
        INSERT INTO "SplitterTypes" ("Id", "CompanyId", "DepartmentId", "Name", "Code", "TotalPorts", "StandbyPortNumber", "Description", "DisplayOrder", "IsActive", "CreatedAt", "UpdatedAt", "IsDeleted") VALUES
            (gen_random_uuid(), v_company_id, v_gpon_department_id, '1:8', '1_8', 8, NULL, '1:8 Splitter (8 ports)', 1, true, NOW(), NOW(), false),
            (gen_random_uuid(), v_company_id, v_gpon_department_id, '1:12', '1_12', 12, NULL, '1:12 Splitter (12 ports)', 2, true, NOW(), NOW(), false),
            (gen_random_uuid(), v_company_id, v_gpon_department_id, '1:32', '1_32', 32, 32, '1:32 Splitter (32 ports, port 32 is standby)', 3, true, NOW(), NOW(), false)
        ON CONFLICT DO NOTHING;
        
        -- Seed Parser Templates (14 templates)
        -- Note: CreatedByUserId is required (NOT NULL), using admin user ID
        INSERT INTO "ParserTemplates" ("Id", "CompanyId", "Name", "Code", "PartnerPattern", "SubjectPattern", "OrderTypeCode", "Priority", "IsActive", "AutoApprove", "Description", "CreatedByUserId", "CreatedAt", "IsDeleted") VALUES
            (gen_random_uuid(), v_company_id, 'TIME Activation', 'TIME_ACTIVATION', '*@time.com.my', '*Activation*', 'ACTIVATION', 100, true, false, 'Parses TIME FTTH/HSBB activation work orders', v_admin_user_id, NOW(), false),
            (gen_random_uuid(), v_company_id, 'TIME Modification (Indoor)', 'TIME_MOD_INDOOR', '*@time.com.my', '*Modification*Indoor*', 'MODIFICATION_INDOOR', 95, true, false, 'Parses TIME indoor modification work orders', v_admin_user_id, NOW(), false),
            (gen_random_uuid(), v_company_id, 'TIME Modification (Outdoor)', 'TIME_MOD_OUTDOOR', '*@time.com.my', '*Modification*Outdoor*', 'MODIFICATION_OUTDOOR', 95, true, false, 'Parses TIME outdoor modification work orders', v_admin_user_id, NOW(), false),
            (gen_random_uuid(), v_company_id, 'TIME Modification (General)', 'TIME_MODIFICATION', '*@time.com.my', '*Modification*', 'MODIFICATION', 90, true, false, 'Parses TIME general modification work orders', v_admin_user_id, NOW(), false),
            (gen_random_uuid(), v_company_id, 'TIME Termination', 'TIME_TERMINATION', '*@time.com.my', '*Termination*', 'TERMINATION', 80, true, false, 'Parses TIME termination/cancellation work orders', v_admin_user_id, NOW(), false),
            (gen_random_uuid(), v_company_id, 'TIME Relocation', 'TIME_RELOCATION', '*@time.com.my', '*Relocation*', 'RELOCATION', 85, true, false, 'Parses TIME relocation work orders', v_admin_user_id, NOW(), false),
            (gen_random_uuid(), v_company_id, 'TIME Assurance', 'TIME_ASSURANCE', '*@time.com.my', '*Assurance*', 'ASSURANCE', 70, true, false, 'Parses TIME assurance/troubleshooting work orders', v_admin_user_id, NOW(), false),
            (gen_random_uuid(), v_company_id, 'TIME General (Fallback)', 'TIME_GENERAL', '*@time.com.my', '*Work Order*', 'GENERAL', 10, true, false, 'Fallback template for TIME work orders that don''t match other patterns', v_admin_user_id, NOW(), false),
            (gen_random_uuid(), v_company_id, 'Celcom HSBB', 'CELCOM_HSBB', '*celcom*', '*HSBB*', 'ACTIVATION', 100, true, false, 'Parses Celcom HSBB work orders via TIME', v_admin_user_id, NOW(), false),
            (gen_random_uuid(), v_company_id, 'TIME Payment Advice', 'TIME_PAYMENT_ADVICE', '*@time.com.my', '*Payment Advice*|*Payment*', NULL, 11, true, false, 'Parses payment advice emails from TIME', v_admin_user_id, NOW(), false),
            (gen_random_uuid(), v_company_id, 'TIME Reschedule Notification', 'TIME_RESCHEDULE', '*@time.com.my', '*Reschedule*|*Rescheduled*', NULL, 12, true, false, 'Parses reschedule notification emails from TIME', v_admin_user_id, NOW(), false),
            (gen_random_uuid(), v_company_id, 'TIME Customer Uncontactable', 'TIME_CUSTOMER_UNCONTACTABLE', '*@time.com.my', '*Customer Uncontactable*|*Uncontactable*', NULL, 13, true, false, 'Parses customer uncontactable notification emails from TIME', v_admin_user_id, NOW(), false),
            (gen_random_uuid(), v_company_id, 'TIME RFB Meeting Notification', 'TIME_RFB', '*@time.com.my', '*RFB MEETING*|*RFB Meeting*|*Request for Building*', NULL, 14, true, false, 'Parses RFB meeting notification emails from TIME. Extracts building information, meeting details, and BM contact information.', v_admin_user_id, NOW(), false),
            (gen_random_uuid(), v_company_id, 'TIME Withdrawal Notification', 'TIME_WITHDRAWAL', '*@time.com.my', '*Withdraw*|*Withdrawn*|*Confirm Withdraw*', NULL, 15, true, false, 'Parses withdrawal notification emails from TIME. Extracts Service ID and updates order status to Cancelled.', v_admin_user_id, NOW(), false)
        ON CONFLICT DO NOTHING;
        
        -- Seed Guard Condition Definitions (10 conditions)
        -- Note: This table uses snake_case column names, but Id is PascalCase
        IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'guard_condition_definitions') THEN
            INSERT INTO "guard_condition_definitions" ("Id", "company_id", "key", "name", "description", "entity_type", "validator_type", "validator_config_json", "is_active", "display_order", "created_at", "updated_at", "is_deleted") VALUES
            (gen_random_uuid(), v_company_id, 'photosRequired', 'Photos Required', 'Checks if photos are uploaded for the order', 'Order', 'PhotosRequiredValidator', '{"checkFlag": true, "checkFiles": true}', true, 1, NOW(), NOW(), false),
            (gen_random_uuid(), v_company_id, 'docketUploaded', 'Docket Uploaded', 'Checks if docket is uploaded for the order', 'Order', 'DocketUploadedValidator', '{"checkFlag": true, "checkDockets": true}', true, 2, NOW(), NOW(), false),
            (gen_random_uuid(), v_company_id, 'splitterAssigned', 'Splitter Assigned', 'Checks if splitter port is assigned to the order', 'Order', 'SplitterAssignedValidator', NULL, true, 3, NOW(), NOW(), false),
            (gen_random_uuid(), v_company_id, 'serialNumbersValidated', 'Serial Numbers Validated', 'Checks if serial numbers are validated for the order', 'Order', 'SerialsValidatedValidator', NULL, true, 4, NOW(), NOW(), false),
            (gen_random_uuid(), v_company_id, 'materialsSpecified', 'Materials Specified', 'Checks if materials are specified for the order', 'Order', 'MaterialsSpecifiedValidator', NULL, true, 5, NOW(), NOW(), false),
            (gen_random_uuid(), v_company_id, 'siaAssigned', 'SI Assigned', 'Checks if Service Installer (SI) is assigned to the order', 'Order', 'SiAssignedValidator', NULL, true, 6, NOW(), NOW(), false),
            (gen_random_uuid(), v_company_id, 'appointmentDateSet', 'Appointment Date Set', 'Checks if appointment date is set for the order', 'Order', 'AppointmentDateSetValidator', NULL, true, 7, NOW(), NOW(), false),
            (gen_random_uuid(), v_company_id, 'buildingSelected', 'Building Selected', 'Checks if building is selected for the order', 'Order', 'BuildingSelectedValidator', NULL, true, 8, NOW(), NOW(), false),
            (gen_random_uuid(), v_company_id, 'customerContactProvided', 'Customer Contact Provided', 'Checks if customer contact (phone or email) is provided for the order', 'Order', 'CustomerContactProvidedValidator', NULL, true, 9, NOW(), NOW(), false),
            (gen_random_uuid(), v_company_id, 'noBlockersActive', 'No Active Blockers', 'Checks if there are no active blockers for the order', 'Order', 'NoActiveBlockersValidator', NULL, true, 10, NOW(), NOW(), false)
            ON CONFLICT DO NOTHING;
        END IF;
        
        -- Seed Side Effect Definitions (5 side effects)
        -- Note: This table uses snake_case column names
        IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'side_effect_definitions') THEN
            INSERT INTO "side_effect_definitions" ("Id", "company_id", "key", "name", "description", "entity_type", "executor_type", "executor_config_json", "is_active", "display_order", "created_at", "updated_at", "is_deleted") VALUES
            (gen_random_uuid(), v_company_id, 'notify', 'Send Notification', 'Sends a notification to relevant users when workflow transition occurs', 'Order', 'NotifySideEffectExecutor', '{"template": "OrderStatusChange"}', true, 1, NOW(), NOW(), false),
            (gen_random_uuid(), v_company_id, 'createStockMovement', 'Create Stock Movement', 'Creates stock movement records when workflow transition occurs', 'Order', 'CreateStockMovementSideEffectExecutor', NULL, true, 2, NOW(), NOW(), false),
            (gen_random_uuid(), v_company_id, 'createOrderStatusLog', 'Create Order Status Log', 'Creates an order status log entry when workflow transition occurs', 'Order', 'CreateOrderStatusLogSideEffectExecutor', NULL, true, 3, NOW(), NOW(), false),
            (gen_random_uuid(), v_company_id, 'updateOrderFlags', 'Update Order Flags', 'Updates order flags (DocketUploaded, PhotosUploaded, etc.) when workflow transition occurs', 'Order', 'UpdateOrderFlagsSideEffectExecutor', NULL, true, 4, NOW(), NOW(), false),
            (gen_random_uuid(), v_company_id, 'triggerInvoiceEligibility', 'Trigger Invoice Eligibility', 'Checks and updates invoice eligibility flag when workflow transition occurs', 'Order', 'TriggerInvoiceEligibilitySideEffectExecutor', '{"requireDocket": true, "requirePhotos": true, "requireSerials": true}', true, 5, NOW(), NOW(), false)
            ON CONFLICT DO NOTHING;
        END IF;
        
        -- Seed Global Settings (~30+ settings)
        -- Note: GlobalSettings table does NOT have IsDeleted column
        INSERT INTO "GlobalSettings" ("Id", "Key", "Value", "ValueType", "Description", "Module", "CreatedAt", "UpdatedAt") VALUES
            (gen_random_uuid(), 'SMS_Enabled', 'false', 'Bool', 'Enable SMS notifications', 'Notifications', NOW(), NOW()),
            (gen_random_uuid(), 'SMS_Provider', 'None', 'String', 'SMS provider (Twilio, SMS_Gateway, None)', 'Notifications', NOW(), NOW()),
            (gen_random_uuid(), 'SMS_Twilio_AccountSid', '', 'String', 'Twilio Account SID (encrypted)', 'Notifications', NOW(), NOW()),
            (gen_random_uuid(), 'SMS_Twilio_AuthToken', '', 'String', 'Twilio Auth Token (encrypted)', 'Notifications', NOW(), NOW()),
            (gen_random_uuid(), 'SMS_Twilio_FromNumber', '', 'String', 'Twilio From Phone Number', 'Notifications', NOW(), NOW()),
            (gen_random_uuid(), 'SMS_AutoSendOnStatusChange', 'false', 'Bool', 'Automatically send SMS when order status changes', 'Notifications', NOW(), NOW()),
            (gen_random_uuid(), 'SMS_RetryAttempts', '3', 'Int', 'Number of retry attempts for failed SMS', 'Notifications', NOW(), NOW()),
            (gen_random_uuid(), 'SMS_RetryDelaySeconds', '5', 'Int', 'Delay between SMS retry attempts (seconds)', 'Notifications', NOW(), NOW()),
            (gen_random_uuid(), 'WhatsApp_Enabled', 'false', 'Bool', 'Enable WhatsApp notifications', 'Notifications', NOW(), NOW()),
            (gen_random_uuid(), 'WhatsApp_Provider', 'None', 'String', 'WhatsApp provider (Twilio, None)', 'Notifications', NOW(), NOW()),
            (gen_random_uuid(), 'WhatsApp_Twilio_AccountSid', '', 'String', 'Twilio Account SID for WhatsApp (encrypted)', 'Notifications', NOW(), NOW()),
            (gen_random_uuid(), 'WhatsApp_Twilio_AuthToken', '', 'String', 'Twilio Auth Token for WhatsApp (encrypted)', 'Notifications', NOW(), NOW()),
            (gen_random_uuid(), 'WhatsApp_Twilio_FromNumber', '', 'String', 'Twilio WhatsApp From Number', 'Notifications', NOW(), NOW()),
            (gen_random_uuid(), 'WhatsApp_AutoSendOnStatusChange', 'false', 'Bool', 'Automatically send WhatsApp when order status changes', 'Notifications', NOW(), NOW()),
            (gen_random_uuid(), 'WhatsApp_RetryAttempts', '3', 'Int', 'Number of retry attempts for failed WhatsApp', 'Notifications', NOW(), NOW()),
            (gen_random_uuid(), 'WhatsApp_RetryDelaySeconds', '5', 'Int', 'Delay between WhatsApp retry attempts (seconds)', 'Notifications', NOW(), NOW()),
            (gen_random_uuid(), 'EInvoice_Enabled', 'false', 'Bool', 'Enable e-invoice submission (MyInvois)', 'Billing', NOW(), NOW()),
            (gen_random_uuid(), 'EInvoice_Provider', 'Null', 'String', 'E-invoice provider (MyInvois, Null)', 'Billing', NOW(), NOW()),
            (gen_random_uuid(), 'MyInvois_BaseUrl', 'https://api-sandbox.myinvois.hasil.gov.my', 'String', 'MyInvois API base URL', 'Billing', NOW(), NOW()),
            (gen_random_uuid(), 'MyInvois_ClientId', '', 'String', 'MyInvois Client ID (encrypted)', 'Billing', NOW(), NOW()),
            (gen_random_uuid(), 'MyInvois_ClientSecret', '', 'String', 'MyInvois Client Secret (encrypted)', 'Billing', NOW(), NOW()),
            (gen_random_uuid(), 'MyInvois_Enabled', 'false', 'Bool', 'Enable MyInvois integration', 'Billing', NOW(), NOW()),
            (gen_random_uuid(), 'Notification_Assigned_SmsTemplateCode', 'ASSIGNED', 'String', 'SMS template code for Assigned status', 'Notifications', NOW(), NOW()),
            (gen_random_uuid(), 'Notification_Assigned_WhatsAppTemplateCode', 'ASSIGNED', 'String', 'WhatsApp template code for Assigned status', 'Notifications', NOW(), NOW()),
            (gen_random_uuid(), 'Notification_OnTheWay_SmsTemplateCode', 'OTW', 'String', 'SMS template code for OnTheWay status', 'Notifications', NOW(), NOW()),
            (gen_random_uuid(), 'Notification_OnTheWay_WhatsAppTemplateCode', 'OTW', 'String', 'WhatsApp template code for OnTheWay status', 'Notifications', NOW(), NOW()),
            (gen_random_uuid(), 'Notification_MetCustomer_SmsTemplateCode', 'MET_CUSTOMER', 'String', 'SMS template code for MetCustomer status', 'Notifications', NOW(), NOW()),
            (gen_random_uuid(), 'Notification_MetCustomer_WhatsAppTemplateCode', 'MET_CUSTOMER', 'String', 'WhatsApp template code for MetCustomer status', 'Notifications', NOW(), NOW()),
            (gen_random_uuid(), 'Notification_OrderCompleted_SmsTemplateCode', 'IN_PROGRESS', 'String', 'SMS template code for OrderCompleted status', 'Notifications', NOW(), NOW()),
            (gen_random_uuid(), 'Notification_OrderCompleted_WhatsAppTemplateCode', 'IN_PROGRESS', 'String', 'WhatsApp template code for OrderCompleted status', 'Notifications', NOW(), NOW()),
            (gen_random_uuid(), 'Notification_Completed_SmsTemplateCode', 'COMPLETED', 'String', 'SMS template code for Completed status', 'Notifications', NOW(), NOW()),
            (gen_random_uuid(), 'Notification_Completed_WhatsAppTemplateCode', 'COMPLETED', 'String', 'WhatsApp template code for Completed status', 'Notifications', NOW(), NOW()),
            (gen_random_uuid(), 'Notification_Cancelled_SmsTemplateCode', 'CANCELLED', 'String', 'SMS template code for Cancelled status', 'Notifications', NOW(), NOW()),
            (gen_random_uuid(), 'Notification_Cancelled_WhatsAppTemplateCode', 'CANCELLED', 'String', 'WhatsApp template code for Cancelled status', 'Notifications', NOW(), NOW()),
            (gen_random_uuid(), 'Notification_ReschedulePendingApproval_SmsTemplateCode', 'RESCHEDULED', 'String', 'SMS template code for ReschedulePendingApproval status', 'Notifications', NOW(), NOW()),
            (gen_random_uuid(), 'Notification_ReschedulePendingApproval_WhatsAppTemplateCode', 'RESCHEDULED', 'String', 'WhatsApp template code for ReschedulePendingApproval status', 'Notifications', NOW(), NOW()),
            (gen_random_uuid(), 'Notification_Blocker_SmsTemplateCode', 'BLOCKER', 'String', 'SMS template code for Blocker status', 'Notifications', NOW(), NOW()),
            (gen_random_uuid(), 'Notification_Blocker_WhatsAppTemplateCode', 'BLOCKER', 'String', 'WhatsApp template code for Blocker status', 'Notifications', NOW(), NOW()),
            (gen_random_uuid(), 'Messaging_SendSmsFallback', 'true', 'Bool', 'Send SMS alongside WhatsApp for non-urgent messages (optional fallback)', 'Notifications', NOW(), NOW()),
            (gen_random_uuid(), 'Messaging_AutoDetectWhatsApp', 'true', 'Bool', 'Automatically detect if customer uses WhatsApp by attempting to send', 'Notifications', NOW(), NOW()),
            (gen_random_uuid(), 'Messaging_WhatsAppRetryOnFailure', 'true', 'Bool', 'Retry with SMS if WhatsApp fails', 'Notifications', NOW(), NOW())
        ON CONFLICT DO NOTHING;
        
        -- Seed Movement Types (11 types)
        INSERT INTO "MovementTypes" ("Id", "CompanyId", "Code", "Name", "Description", "Direction", "RequiresFromLocation", "RequiresToLocation", "RequiresOrderId", "RequiresServiceInstallerId", "RequiresPartnerId", "AffectsStockBalance", "StockImpact", "IsActive", "SortOrder", "CreatedAt", "UpdatedAt", "IsDeleted") VALUES
            (gen_random_uuid(), v_company_id, 'GRN', 'Goods Receipt Note', 'Receipt of materials from supplier', 'In', false, true, false, false, false, true, 'Positive', true, 1, NOW(), NOW(), false),
            (gen_random_uuid(), v_company_id, 'ReturnFromSI', 'Return from Service Installer', 'Materials returned from service installer to warehouse', 'In', false, true, false, true, false, true, 'Positive', true, 2, NOW(), NOW(), false),
            (gen_random_uuid(), v_company_id, 'ReturnFromCustomer', 'Return from Customer', 'Materials returned from customer site', 'In', false, true, true, false, false, true, 'Positive', true, 3, NOW(), NOW(), false),
            (gen_random_uuid(), v_company_id, 'IssueToSI', 'Issue to Service Installer', 'Materials issued to service installer for installation', 'Out', true, false, false, true, false, true, 'Negative', true, 4, NOW(), NOW(), false),
            (gen_random_uuid(), v_company_id, 'IssueToOrder', 'Issue to Order', 'Materials issued directly to order/customer site', 'Out', true, false, true, false, false, true, 'Negative', true, 5, NOW(), NOW(), false),
            (gen_random_uuid(), v_company_id, 'ReturnFaulty', 'Return Faulty', 'Faulty materials returned to warehouse/RMA', 'In', false, true, true, true, false, true, 'Positive', true, 6, NOW(), NOW(), false),
            (gen_random_uuid(), v_company_id, 'Transfer', 'Transfer', 'Transfer materials between locations', 'Transfer', true, true, false, false, false, true, 'Neutral', true, 7, NOW(), NOW(), false),
            (gen_random_uuid(), v_company_id, 'TransferToRMA', 'Transfer to RMA', 'Transfer faulty materials to RMA location', 'Transfer', true, true, false, false, false, true, 'Neutral', true, 8, NOW(), NOW(), false),
            (gen_random_uuid(), v_company_id, 'Adjustment', 'Stock Adjustment', 'Stock count adjustment (increase or decrease)', 'Adjust', false, true, false, false, false, true, 'Positive', true, 9, NOW(), NOW(), false),
            (gen_random_uuid(), v_company_id, 'AdjustmentDown', 'Stock Adjustment (Decrease)', 'Stock count adjustment (decrease)', 'Adjust', true, false, false, false, false, true, 'Negative', true, 10, NOW(), NOW(), false),
            (gen_random_uuid(), v_company_id, 'WriteOff', 'Write Off', 'Materials written off (damaged, expired, etc.)', 'Out', true, false, false, false, false, true, 'Negative', true, 11, NOW(), NOW(), false)
        ON CONFLICT DO NOTHING;
        
        -- Seed Location Types (6 types)
        INSERT INTO "LocationTypes" ("Id", "CompanyId", "Code", "Name", "Description", "RequiresServiceInstallerId", "RequiresBuildingId", "RequiresWarehouseId", "AutoCreate", "AutoCreateTrigger", "IsActive", "SortOrder", "CreatedAt", "UpdatedAt", "IsDeleted") VALUES
            (gen_random_uuid(), v_company_id, 'Warehouse', 'Warehouse', 'Main warehouse location', false, false, false, true, 'WarehouseCreated', true, 1, NOW(), NOW(), false),
            (gen_random_uuid(), v_company_id, 'SI', 'Service Installer', 'Service installer stock location', true, false, false, true, 'ServiceInstallerCreated', true, 2, NOW(), NOW(), false),
            (gen_random_uuid(), v_company_id, 'CustomerSite', 'Customer Site', 'Customer installation site', false, true, false, true, 'BuildingCreated', true, 3, NOW(), NOW(), false),
            (gen_random_uuid(), v_company_id, 'RMA', 'RMA Location', 'Return Merchandise Authorization location', false, false, false, false, NULL, true, 4, NOW(), NOW(), false),
            (gen_random_uuid(), v_company_id, 'Transit', 'Transit', 'Materials in transit', false, false, false, false, NULL, true, 5, NOW(), NOW(), false),
            (gen_random_uuid(), v_company_id, 'Supplier', 'Supplier', 'Supplier location (for tracking)', false, false, false, false, NULL, true, 6, NOW(), NOW(), false)
        ON CONFLICT DO NOTHING;
        
        -- Seed Default Material Categories (if none exist)
        IF NOT EXISTS (SELECT 1 FROM "MaterialCategories") THEN
            INSERT INTO "MaterialCategories" ("Id", "CompanyId", "Name", "Description", "DisplayOrder", "IsActive", "CreatedAt", "UpdatedAt", "IsDeleted") VALUES
                (gen_random_uuid(), v_company_id, 'ONU', 'Optical Network Units - Customer premises equipment', 1, true, NOW(), NOW(), false),
                (gen_random_uuid(), v_company_id, 'Fiber Cable', 'Fiber optic cables (indoor, outdoor, aerial)', 2, true, NOW(), NOW(), false),
                (gen_random_uuid(), v_company_id, 'Splitter', 'Optical splitters for fiber distribution', 3, true, NOW(), NOW(), false),
                (gen_random_uuid(), v_company_id, 'Accessories', 'Termination boxes, connectors, adapters', 4, true, NOW(), NOW(), false),
                (gen_random_uuid(), v_company_id, 'Distribution', 'Distribution units, cabinets, enclosures', 5, true, NOW(), NOW(), false),
                (gen_random_uuid(), v_company_id, 'Tools', 'Installation tools and equipment', 6, true, NOW(), NOW(), false),
                (gen_random_uuid(), v_company_id, 'Consumables', 'Consumable items (cable ties, labels, etc.)', 7, true, NOW(), NOW(), false),
                (gen_random_uuid(), v_company_id, 'Spare Parts', 'Spare parts and replacement components', 8, true, NOW(), NOW(), false)
            ON CONFLICT DO NOTHING;
        END IF;
        
    EXCEPTION
        WHEN OTHERS THEN
            RAISE EXCEPTION 'Error seeding database at line %: % (SQLSTATE: %)', SQLSTATE, SQLERRM, SQLSTATE;
    END $$;
                
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260106014834_SeedAllReferenceData') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260106014834_SeedAllReferenceData', '10.0.3');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260106072239_AddDepartmentIdToSkills') THEN
    DROP INDEX "IX_Skills_CompanyId_Category_IsActive";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260106072239_AddDepartmentIdToSkills') THEN
    DROP INDEX "IX_Skills_CompanyId_Code";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260106072239_AddDepartmentIdToSkills') THEN
    ALTER TABLE "Skills" ADD "DepartmentId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260106072239_AddDepartmentIdToSkills') THEN
    CREATE INDEX "IX_Skills_CompanyId_DepartmentId_Category_IsActive" ON "Skills" ("CompanyId", "DepartmentId", "Category", "IsActive");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260106072239_AddDepartmentIdToSkills') THEN
    CREATE UNIQUE INDEX "IX_Skills_CompanyId_DepartmentId_Code" ON "Skills" ("CompanyId", "DepartmentId", "Code") WHERE "IsDeleted" = false;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260106072239_AddDepartmentIdToSkills') THEN
    CREATE INDEX "IX_Skills_DepartmentId" ON "Skills" ("DepartmentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260106072239_AddDepartmentIdToSkills') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260106072239_AddDepartmentIdToSkills', '10.0.3');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260127073842_CapturePendingModelChanges_20260127') THEN
    ALTER TABLE "KpiProfiles" ADD "InstallationMethodId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260127073842_CapturePendingModelChanges_20260127') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260127073842_CapturePendingModelChanges_20260127', '10.0.3');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260127143150_AddDocumentTemplateTagsDescription') THEN
    ALTER TABLE "DocumentTemplates" ADD "Description" character varying(1000);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260127143150_AddDocumentTemplateTagsDescription') THEN
    ALTER TABLE "DocumentTemplates" ADD "Tags" character varying(1000);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260127143150_AddDocumentTemplateTagsDescription') THEN

    DO $$
    DECLARE
        rec RECORD;
        metadata jsonb;
        tags_list text;
    BEGIN
        FOR rec IN
            SELECT "Id", "JsonSchema"
            FROM "DocumentTemplates"
            WHERE "JsonSchema" IS NOT NULL
        LOOP
            BEGIN
                metadata := NULL;
                IF (rec."JsonSchema"::jsonb ? 'metadata') THEN
                    metadata := rec."JsonSchema"::jsonb -> 'metadata';
                ELSIF (rec."JsonSchema"::jsonb ? 'tags') OR (rec."JsonSchema"::jsonb ? 'description') THEN
                    metadata := rec."JsonSchema"::jsonb;
                END IF;

                IF metadata IS NOT NULL THEN
                    IF (metadata ? 'tags') THEN
                        SELECT string_agg(value, ',')
                        INTO tags_list
                        FROM jsonb_array_elements_text(metadata -> 'tags');
                    ELSE
                        tags_list := NULL;
                    END IF;

                    UPDATE "DocumentTemplates"
                    SET
                        "Description" = COALESCE("Description", metadata ->> 'description'),
                        "Tags" = COALESCE("Tags", tags_list)
                    WHERE "Id" = rec."Id";
                END IF;
            EXCEPTION WHEN others THEN
                -- Skip rows with invalid JSON
                CONTINUE;
            END;
        END LOOP;
    END $$;

    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260127143150_AddDocumentTemplateTagsDescription') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260127143150_AddDocumentTemplateTagsDescription', '10.0.3');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260203043142_AddAuditLogTable') THEN
    CREATE TABLE "AuditLogs" (
        "Id" uuid NOT NULL,
        "Timestamp" timestamp with time zone NOT NULL,
        "CompanyId" uuid,
        "UserId" uuid,
        "EntityType" character varying(100) NOT NULL,
        "EntityId" uuid NOT NULL,
        "Action" character varying(50) NOT NULL,
        "FieldChangesJson" jsonb,
        "Channel" character varying(50) NOT NULL,
        "IpAddress" character varying(45),
        "MetadataJson" jsonb,
        CONSTRAINT "PK_AuditLogs" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260203043142_AddAuditLogTable') THEN
    CREATE INDEX "IX_AuditLogs_CompanyId_EntityType_EntityId" ON "AuditLogs" ("CompanyId", "EntityType", "EntityId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260203043142_AddAuditLogTable') THEN
    CREATE INDEX "IX_AuditLogs_CompanyId_Timestamp" ON "AuditLogs" ("CompanyId", "Timestamp");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260203043142_AddAuditLogTable') THEN
    CREATE INDEX "IX_AuditLogs_UserId_Timestamp" ON "AuditLogs" ("UserId", "Timestamp");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260203043142_AddAuditLogTable') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260203043142_AddAuditLogTable', '10.0.3');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260203063309_AddStockLedgerAndAllocation') THEN
    CREATE TABLE "StockAllocations" (
        "Id" uuid NOT NULL,
        "MaterialId" uuid NOT NULL,
        "SerialisedItemId" uuid,
        "LocationId" uuid NOT NULL,
        "Quantity" numeric NOT NULL,
        "OrderId" uuid NOT NULL,
        "Status" integer NOT NULL,
        "LedgerEntryIdReserved" uuid,
        "LedgerEntryIdIssued" uuid,
        "LedgerEntryIdReturned" uuid,
        "CreatedByUserId" uuid NOT NULL,
        "CompanyId" uuid,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "IsDeleted" boolean NOT NULL,
        "DeletedAt" timestamp with time zone,
        "DeletedByUserId" uuid,
        "RowVersion" bytea,
        CONSTRAINT "PK_StockAllocations" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_StockAllocations_Materials_MaterialId" FOREIGN KEY ("MaterialId") REFERENCES "Materials" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_StockAllocations_SerialisedItems_SerialisedItemId" FOREIGN KEY ("SerialisedItemId") REFERENCES "SerialisedItems" ("Id"),
        CONSTRAINT "FK_StockAllocations_StockLocations_LocationId" FOREIGN KEY ("LocationId") REFERENCES "StockLocations" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260203063309_AddStockLedgerAndAllocation') THEN
    CREATE TABLE "StockLedgerEntries" (
        "Id" uuid NOT NULL,
        "EntryType" integer NOT NULL,
        "MaterialId" uuid NOT NULL,
        "LocationId" uuid NOT NULL,
        "Quantity" numeric NOT NULL,
        "FromLocationId" uuid,
        "ToLocationId" uuid,
        "OrderId" uuid,
        "SerialisedItemId" uuid,
        "AllocationId" uuid,
        "ReferenceType" text,
        "ReferenceId" text,
        "CreatedByUserId" uuid NOT NULL,
        "Remarks" text,
        "CompanyId" uuid,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "IsDeleted" boolean NOT NULL,
        "DeletedAt" timestamp with time zone,
        "DeletedByUserId" uuid,
        "RowVersion" bytea,
        CONSTRAINT "PK_StockLedgerEntries" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_StockLedgerEntries_Materials_MaterialId" FOREIGN KEY ("MaterialId") REFERENCES "Materials" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_StockLedgerEntries_SerialisedItems_SerialisedItemId" FOREIGN KEY ("SerialisedItemId") REFERENCES "SerialisedItems" ("Id"),
        CONSTRAINT "FK_StockLedgerEntries_StockAllocations_AllocationId" FOREIGN KEY ("AllocationId") REFERENCES "StockAllocations" ("Id"),
        CONSTRAINT "FK_StockLedgerEntries_StockLocations_FromLocationId" FOREIGN KEY ("FromLocationId") REFERENCES "StockLocations" ("Id"),
        CONSTRAINT "FK_StockLedgerEntries_StockLocations_LocationId" FOREIGN KEY ("LocationId") REFERENCES "StockLocations" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_StockLedgerEntries_StockLocations_ToLocationId" FOREIGN KEY ("ToLocationId") REFERENCES "StockLocations" ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260203063309_AddStockLedgerAndAllocation') THEN
    CREATE INDEX "IX_StockAllocations_LedgerEntryIdIssued" ON "StockAllocations" ("LedgerEntryIdIssued");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260203063309_AddStockLedgerAndAllocation') THEN
    CREATE INDEX "IX_StockAllocations_LedgerEntryIdReserved" ON "StockAllocations" ("LedgerEntryIdReserved");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260203063309_AddStockLedgerAndAllocation') THEN
    CREATE INDEX "IX_StockAllocations_LedgerEntryIdReturned" ON "StockAllocations" ("LedgerEntryIdReturned");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260203063309_AddStockLedgerAndAllocation') THEN
    CREATE INDEX "IX_StockAllocations_LocationId" ON "StockAllocations" ("LocationId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260203063309_AddStockLedgerAndAllocation') THEN
    CREATE INDEX "IX_StockAllocations_MaterialId" ON "StockAllocations" ("MaterialId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260203063309_AddStockLedgerAndAllocation') THEN
    CREATE INDEX "IX_StockAllocations_SerialisedItemId" ON "StockAllocations" ("SerialisedItemId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260203063309_AddStockLedgerAndAllocation') THEN
    CREATE INDEX "IX_StockLedgerEntries_AllocationId" ON "StockLedgerEntries" ("AllocationId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260203063309_AddStockLedgerAndAllocation') THEN
    CREATE INDEX "IX_StockLedgerEntries_FromLocationId" ON "StockLedgerEntries" ("FromLocationId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260203063309_AddStockLedgerAndAllocation') THEN
    CREATE INDEX "IX_StockLedgerEntries_LocationId" ON "StockLedgerEntries" ("LocationId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260203063309_AddStockLedgerAndAllocation') THEN
    CREATE INDEX "IX_StockLedgerEntries_MaterialId" ON "StockLedgerEntries" ("MaterialId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260203063309_AddStockLedgerAndAllocation') THEN
    CREATE INDEX "IX_StockLedgerEntries_SerialisedItemId" ON "StockLedgerEntries" ("SerialisedItemId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260203063309_AddStockLedgerAndAllocation') THEN
    CREATE INDEX "IX_StockLedgerEntries_ToLocationId" ON "StockLedgerEntries" ("ToLocationId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260203063309_AddStockLedgerAndAllocation') THEN
    ALTER TABLE "StockAllocations" ADD CONSTRAINT "FK_StockAllocations_StockLedgerEntries_LedgerEntryIdIssued" FOREIGN KEY ("LedgerEntryIdIssued") REFERENCES "StockLedgerEntries" ("Id");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260203063309_AddStockLedgerAndAllocation') THEN
    ALTER TABLE "StockAllocations" ADD CONSTRAINT "FK_StockAllocations_StockLedgerEntries_LedgerEntryIdReserved" FOREIGN KEY ("LedgerEntryIdReserved") REFERENCES "StockLedgerEntries" ("Id");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260203063309_AddStockLedgerAndAllocation') THEN
    ALTER TABLE "StockAllocations" ADD CONSTRAINT "FK_StockAllocations_StockLedgerEntries_LedgerEntryIdReturned" FOREIGN KEY ("LedgerEntryIdReturned") REFERENCES "StockLedgerEntries" ("Id");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260203063309_AddStockLedgerAndAllocation') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260203063309_AddStockLedgerAndAllocation', '10.0.3');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260203082321_AddLedgerReadPerformanceIndexes') THEN
    DROP INDEX "IX_StockLedgerEntries_SerialisedItemId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260203082321_AddLedgerReadPerformanceIndexes') THEN
    DROP INDEX "IX_StockAllocations_MaterialId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260203082321_AddLedgerReadPerformanceIndexes') THEN
    CREATE INDEX "IX_StockLedgerEntries_CompanyId_IsDeleted_CreatedAt" ON "StockLedgerEntries" ("CompanyId", "IsDeleted", "CreatedAt" DESC);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260203082321_AddLedgerReadPerformanceIndexes') THEN
    CREATE INDEX "IX_StockLedgerEntries_CompanyId_IsDeleted_MaterialId_LocationId" ON "StockLedgerEntries" ("CompanyId", "IsDeleted", "MaterialId", "LocationId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260203082321_AddLedgerReadPerformanceIndexes') THEN
    CREATE INDEX "IX_StockLedgerEntries_CompanyId_IsDeleted_OrderId" ON "StockLedgerEntries" ("CompanyId", "IsDeleted", "OrderId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260203082321_AddLedgerReadPerformanceIndexes') THEN
    CREATE INDEX "IX_StockLedgerEntries_SerialisedItemId_CreatedAt" ON "StockLedgerEntries" ("SerialisedItemId", "CreatedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260203082321_AddLedgerReadPerformanceIndexes') THEN
    CREATE INDEX "IX_StockAllocations_CompanyId_IsDeleted_Status" ON "StockAllocations" ("CompanyId", "IsDeleted", "Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260203082321_AddLedgerReadPerformanceIndexes') THEN
    CREATE INDEX "IX_StockAllocations_MaterialId_LocationId_Status" ON "StockAllocations" ("MaterialId", "LocationId", "Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260203082321_AddLedgerReadPerformanceIndexes') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260203082321_AddLedgerReadPerformanceIndexes', '10.0.3');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260203082947_AddLedgerBalanceCache') THEN
    CREATE TABLE "LedgerBalanceCaches" (
        "Id" uuid NOT NULL,
        "CompanyId" uuid,
        "MaterialId" uuid NOT NULL,
        "LocationId" uuid NOT NULL,
        "DepartmentId" uuid NOT NULL,
        "OnHand" numeric NOT NULL,
        "Reserved" numeric NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "LastLedgerEntryId" uuid,
        CONSTRAINT "PK_LedgerBalanceCaches" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260203082947_AddLedgerBalanceCache') THEN
    CREATE INDEX "IX_LedgerBalanceCaches_CompanyId_DepartmentId" ON "LedgerBalanceCaches" ("CompanyId", "DepartmentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260203082947_AddLedgerBalanceCache') THEN
    CREATE UNIQUE INDEX "IX_LedgerBalanceCaches_CompanyId_MaterialId_LocationId" ON "LedgerBalanceCaches" ("CompanyId", "MaterialId", "LocationId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260203082947_AddLedgerBalanceCache') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260203082947_AddLedgerBalanceCache', '10.0.3');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260203093328_AddStockByLocationSnapshots') THEN
    CREATE TABLE "StockByLocationSnapshots" (
        "Id" uuid NOT NULL,
        "CompanyId" uuid NOT NULL,
        "DepartmentId" uuid,
        "MaterialId" uuid NOT NULL,
        "LocationId" uuid NOT NULL,
        "PeriodStart" timestamp with time zone NOT NULL,
        "PeriodEnd" timestamp with time zone NOT NULL,
        "SnapshotType" text NOT NULL,
        "QuantityOnHand" numeric NOT NULL,
        "QuantityReserved" numeric NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_StockByLocationSnapshots" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260203093328_AddStockByLocationSnapshots') THEN
    CREATE INDEX "IX_StockByLocationSnapshots_CompanyId_DepartmentId_Period_Type" ON "StockByLocationSnapshots" ("CompanyId", "DepartmentId", "PeriodStart", "SnapshotType");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260203093328_AddStockByLocationSnapshots') THEN
    CREATE UNIQUE INDEX "IX_StockByLocationSnapshots_CompanyId_MaterialId_LocationId_PeriodStart_Type" ON "StockByLocationSnapshots" ("CompanyId", "MaterialId", "LocationId", "PeriodStart", "SnapshotType");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260203093328_AddStockByLocationSnapshots') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260203093328_AddStockByLocationSnapshots', '10.0.3');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260308102000_AddOrderFinancialAlerts') THEN
    ALTER TABLE "OrderTypes" DROP CONSTRAINT "FK_OrderTypes_OrderTypes_ParentOrderTypeId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260308102000_AddOrderFinancialAlerts') THEN

    DO $$
    BEGIN
      IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'ParserReplayRuns') THEN
        ALTER TABLE "ParserReplayRuns" ALTER COLUMN "OldConfidence" TYPE numeric;
        ALTER TABLE "ParserReplayRuns" ALTER COLUMN "NewConfidence" TYPE numeric;
      END IF;
    END $$;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260308102000_AddOrderFinancialAlerts') THEN
    CREATE TABLE "OrderFinancialAlerts" (
        "Id" uuid NOT NULL,
        "OrderId" uuid NOT NULL,
        "AlertCode" character varying(64) NOT NULL,
        "Severity" character varying(32) NOT NULL,
        "Message" character varying(1024) NOT NULL,
        "RevenueAmount" numeric,
        "PayoutAmount" numeric,
        "ProfitAmount" numeric,
        "MarginPercent" numeric,
        "IsActive" boolean NOT NULL,
        "CompanyId" uuid,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "IsDeleted" boolean NOT NULL,
        "DeletedAt" timestamp with time zone,
        "DeletedByUserId" uuid,
        "RowVersion" bytea,
        CONSTRAINT "PK_OrderFinancialAlerts" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260308102000_AddOrderFinancialAlerts') THEN
    CREATE INDEX IF NOT EXISTS "IX_Orders_PartnerId" ON "Orders" ("PartnerId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260308102000_AddOrderFinancialAlerts') THEN
    CREATE INDEX "IX_OrderFinancialAlerts_CompanyId" ON "OrderFinancialAlerts" ("CompanyId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260308102000_AddOrderFinancialAlerts') THEN
    CREATE INDEX "IX_OrderFinancialAlerts_CreatedAt" ON "OrderFinancialAlerts" ("CreatedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260308102000_AddOrderFinancialAlerts') THEN
    CREATE INDEX "IX_OrderFinancialAlerts_IsActive" ON "OrderFinancialAlerts" ("IsActive");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260308102000_AddOrderFinancialAlerts') THEN
    CREATE INDEX "IX_OrderFinancialAlerts_OrderId_AlertCode" ON "OrderFinancialAlerts" ("OrderId", "AlertCode");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260308102000_AddOrderFinancialAlerts') THEN

    DO $$
    BEGIN
      IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'FK_Orders_Partners_PartnerId'
      ) THEN
        ALTER TABLE "Orders" ADD CONSTRAINT "FK_Orders_Partners_PartnerId"
          FOREIGN KEY ("PartnerId") REFERENCES "Partners" ("Id") ON DELETE RESTRICT;
      END IF;
    END $$;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260308102000_AddOrderFinancialAlerts') THEN
    ALTER TABLE "OrderTypes" ADD CONSTRAINT "FK_OrderTypes_OrderTypes_ParentOrderTypeId" FOREIGN KEY ("ParentOrderTypeId") REFERENCES "OrderTypes" ("Id");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260308102000_AddOrderFinancialAlerts') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260308102000_AddOrderFinancialAlerts', '10.0.3');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260308133701_AddServiceProfiles') THEN
    CREATE TABLE "ServiceProfiles" (
        "Id" uuid NOT NULL,
        "Code" character varying(50) NOT NULL,
        "Name" character varying(200) NOT NULL,
        "Description" character varying(500),
        "IsActive" boolean NOT NULL,
        "DisplayOrder" integer NOT NULL,
        "CompanyId" uuid,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "IsDeleted" boolean NOT NULL,
        "DeletedAt" timestamp with time zone,
        "DeletedByUserId" uuid,
        "RowVersion" bytea,
        CONSTRAINT "PK_ServiceProfiles" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260308133701_AddServiceProfiles') THEN
    CREATE UNIQUE INDEX "IX_ServiceProfiles_CompanyId_Code" ON "ServiceProfiles" ("CompanyId", "Code") WHERE "IsDeleted" = false;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260308133701_AddServiceProfiles') THEN
    CREATE INDEX "IX_ServiceProfiles_CompanyId_IsActive" ON "ServiceProfiles" ("CompanyId", "IsActive") WHERE "IsDeleted" = false;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260308133701_AddServiceProfiles') THEN
    CREATE TABLE "OrderCategoryServiceProfiles" (
        "Id" uuid NOT NULL,
        "OrderCategoryId" uuid NOT NULL,
        "ServiceProfileId" uuid NOT NULL,
        "CompanyId" uuid,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "IsDeleted" boolean NOT NULL,
        "DeletedAt" timestamp with time zone,
        "DeletedByUserId" uuid,
        "RowVersion" bytea,
        CONSTRAINT "PK_OrderCategoryServiceProfiles" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_OrderCategoryServiceProfiles_OrderCategories_OrderCategoryId" FOREIGN KEY ("OrderCategoryId") REFERENCES "OrderCategories" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_OrderCategoryServiceProfiles_ServiceProfiles_ServiceProfileId" FOREIGN KEY ("ServiceProfileId") REFERENCES "ServiceProfiles" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260308133701_AddServiceProfiles') THEN
    CREATE UNIQUE INDEX "IX_OrderCategoryServiceProfiles_CompanyId_OrderCategoryId" ON "OrderCategoryServiceProfiles" ("CompanyId", "OrderCategoryId") WHERE "IsDeleted" = false;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260308133701_AddServiceProfiles') THEN
    CREATE INDEX "IX_OrderCategoryServiceProfiles_OrderCategoryId" ON "OrderCategoryServiceProfiles" ("OrderCategoryId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260308133701_AddServiceProfiles') THEN
    CREATE INDEX "IX_OrderCategoryServiceProfiles_ServiceProfileId" ON "OrderCategoryServiceProfiles" ("ServiceProfileId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260308133701_AddServiceProfiles') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260308133701_AddServiceProfiles', '10.0.3');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260308134742_AddServiceProfileIdToBaseWorkRate') THEN
    ALTER TABLE "BaseWorkRates" ADD "ServiceProfileId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260308134742_AddServiceProfileIdToBaseWorkRate') THEN
    CREATE INDEX "IX_BaseWorkRates_ServiceProfileId" ON "BaseWorkRates" ("ServiceProfileId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260308134742_AddServiceProfileIdToBaseWorkRate') THEN
    CREATE INDEX "IX_BaseWorkRates_ServiceProfileLookup" ON "BaseWorkRates" ("RateGroupId", "ServiceProfileId", "InstallationMethodId", "OrderSubtypeId") WHERE "IsDeleted" = false AND "IsActive" = true;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260308134742_AddServiceProfileIdToBaseWorkRate') THEN
    ALTER TABLE "BaseWorkRates" ADD CONSTRAINT "FK_BaseWorkRates_ServiceProfiles_ServiceProfileId" FOREIGN KEY ("ServiceProfileId") REFERENCES "ServiceProfiles" ("Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260308134742_AddServiceProfileIdToBaseWorkRate') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260308134742_AddServiceProfileIdToBaseWorkRate', '10.0.3');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260308163857_AddUserAgentToRefreshToken') THEN
    DROP INDEX IF EXISTS "IX_PasswordResetTokens_TokenHash";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260308163857_AddUserAgentToRefreshToken') THEN
    ALTER TABLE "RefreshTokens" ADD "UserAgent" text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260308163857_AddUserAgentToRefreshToken') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260308163857_AddUserAgentToRefreshToken', '10.0.3');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260308193638_AddEventBusCorrelationAndEventStore') THEN
    ALTER TABLE "WorkflowJobs" ADD "CorrelationId" character varying(100);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260308193638_AddEventBusCorrelationAndEventStore') THEN
    CREATE TABLE "EventStore" (
        "EventId" uuid NOT NULL,
        "EventType" character varying(200) NOT NULL,
        "Payload" jsonb NOT NULL,
        "OccurredAtUtc" timestamp with time zone NOT NULL,
        "ProcessedAtUtc" timestamp with time zone,
        "RetryCount" integer NOT NULL,
        "Status" character varying(50) NOT NULL,
        "CorrelationId" character varying(100),
        "CompanyId" uuid,
        CONSTRAINT "PK_EventStore" PRIMARY KEY ("EventId")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260308193638_AddEventBusCorrelationAndEventStore') THEN
    CREATE INDEX "IX_EventStore_CompanyId_EventType_OccurredAtUtc" ON "EventStore" ("CompanyId", "EventType", "OccurredAtUtc");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260308193638_AddEventBusCorrelationAndEventStore') THEN
    CREATE INDEX "IX_EventStore_CorrelationId" ON "EventStore" ("CorrelationId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260308193638_AddEventBusCorrelationAndEventStore') THEN
    CREATE INDEX "IX_EventStore_Status" ON "EventStore" ("Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260308193638_AddEventBusCorrelationAndEventStore') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260308193638_AddEventBusCorrelationAndEventStore', '10.0.3');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260308194528_ExtendEventStorePhase2') THEN
    ALTER TABLE "EventStore" ADD "CreatedAtUtc" timestamp with time zone NOT NULL DEFAULT TIMESTAMPTZ '-infinity';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260308194528_ExtendEventStorePhase2') THEN
    ALTER TABLE "EventStore" ADD "EntityId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260308194528_ExtendEventStorePhase2') THEN
    ALTER TABLE "EventStore" ADD "EntityType" character varying(200);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260308194528_ExtendEventStorePhase2') THEN
    ALTER TABLE "EventStore" ADD "LastError" character varying(2000);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260308194528_ExtendEventStorePhase2') THEN
    ALTER TABLE "EventStore" ADD "LastErrorAtUtc" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260308194528_ExtendEventStorePhase2') THEN
    ALTER TABLE "EventStore" ADD "LastHandler" character varying(500);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260308194528_ExtendEventStorePhase2') THEN
    ALTER TABLE "EventStore" ADD "ParentEventId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260308194528_ExtendEventStorePhase2') THEN
    ALTER TABLE "EventStore" ADD "Source" character varying(200);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260308194528_ExtendEventStorePhase2') THEN
    ALTER TABLE "EventStore" ADD "TriggeredByUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260308194528_ExtendEventStorePhase2') THEN
    CREATE INDEX "IX_EventStore_OccurredAtUtc" ON "EventStore" ("OccurredAtUtc");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260308194528_ExtendEventStorePhase2') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260308194528_ExtendEventStorePhase2', '10.0.3');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260308204047_AddReplayOperations') THEN

                    CREATE TABLE IF NOT EXISTS "ReplayOperations" (
                        "Id" uuid NOT NULL,
                        "RequestedByUserId" uuid NULL,
                        "RequestedAtUtc" timestamp with time zone NOT NULL,
                        "DryRun" boolean NOT NULL,
                        "ReplayReason" character varying(2000) NULL,
                        "CompanyId" uuid NULL,
                        "EventType" character varying(200) NULL,
                        "Status" character varying(50) NULL,
                        "FromOccurredAtUtc" timestamp with time zone NULL,
                        "ToOccurredAtUtc" timestamp with time zone NULL,
                        "EntityType" character varying(200) NULL,
                        "EntityId" uuid NULL,
                        "CorrelationId" character varying(200) NULL,
                        "MaxEvents" integer NULL,
                        "TotalMatched" integer NULL,
                        "TotalEligible" integer NULL,
                        "TotalExecuted" integer NULL,
                        "TotalSucceeded" integer NULL,
                        "TotalFailed" integer NULL,
                        "ReplayCorrelationId" character varying(200) NULL,
                        "Notes" character varying(4000) NULL,
                        "State" character varying(50) NULL,
                        "CompletedAtUtc" timestamp with time zone NULL,
                        CONSTRAINT "PK_ReplayOperations" PRIMARY KEY ("Id")
                    );
                    CREATE INDEX IF NOT EXISTS "IX_ReplayOperations_RequestedAtUtc" ON "ReplayOperations" ("RequestedAtUtc");
                    CREATE INDEX IF NOT EXISTS "IX_ReplayOperations_CompanyId_RequestedAtUtc" ON "ReplayOperations" ("CompanyId", "RequestedAtUtc");
                    CREATE INDEX IF NOT EXISTS "IX_ReplayOperations_RequestedByUserId" ON "ReplayOperations" ("RequestedByUserId");

                    CREATE TABLE IF NOT EXISTS "ReplayOperationEvents" (
                        "Id" uuid NOT NULL,
                        "ReplayOperationId" uuid NOT NULL,
                        "EventId" uuid NOT NULL,
                        "Succeeded" boolean NOT NULL,
                        "ErrorMessage" character varying(2000) NULL,
                        "ProcessedAtUtc" timestamp with time zone NOT NULL,
                        CONSTRAINT "PK_ReplayOperationEvents" PRIMARY KEY ("Id")
                    );
                    CREATE INDEX IF NOT EXISTS "IX_ReplayOperationEvents_ReplayOperationId" ON "ReplayOperationEvents" ("ReplayOperationId");
                    CREATE INDEX IF NOT EXISTS "IX_ReplayOperationEvents_EventId" ON "ReplayOperationEvents" ("EventId");
                
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260308204047_AddReplayOperations') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260308204047_AddReplayOperations', '10.0.3');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260308204750_AddSlaRulesTable') THEN
    CREATE TABLE "SlaRules" (
        "Id" uuid NOT NULL,
        "RuleType" character varying(50) NOT NULL,
        "TargetType" character varying(50) NOT NULL,
        "TargetName" character varying(200) NOT NULL,
        "MaxDurationSeconds" integer NOT NULL,
        "WarningThresholdSeconds" integer,
        "EscalationThresholdSeconds" integer,
        "Enabled" boolean NOT NULL DEFAULT TRUE,
        "CompanyId" uuid,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "IsDeleted" boolean NOT NULL,
        "DeletedAt" timestamp with time zone,
        "DeletedByUserId" uuid,
        "RowVersion" bytea,
        CONSTRAINT "PK_SlaRules" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260308204750_AddSlaRulesTable') THEN
    CREATE INDEX "IX_SlaRules_CompanyId" ON "SlaRules" ("CompanyId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260308204750_AddSlaRulesTable') THEN
    CREATE INDEX "IX_SlaRules_CompanyId_Enabled_RuleType" ON "SlaRules" ("CompanyId", "Enabled", "RuleType");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260308204750_AddSlaRulesTable') THEN
    CREATE INDEX "IX_SlaRules_CompanyId_TargetType_TargetName" ON "SlaRules" ("CompanyId", "TargetType", "TargetName");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260308204750_AddSlaRulesTable') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260308204750_AddSlaRulesTable', '10.0.3');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260308204838_AddSlaBreachesTable') THEN
    CREATE TABLE "SlaBreaches" (
        "Id" uuid NOT NULL,
        "CompanyId" uuid,
        "RuleId" uuid NOT NULL,
        "TargetType" character varying(50) NOT NULL,
        "TargetId" character varying(100) NOT NULL,
        "CorrelationId" character varying(100),
        "DetectedAtUtc" timestamp with time zone NOT NULL,
        "DurationSeconds" double precision NOT NULL,
        "Severity" character varying(20) NOT NULL,
        "Status" character varying(20) NOT NULL,
        "AcknowledgedAtUtc" timestamp with time zone,
        "ResolvedAtUtc" timestamp with time zone,
        "ResolvedByUserId" uuid,
        "Title" character varying(500),
        CONSTRAINT "PK_SlaBreaches" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260308204838_AddSlaBreachesTable') THEN
    CREATE INDEX "IX_SlaBreaches_CompanyId" ON "SlaBreaches" ("CompanyId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260308204838_AddSlaBreachesTable') THEN
    CREATE INDEX "IX_SlaBreaches_CompanyId_Severity" ON "SlaBreaches" ("CompanyId", "Severity");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260308204838_AddSlaBreachesTable') THEN
    CREATE INDEX "IX_SlaBreaches_CompanyId_Status_DetectedAtUtc" ON "SlaBreaches" ("CompanyId", "Status", "DetectedAtUtc");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260308204838_AddSlaBreachesTable') THEN
    CREATE INDEX "IX_SlaBreaches_CorrelationId" ON "SlaBreaches" ("CorrelationId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260308204838_AddSlaBreachesTable') THEN
    CREATE INDEX "IX_SlaBreaches_RuleId" ON "SlaBreaches" ("RuleId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260308204838_AddSlaBreachesTable') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260308204838_AddSlaBreachesTable', '10.0.3');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260308214936_AddWorkflowTransitionHistory') THEN
    CREATE TABLE "WorkflowTransitionHistory" (
        "EventId" uuid NOT NULL,
        "WorkflowJobId" uuid NOT NULL,
        "CompanyId" uuid,
        "EntityType" character varying(100) NOT NULL,
        "EntityId" uuid NOT NULL,
        "FromStatus" character varying(100) NOT NULL,
        "ToStatus" character varying(100) NOT NULL,
        "OccurredAtUtc" timestamp with time zone NOT NULL,
        "CreatedAtUtc" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_WorkflowTransitionHistory" PRIMARY KEY ("EventId")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260308214936_AddWorkflowTransitionHistory') THEN
    CREATE INDEX "IX_WorkflowTransitionHistory_CompanyId_EntityType_EntityId" ON "WorkflowTransitionHistory" ("CompanyId", "EntityType", "EntityId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260308214936_AddWorkflowTransitionHistory') THEN
    CREATE INDEX "IX_WorkflowTransitionHistory_OccurredAtUtc" ON "WorkflowTransitionHistory" ("OccurredAtUtc");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260308214936_AddWorkflowTransitionHistory') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260308214936_AddWorkflowTransitionHistory', '10.0.3');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260309013846_AddEventLedgerEntries') THEN
    CREATE TABLE "LedgerEntries" (
        "Id" uuid NOT NULL,
        "SourceEventId" uuid,
        "ReplayOperationId" uuid,
        "LedgerFamily" character varying(64) NOT NULL,
        "Category" character varying(128),
        "CompanyId" uuid,
        "EntityType" character varying(128),
        "EntityId" uuid,
        "EventType" character varying(128) NOT NULL,
        "OccurredAtUtc" timestamp with time zone NOT NULL,
        "RecordedAtUtc" timestamp with time zone NOT NULL,
        "PayloadSnapshot" text,
        "CorrelationId" character varying(128),
        "TriggeredByUserId" uuid,
        "OrderingStrategyId" character varying(64),
        CONSTRAINT "PK_LedgerEntries" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260309013846_AddEventLedgerEntries') THEN
    CREATE INDEX "IX_LedgerEntries_CompanyId_LedgerFamily_OccurredAtUtc" ON "LedgerEntries" ("CompanyId", "LedgerFamily", "OccurredAtUtc");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260309013846_AddEventLedgerEntries') THEN
    CREATE INDEX "IX_LedgerEntries_EntityType_EntityId_LedgerFamily" ON "LedgerEntries" ("EntityType", "EntityId", "LedgerFamily");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260309013846_AddEventLedgerEntries') THEN
    CREATE INDEX "IX_LedgerEntries_RecordedAtUtc" ON "LedgerEntries" ("RecordedAtUtc");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260309013846_AddEventLedgerEntries') THEN
    CREATE INDEX "IX_LedgerEntries_ReplayOperationId" ON "LedgerEntries" ("ReplayOperationId") WHERE "ReplayOperationId" IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260309013846_AddEventLedgerEntries') THEN
    CREATE UNIQUE INDEX "IX_LedgerEntries_ReplayOperationId_LedgerFamily" ON "LedgerEntries" ("ReplayOperationId", "LedgerFamily") WHERE "ReplayOperationId" IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260309013846_AddEventLedgerEntries') THEN
    CREATE INDEX "IX_LedgerEntries_SourceEventId" ON "LedgerEntries" ("SourceEventId") WHERE "SourceEventId" IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260309013846_AddEventLedgerEntries') THEN
    CREATE UNIQUE INDEX "IX_LedgerEntries_SourceEventId_LedgerFamily" ON "LedgerEntries" ("SourceEventId", "LedgerFamily") WHERE "SourceEventId" IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260309013846_AddEventLedgerEntries') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260309013846_AddEventLedgerEntries', '10.0.3');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260309025207_AddEventProcessingLog') THEN
    CREATE TABLE "EventProcessingLog" (
        "Id" uuid NOT NULL,
        "EventId" uuid NOT NULL,
        "HandlerName" character varying(256) NOT NULL,
        "ReplayOperationId" uuid,
        "State" character varying(32) NOT NULL,
        "StartedAtUtc" timestamp with time zone NOT NULL,
        "CompletedAtUtc" timestamp with time zone,
        "Error" character varying(2000),
        "AttemptCount" integer NOT NULL,
        "CorrelationId" character varying(128),
        CONSTRAINT "PK_EventProcessingLog" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260309025207_AddEventProcessingLog') THEN
    CREATE INDEX "IX_EventProcessingLog_EventId" ON "EventProcessingLog" ("EventId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260309025207_AddEventProcessingLog') THEN
    CREATE UNIQUE INDEX "IX_EventProcessingLog_EventId_HandlerName" ON "EventProcessingLog" ("EventId", "HandlerName");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260309025207_AddEventProcessingLog') THEN
    CREATE INDEX "IX_EventProcessingLog_ReplayOperationId" ON "EventProcessingLog" ("ReplayOperationId") WHERE "ReplayOperationId" IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260309025207_AddEventProcessingLog') THEN
    CREATE INDEX "IX_EventProcessingLog_State_StartedAtUtc" ON "EventProcessingLog" ("State", "StartedAtUtc");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260309025207_AddEventProcessingLog') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260309025207_AddEventProcessingLog', '10.0.3');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260309030727_AddReplayExecutionLock') THEN
    CREATE TABLE "ReplayExecutionLock" (
        "Id" uuid NOT NULL,
        "CompanyId" uuid NOT NULL,
        "ReplayOperationId" uuid NOT NULL,
        "AcquiredAtUtc" timestamp with time zone NOT NULL,
        "ExpiresAtUtc" timestamp with time zone,
        "ReleasedAtUtc" timestamp with time zone,
        CONSTRAINT "PK_ReplayExecutionLock" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260309030727_AddReplayExecutionLock') THEN
    CREATE UNIQUE INDEX "IX_ReplayExecutionLock_CompanyId" ON "ReplayExecutionLock" ("CompanyId") WHERE "ReleasedAtUtc" IS NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260309030727_AddReplayExecutionLock') THEN
    CREATE INDEX "IX_ReplayExecutionLock_ReleasedAtUtc" ON "ReplayExecutionLock" ("ReleasedAtUtc");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260309030727_AddReplayExecutionLock') THEN
    CREATE INDEX "IX_ReplayExecutionLock_ReplayOperationId" ON "ReplayExecutionLock" ("ReplayOperationId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260309030727_AddReplayExecutionLock') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260309030727_AddReplayExecutionLock', '10.0.3');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260309120000_AddJobRunEventId') THEN

    DO $$
    BEGIN
      IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = current_schema() AND table_name = 'JobRuns') THEN
        IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = current_schema() AND table_name = 'JobRuns' AND column_name = 'EventId') THEN
          ALTER TABLE "JobRuns" ADD COLUMN "EventId" uuid;
        END IF;
        IF NOT EXISTS (SELECT 1 FROM pg_indexes WHERE schemaname = current_schema() AND tablename = 'JobRuns' AND indexname = 'IX_JobRuns_EventId') THEN
          CREATE INDEX "IX_JobRuns_EventId" ON "JobRuns" ("EventId") WHERE "EventId" IS NOT NULL;
        END IF;
      END IF;
    END $$;

    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260309120000_AddJobRunEventId') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260309120000_AddJobRunEventId', '10.0.3');
    END IF;
END $EF$;
COMMIT;

