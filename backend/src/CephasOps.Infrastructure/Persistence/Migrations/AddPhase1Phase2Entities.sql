-- Migration: Add Phase 1 & Phase 2 Entities
-- Description: Creates all database tables for Phase 1 and Phase 2 entities
-- Date: 2025-01-XX

-- ============================================
-- COMPANIES & PARTNERS
-- ============================================

-- Companies table
CREATE TABLE IF NOT EXISTS "Companies" (
    "Id" uuid NOT NULL,
    "LegalName" character varying(500) NOT NULL,
    "ShortName" character varying(100) NOT NULL,
    "RegistrationNo" character varying(100) NULL,
    "TaxId" character varying(100) NULL,
    "Vertical" character varying(50) NOT NULL,
    "Address" text NULL,
    "Phone" character varying(50) NULL,
    "Email" character varying(255) NULL,
    "IsActive" boolean NOT NULL DEFAULT true,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT (now()),
    CONSTRAINT "PK_Companies" PRIMARY KEY ("Id")
);

CREATE INDEX IF NOT EXISTS "IX_Companies_ShortName" ON "Companies" ("ShortName");
CREATE INDEX IF NOT EXISTS "IX_Companies_IsActive" ON "Companies" ("IsActive");

-- Partners table
CREATE TABLE IF NOT EXISTS "Partners" (
    "Id" uuid NOT NULL,
    "CompanyId" uuid NOT NULL,
    "Name" character varying(500) NOT NULL,
    "PartnerType" character varying(50) NOT NULL,
    "GroupId" uuid NULL,
    "BillingAddress" text NULL,
    "ContactName" character varying(200) NULL,
    "ContactEmail" character varying(255) NULL,
    "ContactPhone" character varying(50) NULL,
    "IsActive" boolean NOT NULL DEFAULT true,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT (now()),
    "CreatedById" uuid NOT NULL,
    CONSTRAINT "PK_Partners" PRIMARY KEY ("Id")
);

CREATE INDEX IF NOT EXISTS "IX_Partners_CompanyId_Name" ON "Partners" ("CompanyId", "Name");
CREATE INDEX IF NOT EXISTS "IX_Partners_CompanyId_IsActive" ON "Partners" ("CompanyId", "IsActive");

-- PartnerGroups table
CREATE TABLE IF NOT EXISTS "PartnerGroups" (
    "Id" uuid NOT NULL,
    "CompanyId" uuid NOT NULL,
    "Name" character varying(200) NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT (now()),
    "CreatedById" uuid NOT NULL,
    CONSTRAINT "PK_PartnerGroups" PRIMARY KEY ("Id")
);

CREATE INDEX IF NOT EXISTS "IX_PartnerGroups_CompanyId_Name" ON "PartnerGroups" ("CompanyId", "Name");

-- CostCentres table
CREATE TABLE IF NOT EXISTS "CostCentres" (
    "Id" uuid NOT NULL,
    "CompanyId" uuid NOT NULL,
    "Code" character varying(50) NOT NULL,
    "Name" character varying(200) NOT NULL,
    "Description" character varying(1000) NULL,
    "IsActive" boolean NOT NULL DEFAULT true,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT (now()),
    "CreatedById" uuid NOT NULL,
    CONSTRAINT "PK_CostCentres" PRIMARY KEY ("Id"),
    CONSTRAINT "UQ_CostCentres_CompanyId_Code" UNIQUE ("CompanyId", "Code")
);

CREATE INDEX IF NOT EXISTS "IX_CostCentres_CompanyId_IsActive" ON "CostCentres" ("CompanyId", "IsActive");

-- ============================================
-- USERS & RBAC
-- ============================================

-- Users table
CREATE TABLE IF NOT EXISTS "Users" (
    "Id" uuid NOT NULL,
    "Name" character varying(200) NOT NULL,
    "Email" character varying(255) NOT NULL,
    "Phone" character varying(50) NULL,
    "PasswordHash" character varying(500) NULL,
    "IsActive" boolean NOT NULL DEFAULT true,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT (now()),
    CONSTRAINT "PK_Users" PRIMARY KEY ("Id"),
    CONSTRAINT "UQ_Users_Email" UNIQUE ("Email")
);

CREATE INDEX IF NOT EXISTS "IX_Users_IsActive" ON "Users" ("IsActive");

-- UserCompanies table
CREATE TABLE IF NOT EXISTS "UserCompanies" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "CompanyId" uuid NOT NULL,
    "IsDefault" boolean NOT NULL DEFAULT false,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT (now()),
    CONSTRAINT "PK_UserCompanies" PRIMARY KEY ("Id"),
    CONSTRAINT "UQ_UserCompanies_UserId_CompanyId" UNIQUE ("UserId", "CompanyId")
);

CREATE INDEX IF NOT EXISTS "IX_UserCompanies_UserId_IsDefault" ON "UserCompanies" ("UserId", "IsDefault");

