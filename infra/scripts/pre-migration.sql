-- Pre-migration: Create tables referenced by apply-all-migrations.sql but
-- defined in separate migration SQL files that were not consolidated.
-- This script is idempotent (safe to run multiple times).

CREATE EXTENSION IF NOT EXISTS pgcrypto;

-- Create __EFMigrationsHistory if it doesn't exist (needed for first run)
CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

-- Create InstallationMethods table (referenced by AddRateEngineAndInstallationMethod migration)
CREATE TABLE IF NOT EXISTS "InstallationMethods" (
    "Id" UUID PRIMARY KEY,
    "CompanyId" UUID NULL,
    "Name" VARCHAR(100) NOT NULL,
    "Code" VARCHAR(50) NOT NULL,
    "Category" VARCHAR(50) NULL,
    "Description" VARCHAR(1000) NULL,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "DisplayOrder" INTEGER NOT NULL DEFAULT 0,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_InstallationMethods_CompanyId_Code"
    ON "InstallationMethods" ("CompanyId", "Code");

CREATE INDEX IF NOT EXISTS "IX_InstallationMethods_CompanyId_IsActive"
    ON "InstallationMethods" ("CompanyId", "IsActive");

CREATE INDEX IF NOT EXISTS "IX_InstallationMethods_CompanyId_Category"
    ON "InstallationMethods" ("CompanyId", "Category");

CREATE INDEX IF NOT EXISTS "IX_InstallationMethods_DisplayOrder"
    ON "InstallationMethods" ("DisplayOrder");

-- Seed default installation methods
INSERT INTO "InstallationMethods" ("Id", "CompanyId", "Name", "Code", "Category", "Description", "IsActive", "DisplayOrder", "CreatedAt")
VALUES
    (gen_random_uuid(), NULL, 'Prelaid', 'PRELAID', 'FTTH',
     'Fibre already laid by building builder/management. Minimal materials needed.',
     TRUE, 1, CURRENT_TIMESTAMP),
    (gen_random_uuid(), NULL, 'Non-prelaid (MDU / old building)', 'NON_PRELAID', 'FTTH',
     'Multi-dwelling units and old buildings. Full infrastructure pack required.',
     TRUE, 2, CURRENT_TIMESTAMP),
    (gen_random_uuid(), NULL, 'SDU / RDF Pole', 'SDU_RDF', 'FTTH',
     'Single dwelling units and pole-based installations.',
     TRUE, 3, CURRENT_TIMESTAMP)
ON CONFLICT DO NOTHING;
