-- Record migration as applied when tables were created by apply-OperationalInsights-And-FeatureFlags.sql
-- Run once; then: dotnet ef database update
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
SELECT '20260310065356_AddOperationalInsightsAndFeatureFlags', '10.0.3'
WHERE NOT EXISTS (SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260310065356_AddOperationalInsightsAndFeatureFlags');