-- Roles table
CREATE TABLE IF NOT EXISTS "Roles" (
    "Id" uuid NOT NULL,
    "Name" character varying(100) NOT NULL,
    "Scope" character varying(50) NOT NULL,
    CONSTRAINT "PK_Roles" PRIMARY KEY ("Id")
);

CREATE INDEX IF NOT EXISTS "IX_Roles_Name" ON "Roles" ("Name");

-- UserRoles table
CREATE TABLE IF NOT EXISTS "UserRoles" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "CompanyId" uuid NOT NULL,
    "RoleId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT (now()),
    CONSTRAINT "PK_UserRoles" PRIMARY KEY ("Id"),
    CONSTRAINT "UQ_UserRoles_UserId_CompanyId_RoleId" UNIQUE ("UserId", "CompanyId", "RoleId")
);

-- Permissions table
CREATE TABLE IF NOT EXISTS "Permissions" (
    "Id" uuid NOT NULL,
    "Name" character varying(200) NOT NULL,
    "Description" character varying(500) NULL,
    CONSTRAINT "PK_Permissions" PRIMARY KEY ("Id"),
    CONSTRAINT "UQ_Permissions_Name" UNIQUE ("Name")
);

-- RolePermissions table
CREATE TABLE IF NOT EXISTS "RolePermissions" (
    "RoleId" uuid NOT NULL,
    "PermissionId" uuid NOT NULL,
    CONSTRAINT "PK_RolePermissions" PRIMARY KEY ("RoleId", "PermissionId")
);

-- ============================================
-- ORDERS
-- ============================================

-- Orders table
CREATE TABLE IF NOT EXISTS "Orders" (
    "Id" uuid NOT NULL,
    "CompanyId" uuid NOT NULL,
    "PartnerId" uuid NOT NULL,
    "SourceSystem" character varying(50) NOT NULL,
    "SourceEmailId" uuid NULL,
    "OrderTypeId" uuid NOT NULL,
    "ServiceId" character varying(200) NOT NULL,
    "TicketId" character varying(200) NULL,
    "ExternalRef" character varying(200) NULL,
    "Status" character varying(50) NOT NULL,
    "StatusReason" character varying(500) NULL,
    "Priority" character varying(20) NULL,
    "BuildingId" uuid NOT NULL,
    "BuildingName" character varying(500) NULL,
    "UnitNo" character varying(50) NULL,
    "AddressLine1" character varying(500) NOT NULL,
    "AddressLine2" character varying(500) NULL,
    "City" character varying(100) NOT NULL,
    "State" character varying(100) NOT NULL,
    "Postcode" character varying(20) NOT NULL,
    "Latitude" numeric NULL,
    "Longitude" numeric NULL,
    "CustomerName" character varying(200) NOT NULL,
    "CustomerPhone" character varying(50) NOT NULL,
    "CustomerEmail" character varying(255) NULL,
    "OrderNotesInternal" text NULL,
    "PartnerNotes" text NULL,
    "RequestedAppointmentAt" timestamp with time zone NULL,
    "AppointmentDate" date NOT NULL,
    "AppointmentWindowFrom" time NOT NULL,
    "AppointmentWindowTo" time NOT NULL,
    "AssignedSiId" uuid NULL,
    "AssignedTeamId" uuid NULL,
    "KpiCategory" character varying(50) NULL,
    "KpiDueAt" timestamp with time zone NULL,
    "KpiBreachedAt" timestamp with time zone NULL,
    "HasReschedules" boolean NOT NULL DEFAULT false,
    "RescheduleCount" integer NOT NULL DEFAULT 0,
    "DocketUploaded" boolean NOT NULL DEFAULT false,
    "PhotosUploaded" boolean NOT NULL DEFAULT false,
    "SerialsValidated" boolean NOT NULL DEFAULT false,
    "InvoiceEligible" boolean NOT NULL DEFAULT false,
    "InvoiceId" uuid NULL,
    "PayrollPeriodId" uuid NULL,
    "PnlPeriod" character varying(10) NULL,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT (now()),
    "CreatedById" uuid NOT NULL,
    "CancelledAt" timestamp with time zone NULL,
    "CancelledByUserId" uuid NULL,
    CONSTRAINT "PK_Orders" PRIMARY KEY ("Id"),
    CONSTRAINT "UQ_Orders_CompanyId_ServiceId" UNIQUE ("CompanyId", "ServiceId")
);

