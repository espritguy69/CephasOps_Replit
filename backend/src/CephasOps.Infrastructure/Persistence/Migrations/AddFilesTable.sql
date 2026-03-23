-- Migration: Add Files Table
-- Description: Creates the Files table for file upload/download functionality
-- Date: 2025-11-23

-- Create Files table
CREATE TABLE IF NOT EXISTS "Files" (
    "Id" uuid NOT NULL,
    "CompanyId" uuid NOT NULL,
    "FileName" character varying(500) NOT NULL,
    "StoragePath" character varying(1000) NOT NULL,
    "ContentType" character varying(100) NOT NULL,
    "SizeBytes" bigint NOT NULL,
    "Checksum" character varying(100) NULL,
    "CreatedById" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "Module" character varying(50) NULL,
    "EntityId" uuid NULL,
    "EntityType" character varying(50) NULL,
    CONSTRAINT "PK_Files" PRIMARY KEY ("Id")
);

-- Create indexes
CREATE INDEX IF NOT EXISTS "IX_Files_CompanyId_Id" ON "Files" ("CompanyId", "Id");
CREATE INDEX IF NOT EXISTS "IX_Files_CompanyId_EntityId_EntityType" ON "Files" ("CompanyId", "EntityId", "EntityType");
CREATE INDEX IF NOT EXISTS "IX_Files_CreatedAt" ON "Files" ("CreatedAt");

-- Add foreign key constraint to Companies (if Companies table exists)
-- ALTER TABLE "Files" ADD CONSTRAINT "FK_Files_Companies_CompanyId" 
--     FOREIGN KEY ("CompanyId") REFERENCES "Companies" ("Id") ON DELETE CASCADE;

-- Add foreign key constraint to Users (if Users table exists)
-- ALTER TABLE "Files" ADD CONSTRAINT "FK_Files_Users_CreatedById" 
--     FOREIGN KEY ("CreatedById") REFERENCES "Users" ("Id") ON DELETE RESTRICT;

