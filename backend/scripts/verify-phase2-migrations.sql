-- Verification script for Phase 2 Settings migrations
-- Run this to verify all tables and indexes were created correctly

-- ============================================
-- PHASE 1 SETTINGS ENTITIES
-- ============================================

-- Verify SLA Profiles
SELECT 
    'SLA Profiles' AS "Table",
    COUNT(*) AS "Row Count",
    (SELECT COUNT(*) FROM information_schema.columns WHERE table_name = 'SlaProfiles') AS "Column Count",
    (SELECT COUNT(*) FROM pg_indexes WHERE tablename = 'SlaProfiles') AS "Index Count"
FROM "SlaProfiles";

-- Verify Automation Rules
SELECT 
    'Automation Rules' AS "Table",
    COUNT(*) AS "Row Count",
    (SELECT COUNT(*) FROM information_schema.columns WHERE table_name = 'automation_rules') AS "Column Count",
    (SELECT COUNT(*) FROM pg_indexes WHERE tablename = 'automation_rules') AS "Index Count"
FROM "automation_rules";

-- ============================================
-- PHASE 2 SETTINGS ENTITIES
-- ============================================

-- Verify Approval Workflows
SELECT 
    'Approval Workflows' AS "Table",
    COUNT(*) AS "Row Count",
    (SELECT COUNT(*) FROM information_schema.columns WHERE table_name = 'approval_workflows') AS "Column Count",
    (SELECT COUNT(*) FROM pg_indexes WHERE tablename = 'approval_workflows') AS "Index Count"
FROM "approval_workflows";

-- Verify Approval Steps
SELECT 
    'Approval Steps' AS "Table",
    COUNT(*) AS "Row Count",
    (SELECT COUNT(*) FROM information_schema.columns WHERE table_name = 'approval_steps') AS "Column Count",
    (SELECT COUNT(*) FROM pg_indexes WHERE tablename = 'approval_steps') AS "Index Count"
FROM "approval_steps";

-- Verify Business Hours
SELECT 
    'Business Hours' AS "Table",
    COUNT(*) AS "Row Count",
    (SELECT COUNT(*) FROM information_schema.columns WHERE table_name = 'business_hours') AS "Column Count",
    (SELECT COUNT(*) FROM pg_indexes WHERE tablename = 'business_hours') AS "Index Count"
FROM "business_hours";

-- Verify Public Holidays
SELECT 
    'Public Holidays' AS "Table",
    COUNT(*) AS "Row Count",
    (SELECT COUNT(*) FROM information_schema.columns WHERE table_name = 'public_holidays') AS "Column Count",
    (SELECT COUNT(*) FROM pg_indexes WHERE tablename = 'public_holidays') AS "Index Count"
FROM "public_holidays";

-- Verify Escalation Rules
SELECT 
    'Escalation Rules' AS "Table",
    COUNT(*) AS "Row Count",
    (SELECT COUNT(*) FROM information_schema.columns WHERE table_name = 'escalation_rules') AS "Column Count",
    (SELECT COUNT(*) FROM pg_indexes WHERE tablename = 'escalation_rules') AS "Index Count"
FROM "escalation_rules";

-- ============================================
-- DETAILED TABLE STRUCTURE
-- ============================================

-- List all columns for each table
SELECT 
    table_name,
    column_name,
    data_type,
    is_nullable,
    column_default
FROM information_schema.columns
WHERE table_name IN (
    'SlaProfiles',
    'automation_rules',
    'approval_workflows',
    'approval_steps',
    'business_hours',
    'public_holidays',
    'escalation_rules'
)
ORDER BY table_name, ordinal_position;

-- List all indexes
SELECT 
    tablename,
    indexname,
    indexdef
FROM pg_indexes
WHERE tablename IN (
    'SlaProfiles',
    'automation_rules',
    'approval_workflows',
    'approval_steps',
    'business_hours',
    'public_holidays',
    'escalation_rules'
)
ORDER BY tablename, indexname;