CREATE INDEX IF NOT EXISTS "IX_Orders_CompanyId_Status_AppointmentDate" ON "Orders" ("CompanyId", "Status", "AppointmentDate");
CREATE INDEX IF NOT EXISTS "IX_Orders_CompanyId_AssignedSiId_AppointmentDate" ON "Orders" ("CompanyId", "AssignedSiId", "AppointmentDate");
CREATE INDEX IF NOT EXISTS "IX_Orders_CompanyId_PartnerId" ON "Orders" ("CompanyId", "PartnerId");
CREATE INDEX IF NOT EXISTS "IX_Orders_CompanyId_BuildingId" ON "Orders" ("CompanyId", "BuildingId");
CREATE INDEX IF NOT EXISTS "IX_Orders_Status" ON "Orders" ("Status");

-- OrderStatusLogs table
CREATE TABLE IF NOT EXISTS "OrderStatusLogs" (
    "Id" uuid NOT NULL,
    "CompanyId" uuid NOT NULL,
    "OrderId" uuid NOT NULL,
    "FromStatus" character varying(50) NULL,
    "ToStatus" character varying(50) NOT NULL,
    "TransitionReason" character varying(500) NULL,
    "TriggeredByUserId" uuid NULL,
    "TriggeredBySiId" uuid NULL,
    "Source" character varying(50) NOT NULL,
    "MetadataJson" jsonb NULL,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT (now()),
    "CreatedById" uuid NOT NULL,
    CONSTRAINT "PK_OrderStatusLogs" PRIMARY KEY ("Id")
);

CREATE INDEX IF NOT EXISTS "IX_OrderStatusLogs_CompanyId_OrderId" ON "OrderStatusLogs" ("CompanyId", "OrderId");
CREATE INDEX IF NOT EXISTS "IX_OrderStatusLogs_CompanyId_CreatedAt" ON "OrderStatusLogs" ("CompanyId", "CreatedAt");
CREATE INDEX IF NOT EXISTS "IX_OrderStatusLogs_OrderId" ON "OrderStatusLogs" ("OrderId");

-- OrderReschedules table
CREATE TABLE IF NOT EXISTS "OrderReschedules" (
    "Id" uuid NOT NULL,
    "CompanyId" uuid NOT NULL,
    "OrderId" uuid NOT NULL,
    "RequestedByUserId" uuid NULL,
    "RequestedBySiId" uuid NULL,
    "RequestedBySource" character varying(50) NOT NULL,
    "RequestedAt" timestamp with time zone NOT NULL,
    "OriginalDate" date NOT NULL,
    "OriginalWindowFrom" time NOT NULL,
    "OriginalWindowTo" time NOT NULL,
    "NewDate" date NOT NULL,
    "NewWindowFrom" time NOT NULL,
    "NewWindowTo" time NOT NULL,
    "Reason" character varying(2000) NOT NULL,
    "ApprovalSource" character varying(50) NULL,
    "ApprovalEmailId" uuid NULL,
    "Status" character varying(50) NOT NULL,
    "StatusChangedByUserId" uuid NULL,
    "StatusChangedAt" timestamp with time zone NULL,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT (now()),
    "CreatedById" uuid NOT NULL,
    CONSTRAINT "PK_OrderReschedules" PRIMARY KEY ("Id")
);

CREATE INDEX IF NOT EXISTS "IX_OrderReschedules_CompanyId_OrderId" ON "OrderReschedules" ("CompanyId", "OrderId");
CREATE INDEX IF NOT EXISTS "IX_OrderReschedules_CompanyId_Status" ON "OrderReschedules" ("CompanyId", "Status");

-- OrderBlockers table
CREATE TABLE IF NOT EXISTS "OrderBlockers" (
    "Id" uuid NOT NULL,
    "CompanyId" uuid NOT NULL,
    "OrderId" uuid NOT NULL,
    "BlockerType" character varying(50) NOT NULL,
    "Description" character varying(2000) NOT NULL,
    "RaisedBySiId" uuid NULL,
    "RaisedByUserId" uuid NULL,
    "RaisedAt" timestamp with time zone NOT NULL,
    "Resolved" boolean NOT NULL DEFAULT false,
    "ResolvedAt" timestamp with time zone NULL,
    "ResolvedByUserId" uuid NULL,
    "ResolutionNotes" character varying(2000) NULL,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT (now()),
    "CreatedById" uuid NOT NULL,
    CONSTRAINT "PK_OrderBlockers" PRIMARY KEY ("Id")
);

CREATE INDEX IF NOT EXISTS "IX_OrderBlockers_CompanyId_OrderId_Resolved" ON "OrderBlockers" ("CompanyId", "OrderId", "Resolved");
CREATE INDEX IF NOT EXISTS "IX_OrderBlockers_OrderId" ON "OrderBlockers" ("OrderId");

-- OrderDockets table
CREATE TABLE IF NOT EXISTS "OrderDockets" (
    "Id" uuid NOT NULL,
    "CompanyId" uuid NOT NULL,
    "OrderId" uuid NOT NULL,
    "FileId" uuid NOT NULL,
    "UploadedBySiId" uuid NULL,
    "UploadedByUserId" uuid NULL,
    "UploadSource" character varying(50) NOT NULL,
    "IsFinal" boolean NOT NULL DEFAULT false,
    "Notes" character varying(2000) NULL,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT (now()),
    "CreatedById" uuid NOT NULL,
    CONSTRAINT "PK_OrderDockets" PRIMARY KEY ("Id")
);

