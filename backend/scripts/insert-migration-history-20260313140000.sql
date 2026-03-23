INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260313140000_AddEnterpriseSaaSColumnsAndTenantActivity', '10.0.0')
ON CONFLICT ("MigrationId") DO NOTHING;
