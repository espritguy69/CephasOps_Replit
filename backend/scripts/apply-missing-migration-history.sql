-- Insert migration rows so idempotent script skips already-applied schema (Phase7 script, etc.)
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
SELECT '20260309065950_VerifyNoPending', '10.0.0'
WHERE NOT EXISTS (SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260309065950_VerifyNoPending');

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
SELECT '20260309070232_SyncSnapshotCheck', '10.0.0'
WHERE NOT EXISTS (SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260309070232_SyncSnapshotCheck');