CREATE INDEX IF NOT EXISTS "IX_OrderDockets_CompanyId_OrderId" ON "OrderDockets" ("CompanyId", "OrderId");
CREATE INDEX IF NOT EXISTS "IX_OrderDockets_CompanyId_OrderId_IsFinal" ON "OrderDockets" ("CompanyId", "OrderId", "IsFinal");

-- OrderMaterialUsage table
CREATE TABLE IF NOT EXISTS "OrderMaterialUsage" (
    "Id" uuid NOT NULL,
    "CompanyId" uuid NOT NULL,
    "OrderId" uuid NOT NULL,
    "MaterialId" uuid NOT NULL,
    "SerialisedItemId" uuid NULL,
    "Quantity" numeric(18,4) NOT NULL,
    "UnitCost" numeric(18,4) NULL,
    "TotalCost" numeric(18,4) NULL,
    "SourceLocationId" uuid NULL,
    "StockMovementId" uuid NULL,
    "RecordedBySiId" uuid NULL,
    "RecordedByUserId" uuid NULL,
    "RecordedAt" timestamp with time zone NOT NULL,
    "Notes" character varying(2000) NULL,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT (now()),
    "CreatedById" uuid NOT NULL,
    CONSTRAINT "PK_OrderMaterialUsage" PRIMARY KEY ("Id")
);

CREATE INDEX IF NOT EXISTS "IX_OrderMaterialUsage_CompanyId_OrderId" ON "OrderMaterialUsage" ("CompanyId", "OrderId");
CREATE INDEX IF NOT EXISTS "IX_OrderMaterialUsage_CompanyId_MaterialId" ON "OrderMaterialUsage" ("CompanyId", "MaterialId");
CREATE INDEX IF NOT EXISTS "IX_OrderMaterialUsage_SerialisedItemId" ON "OrderMaterialUsage" ("SerialisedItemId");

-- ============================================
-- BUILDINGS
-- ============================================

-- Buildings table
CREATE TABLE IF NOT EXISTS "Buildings" (
    "Id" uuid NOT NULL,
    "CompanyId" uuid NOT NULL,
    "Name" character varying(500) NOT NULL,
    "Code" character varying(100) NULL,
    "AddressLine1" character varying(500) NOT NULL,
    "AddressLine2" character varying(500) NULL,
    "City" character varying(100) NOT NULL,
    "State" character varying(100) NOT NULL,
    "Postcode" character varying(20) NOT NULL,
    "Latitude" numeric NULL,
    "Longitude" numeric NULL,
    "BuildingType" character varying(50) NULL,
    "IsActive" boolean NOT NULL DEFAULT true,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT (now()),
    "CreatedById" uuid NOT NULL,
    CONSTRAINT "PK_Buildings" PRIMARY KEY ("Id")
);

CREATE INDEX IF NOT EXISTS "IX_Buildings_CompanyId_Name" ON "Buildings" ("CompanyId", "Name");
CREATE INDEX IF NOT EXISTS "IX_Buildings_CompanyId_Code" ON "Buildings" ("CompanyId", "Code");
CREATE INDEX IF NOT EXISTS "IX_Buildings_CompanyId_IsActive" ON "Buildings" ("CompanyId", "IsActive");

-- Splitters table
CREATE TABLE IF NOT EXISTS "Splitters" (
    "Id" uuid NOT NULL,
    "CompanyId" uuid NOT NULL,
    "BuildingId" uuid NOT NULL,
    "Name" character varying(200) NOT NULL,
    "Code" character varying(100) NULL,
    "SplitterType" character varying(20) NOT NULL,
    "Location" character varying(200) NULL,
    "IsActive" boolean NOT NULL DEFAULT true,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT (now()),
    "CreatedById" uuid NOT NULL,
    CONSTRAINT "PK_Splitters" PRIMARY KEY ("Id")
);

CREATE INDEX IF NOT EXISTS "IX_Splitters_CompanyId_BuildingId" ON "Splitters" ("CompanyId", "BuildingId");
CREATE INDEX IF NOT EXISTS "IX_Splitters_CompanyId_IsActive" ON "Splitters" ("CompanyId", "IsActive");

