-- Read-only verification for migration 20260313025530_EnsureEmailMessageBodyAndErrorColumnsAreText.
-- Run after applying the migration in each environment (Development, Staging, Production).
-- Expect: migration row present; all four columns data_type = 'text', character_maximum_length IS NULL.

\echo '=== Migration 20260313025530 in __EFMigrationsHistory? ==='
SELECT "MigrationId", "ProductVersion"
FROM "__EFMigrationsHistory"
WHERE "MigrationId" = '20260313025530_EnsureEmailMessageBodyAndErrorColumnsAreText';

\echo ''
\echo '=== EmailMessages: BodyText, BodyHtml, BodyPreview, ParserError column types ==='
SELECT column_name, data_type, character_maximum_length
FROM information_schema.columns
WHERE table_schema = 'public'
  AND table_name = 'EmailMessages'
  AND column_name IN ('BodyText', 'BodyHtml', 'BodyPreview', 'ParserError')
ORDER BY column_name;

\echo ''
\echo '=== Pass: Exactly one migration row; all four columns data_type = text, character_maximum_length IS NULL ==='
\echo '=== Fail: No migration row, or any column character varying(2000) → do not promote ==='
