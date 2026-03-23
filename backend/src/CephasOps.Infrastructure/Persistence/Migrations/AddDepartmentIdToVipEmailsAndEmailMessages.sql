-- Migration: Add DepartmentId to VipEmails and EmailMessages
-- Date: 2024-11-30
-- Purpose: Enable department-based filtering and routing for VIP emails and email messages

-- ============================================================================
-- Step 1: Add DepartmentId column to VipEmails
-- ============================================================================
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'VipEmails' 
        AND column_name = 'DepartmentId'
    ) THEN
        ALTER TABLE "VipEmails" ADD COLUMN "DepartmentId" uuid NULL;
        RAISE NOTICE 'Added DepartmentId column to VipEmails';
    ELSE
        RAISE NOTICE 'DepartmentId column already exists in VipEmails';
    END IF;
END $$;

-- ============================================================================
-- Step 2: Add DepartmentId column to EmailMessages
-- ============================================================================
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'EmailMessages' 
        AND column_name = 'DepartmentId'
    ) THEN
        ALTER TABLE "EmailMessages" ADD COLUMN "DepartmentId" uuid NULL;
        RAISE NOTICE 'Added DepartmentId column to EmailMessages';
    ELSE
        RAISE NOTICE 'DepartmentId column already exists in EmailMessages';
    END IF;
END $$;

-- ============================================================================
-- Step 3: Add foreign key constraints (optional but recommended)
-- ============================================================================
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
        RAISE NOTICE 'Added FK_VipEmails_Departments_DepartmentId';
    ELSE
        RAISE NOTICE 'FK_VipEmails_Departments_DepartmentId already exists';
    END IF;

    -- FK for EmailMessages.DepartmentId
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.table_constraints 
        WHERE constraint_name = 'FK_EmailMessages_Departments_DepartmentId'
    ) THEN
        ALTER TABLE "EmailMessages" 
        ADD CONSTRAINT "FK_EmailMessages_Departments_DepartmentId" 
        FOREIGN KEY ("DepartmentId") REFERENCES "Departments"("Id") ON DELETE SET NULL;
        RAISE NOTICE 'Added FK_EmailMessages_Departments_DepartmentId';
    ELSE
        RAISE NOTICE 'FK_EmailMessages_Departments_DepartmentId already exists';
    END IF;
END $$;

-- ============================================================================
-- Step 4: Create indexes for better query performance
-- ============================================================================
CREATE INDEX IF NOT EXISTS "IX_VipEmails_DepartmentId" ON "VipEmails" ("DepartmentId");
CREATE INDEX IF NOT EXISTS "IX_EmailMessages_DepartmentId" ON "EmailMessages" ("DepartmentId");

-- ============================================================================
-- Verification
-- ============================================================================
SELECT 
    'VipEmails' AS table_name,
    column_name,
    data_type
FROM information_schema.columns
WHERE table_name = 'VipEmails' AND column_name = 'DepartmentId'
UNION ALL
SELECT 
    'EmailMessages' AS table_name,
    column_name,
    data_type
FROM information_schema.columns
WHERE table_name = 'EmailMessages' AND column_name = 'DepartmentId';