-- SplitterPorts table
CREATE TABLE IF NOT EXISTS "SplitterPorts" (
    "Id" uuid NOT NULL,
    "CompanyId" uuid NOT NULL,
    "SplitterId" uuid NOT NULL,
    "PortNumber" integer NOT NULL,
    "Status" character varying(50) NOT NULL,
    "OrderId" uuid NULL,
    "AssignedAt" timestamp with time zone NULL,
    "IsStandby" boolean NOT NULL DEFAULT false,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT (now()),
    "CreatedById" uuid NOT NULL,
    CONSTRAINT "PK_SplitterPorts" PRIMARY KEY ("Id"),
    CONSTRAINT "UQ_SplitterPorts_CompanyId_SplitterId_PortNumber" UNIQUE ("CompanyId", "SplitterId", "PortNumber")
);

CREATE INDEX IF NOT EXISTS "IX_SplitterPorts_CompanyId_SplitterId_Status" ON "SplitterPorts" ("CompanyId", "SplitterId", "Status");
CREATE INDEX IF NOT EXISTS "IX_SplitterPorts_OrderId" ON "SplitterPorts" ("OrderId");

-- ============================================
-- SERVICE INSTALLERS
-- ============================================

-- ServiceInstallers table
CREATE TABLE IF NOT EXISTS "ServiceInstallers" (
    "Id" uuid NOT NULL,
    "CompanyId" uuid NOT NULL,
    "Name" character varying(200) NOT NULL,
    "EmployeeId" character varying(50) NULL,
    "Phone" character varying(50) NULL,
    "Email" character varying(255) NULL,
    "SiLevel" character varying(50) NOT NULL,
    "IsSubcontractor" boolean NOT NULL DEFAULT false,
    "IsActive" boolean NOT NULL DEFAULT true,
    "UserId" uuid NULL,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT (now()),
    "CreatedById" uuid NOT NULL,
    CONSTRAINT "PK_ServiceInstallers" PRIMARY KEY ("Id")
);

CREATE INDEX IF NOT EXISTS "IX_ServiceInstallers_CompanyId_EmployeeId" ON "ServiceInstallers" ("CompanyId", "EmployeeId");
CREATE INDEX IF NOT EXISTS "IX_ServiceInstallers_CompanyId_IsActive" ON "ServiceInstallers" ("CompanyId", "IsActive");
CREATE INDEX IF NOT EXISTS "IX_ServiceInstallers_UserId" ON "ServiceInstallers" ("UserId");

-- ============================================
-- TASKS
-- ============================================

-- TaskItems table
CREATE TABLE IF NOT EXISTS "TaskItems" (
    "Id" uuid NOT NULL,
    "CompanyId" uuid NOT NULL,
    "DepartmentId" uuid NULL,
    "RequestedByUserId" uuid NOT NULL,
    "AssignedToUserId" uuid NOT NULL,
    "Title" character varying(256) NOT NULL,
    "Description" character varying(4000) NULL,
    "RequestedAt" timestamp with time zone NOT NULL,
    "DueAt" timestamp with time zone NULL,
    "Priority" character varying(20) NOT NULL,
    "Status" character varying(20) NOT NULL,
    "StartedAt" timestamp with time zone NULL,
    "CompletedAt" timestamp with time zone NULL,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT (now()),
    "CreatedById" uuid NOT NULL,
    "UpdatedByUserId" uuid NOT NULL,
    CONSTRAINT "PK_TaskItems" PRIMARY KEY ("Id")
);

CREATE INDEX IF NOT EXISTS "IX_TaskItems_CompanyId_AssignedToUserId_Status" ON "TaskItems" ("CompanyId", "AssignedToUserId", "Status");
CREATE INDEX IF NOT EXISTS "IX_TaskItems_CompanyId_DepartmentId_Status" ON "TaskItems" ("CompanyId", "DepartmentId", "Status");
CREATE INDEX IF NOT EXISTS "IX_TaskItems_CompanyId_RequestedByUserId" ON "TaskItems" ("CompanyId", "RequestedByUserId");

-- ============================================
-- SCHEDULER
-- ============================================

-- ScheduledSlots table
CREATE TABLE IF NOT EXISTS "ScheduledSlots" (
    "Id" uuid NOT NULL,
    "CompanyId" uuid NOT NULL,
    "OrderId" uuid NOT NULL,
    "ServiceInstallerId" uuid NOT NULL,
    "Date" date NOT NULL,
    "WindowFrom" time NOT NULL,
    "WindowTo" time NOT NULL,
    "PlannedTravelMin" integer NULL,
    "SequenceIndex" integer NOT NULL,
    "Status" character varying(50) NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT (now()),
    "CreatedById" uuid NOT NULL,
    CONSTRAINT "PK_ScheduledSlots" PRIMARY KEY ("Id")
);

CREATE INDEX IF NOT EXISTS "IX_ScheduledSlots_CompanyId_ServiceInstallerId_Date" ON "ScheduledSlots" ("CompanyId", "ServiceInstallerId", "Date");
CREATE INDEX IF NOT EXISTS "IX_ScheduledSlots_CompanyId_OrderId" ON "ScheduledSlots" ("CompanyId", "OrderId");
CREATE INDEX IF NOT EXISTS "IX_ScheduledSlots_CompanyId_Date_Status" ON "ScheduledSlots" ("CompanyId", "Date", "Status");

-- SiAvailabilities table
CREATE TABLE IF NOT EXISTS "SiAvailabilities" (
    "Id" uuid NOT NULL,
    "CompanyId" uuid NOT NULL,
    "ServiceInstallerId" uuid NOT NULL,
    "Date" date NOT NULL,
    "IsWorkingDay" boolean NOT NULL DEFAULT true,
    "WorkingFrom" time NULL,
    "WorkingTo" time NULL,
    "MaxJobs" integer NOT NULL DEFAULT 0,
    "CurrentJobsCount" integer NOT NULL DEFAULT 0,
    "Notes" character varying(1000) NULL,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT (now()),
    "CreatedById" uuid NOT NULL,
    CONSTRAINT "PK_SiAvailabilities" PRIMARY KEY ("Id"),
    CONSTRAINT "UQ_SiAvailabilities_CompanyId_ServiceInstallerId_Date" UNIQUE ("CompanyId", "ServiceInstallerId", "Date")
);

CREATE INDEX IF NOT EXISTS "IX_SiAvailabilities_CompanyId_Date" ON "SiAvailabilities" ("CompanyId", "Date");

-- SiLeaveRequests table
CREATE TABLE IF NOT EXISTS "SiLeaveRequests" (
    "Id" uuid NOT NULL,
    "CompanyId" uuid NOT NULL,
    "ServiceInstallerId" uuid NOT NULL,
    "DateFrom" date NOT NULL,
    "DateTo" date NOT NULL,
    "Reason" character varying(500) NOT NULL,
    "Status" character varying(50) NOT NULL,
    "ApprovedByUserId" uuid NULL,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT (now()),
    "CreatedById" uuid NOT NULL,
    CONSTRAINT "PK_SiLeaveRequests" PRIMARY KEY ("Id")
);

CREATE INDEX IF NOT EXISTS "IX_SiLeaveRequests_CompanyId_ServiceInstallerId_DateFrom_DateTo" ON "SiLeaveRequests" ("CompanyId", "ServiceInstallerId", "DateFrom", "DateTo");
CREATE INDEX IF NOT EXISTS "IX_SiLeaveRequests_CompanyId_Status" ON "SiLeaveRequests" ("CompanyId", "Status");

-- ============================================
-- PARSER
-- ============================================

-- ParseSessions table
CREATE TABLE IF NOT EXISTS "ParseSessions" (
    "Id" uuid NOT NULL,
    "CompanyId" uuid NOT NULL,
    "EmailMessageId" uuid NOT NULL,
    "ParserTemplateId" uuid NULL,
    "Status" character varying(50) NOT NULL,
    "ErrorMessage" character varying(2000) NULL,
    "SnapshotFileId" uuid NULL,
    "ParsedOrdersCount" integer NOT NULL DEFAULT 0,
    "CompletedAt" timestamp with time zone NULL,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT (now()),
    "CreatedById" uuid NOT NULL,
    CONSTRAINT "PK_ParseSessions" PRIMARY KEY ("Id")
);

CREATE INDEX IF NOT EXISTS "IX_ParseSessions_CompanyId_EmailMessageId" ON "ParseSessions" ("CompanyId", "EmailMessageId");
CREATE INDEX IF NOT EXISTS "IX_ParseSessions_CompanyId_Status_CreatedAt" ON "ParseSessions" ("CompanyId", "Status", "CreatedAt");

-- ParsedOrderDrafts table
CREATE TABLE IF NOT EXISTS "ParsedOrderDrafts" (
    "Id" uuid NOT NULL,
    "CompanyId" uuid NOT NULL,
    "ParseSessionId" uuid NOT NULL,
    "PartnerId" uuid NULL,
    "BuildingId" uuid NULL,
    "ServiceId" character varying(200) NULL,
    "TicketId" character varying(200) NULL,
    "CustomerName" character varying(200) NULL,
    "CustomerPhone" character varying(50) NULL,
    "AddressText" character varying(1000) NULL,
    "AppointmentDate" date NULL,
    "AppointmentWindow" character varying(50) NULL,
    "OrderTypeHint" character varying(100) NULL,
    "ConfidenceScore" numeric(5,4) NOT NULL DEFAULT 0,
    "ValidationStatus" character varying(50) NOT NULL,
    "ValidationNotes" character varying(2000) NULL,
    "CreatedOrderId" uuid NULL,
    "CreatedByUserId" uuid NULL,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT (now()),
    CONSTRAINT "PK_ParsedOrderDrafts" PRIMARY KEY ("Id")
);

CREATE INDEX IF NOT EXISTS "IX_ParsedOrderDrafts_CompanyId_ParseSessionId" ON "ParsedOrderDrafts" ("CompanyId", "ParseSessionId");
CREATE INDEX IF NOT EXISTS "IX_ParsedOrderDrafts_CompanyId_ValidationStatus" ON "ParsedOrderDrafts" ("CompanyId", "ValidationStatus");
CREATE INDEX IF NOT EXISTS "IX_ParsedOrderDrafts_CreatedOrderId" ON "ParsedOrderDrafts" ("CreatedOrderId");

-- EmailMessages table
CREATE TABLE IF NOT EXISTS "EmailMessages" (
    "Id" uuid NOT NULL,
    "CompanyId" uuid NOT NULL,
    "EmailAccountId" uuid NOT NULL,
    "MessageId" character varying(500) NOT NULL,
    "FromAddress" character varying(255) NOT NULL,
    "ToAddresses" character varying(1000) NOT NULL,
    "CcAddresses" character varying(1000) NULL,
    "Subject" character varying(1000) NOT NULL,
    "BodyPreview" character varying(2000) NULL,
    "ReceivedAt" timestamp with time zone NOT NULL,
    "RawStoragePath" character varying(1000) NULL,
    "HasAttachments" boolean NOT NULL DEFAULT false,
    "ParserStatus" character varying(50) NOT NULL,
    "ParserError" character varying(2000) NULL,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT (now()),
    "CreatedById" uuid NOT NULL,
    CONSTRAINT "PK_EmailMessages" PRIMARY KEY ("Id"),
    CONSTRAINT "UQ_EmailMessages_CompanyId_MessageId" UNIQUE ("CompanyId", "MessageId")
);

CREATE INDEX IF NOT EXISTS "IX_EmailMessages_CompanyId_ParserStatus_ReceivedAt" ON "EmailMessages" ("CompanyId", "ParserStatus", "ReceivedAt");

-- ParserRules table
CREATE TABLE IF NOT EXISTS "ParserRules" (
    "Id" uuid NOT NULL,
    "CompanyId" uuid NOT NULL,
    "EmailAccountId" uuid NULL,
    "FromAddressPattern" character varying(500) NULL,
    "DomainPattern" character varying(200) NULL,
    "SubjectContains" character varying(500) NULL,
    "IsVip" boolean NOT NULL DEFAULT false,
    "TargetDepartmentId" uuid NULL,
    "TargetUserId" uuid NULL,
    "ActionType" character varying(50) NOT NULL,
    "Priority" integer NOT NULL DEFAULT 0,
    "IsActive" boolean NOT NULL DEFAULT true,
    "Description" character varying(1000) NULL,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT (now()),
    "CreatedById" uuid NOT NULL,
    "UpdatedByUserId" uuid NULL,
    CONSTRAINT "PK_ParserRules" PRIMARY KEY ("Id")
);

CREATE INDEX IF NOT EXISTS "IX_ParserRules_CompanyId_Priority_IsActive" ON "ParserRules" ("CompanyId", "Priority", "IsActive");
CREATE INDEX IF NOT EXISTS "IX_ParserRules_CompanyId_EmailAccountId_IsActive" ON "ParserRules" ("CompanyId", "EmailAccountId", "IsActive");

-- ============================================
-- NOTIFICATIONS
-- ============================================

-- Notifications table
CREATE TABLE IF NOT EXISTS "Notifications" (
    "Id" uuid NOT NULL,
    "CompanyId" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "Type" character varying(50) NOT NULL,
    "Priority" character varying(20) NOT NULL,
    "Status" character varying(20) NOT NULL,
    "Title" character varying(500) NOT NULL,
    "Message" character varying(2000) NOT NULL,
    "ActionUrl" character varying(1000) NULL,
    "ActionText" character varying(200) NULL,
    "RelatedEntityId" uuid NULL,
    "RelatedEntityType" character varying(100) NULL,
    "MetadataJson" jsonb NULL,
    "ReadAt" timestamp with time zone NULL,
    "ArchivedAt" timestamp with time zone NULL,
    "ReadByUserId" uuid NULL,
    "ExpiresAt" timestamp with time zone NULL,
    "DeliveryChannels" character varying(200) NULL,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT (now()),
    "CreatedById" uuid NOT NULL,
    CONSTRAINT "PK_Notifications" PRIMARY KEY ("Id")
);

CREATE INDEX IF NOT EXISTS "IX_Notifications_UserId_CompanyId_Status" ON "Notifications" ("UserId", "CompanyId", "Status");
CREATE INDEX IF NOT EXISTS "IX_Notifications_CompanyId_Type_CreatedAt" ON "Notifications" ("CompanyId", "Type", "CreatedAt");
CREATE INDEX IF NOT EXISTS "IX_Notifications_RelatedEntityId_RelatedEntityType" ON "Notifications" ("RelatedEntityId", "RelatedEntityType");

-- NotificationSettings table
CREATE TABLE IF NOT EXISTS "NotificationSettings" (
    "Id" uuid NOT NULL,
    "CompanyId" uuid NOT NULL,
    "UserId" uuid NULL,
    "NotificationType" character varying(50) NULL,
    "Channel" character varying(20) NOT NULL,
    "Enabled" boolean NOT NULL DEFAULT true,
    "MinimumPriority" character varying(20) NULL,
    "SoundEnabled" boolean NOT NULL DEFAULT true,
    "DesktopNotificationsEnabled" boolean NOT NULL DEFAULT true,
    "Notes" character varying(1000) NULL,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT (now()),
    "CreatedById" uuid NOT NULL,
    CONSTRAINT "PK_NotificationSettings" PRIMARY KEY ("Id")
);

CREATE INDEX IF NOT EXISTS "IX_NotificationSettings_UserId_CompanyId_NotificationType" ON "NotificationSettings" ("UserId", "CompanyId", "NotificationType");
CREATE INDEX IF NOT EXISTS "IX_NotificationSettings_CompanyId_NotificationType" ON "NotificationSettings" ("CompanyId", "NotificationType");

-- ============================================
-- FOREIGN KEY CONSTRAINTS (Optional - Add after verifying table existence)
-- ============================================

-- Note: Foreign key constraints should be added after all tables are created
-- and after verifying that referenced tables exist. Uncomment and modify as needed.

-- Example foreign keys (uncomment and adjust as needed):
-- ALTER TABLE "Partners" ADD CONSTRAINT "FK_Partners_Companies_CompanyId" 
--     FOREIGN KEY ("CompanyId") REFERENCES "Companies" ("Id") ON DELETE CASCADE;
-- 
-- ALTER TABLE "UserCompanies" ADD CONSTRAINT "FK_UserCompanies_Users_UserId" 
--     FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE;
-- 
-- ALTER TABLE "UserCompanies" ADD CONSTRAINT "FK_UserCompanies_Companies_CompanyId" 
--     FOREIGN KEY ("CompanyId") REFERENCES "Companies" ("Id") ON DELETE CASCADE;
-- 
-- ALTER TABLE "Orders" ADD CONSTRAINT "FK_Orders_Partners_PartnerId" 
--     FOREIGN KEY ("PartnerId") REFERENCES "Partners" ("Id") ON DELETE RESTRICT;
-- 
-- ALTER TABLE "Orders" ADD CONSTRAINT "FK_Orders_Buildings_BuildingId" 
--     FOREIGN KEY ("BuildingId") REFERENCES "Buildings" ("Id") ON DELETE RESTRICT;
-- 
-- ALTER TABLE "OrderStatusLogs" ADD CONSTRAINT "FK_OrderStatusLogs_Orders_OrderId" 
--     FOREIGN KEY ("OrderId") REFERENCES "Orders" ("Id") ON DELETE CASCADE;
-- 
-- ALTER TABLE "Splitters" ADD CONSTRAINT "FK_Splitters_Buildings_BuildingId" 
--     FOREIGN KEY ("BuildingId") REFERENCES "Buildings" ("Id") ON DELETE CASCADE;
-- 
-- ALTER TABLE "SplitterPorts" ADD CONSTRAINT "FK_SplitterPorts_Splitters_SplitterId" 
--     FOREIGN KEY ("SplitterId") REFERENCES "Splitters" ("Id") ON DELETE CASCADE;
-- 
-- ALTER TABLE "ScheduledSlots" ADD CONSTRAINT "FK_ScheduledSlots_Orders_OrderId" 
--     FOREIGN KEY ("OrderId") REFERENCES "Orders" ("Id") ON DELETE CASCADE;
-- 
-- ALTER TABLE "ScheduledSlots" ADD CONSTRAINT "FK_ScheduledSlots_ServiceInstallers_ServiceInstallerId" 
--     FOREIGN KEY ("ServiceInstallerId") REFERENCES "ServiceInstallers" ("Id") ON DELETE RESTRICT;
-- 
-- ALTER TABLE "SiAvailabilities" ADD CONSTRAINT "FK_SiAvailabilities_ServiceInstallers_ServiceInstallerId" 
--     FOREIGN KEY ("ServiceInstallerId") REFERENCES "ServiceInstallers" ("Id") ON DELETE CASCADE;
-- 
-- ALTER TABLE "ParseSessions" ADD CONSTRAINT "FK_ParseSessions_EmailMessages_EmailMessageId" 
--     FOREIGN KEY ("EmailMessageId") REFERENCES "EmailMessages" ("Id") ON DELETE CASCADE;
-- 
-- ALTER TABLE "ParsedOrderDrafts" ADD CONSTRAINT "FK_ParsedOrderDrafts_ParseSessions_ParseSessionId" 
--     FOREIGN KEY ("ParseSessionId") REFERENCES "ParseSessions" ("Id") ON DELETE CASCADE;

