START TRANSACTION;
ALTER TABLE "Files" ADD "OneDriveFileId" character varying(500);

ALTER TABLE "Files" ADD "OneDriveWebUrl" character varying(1000);

ALTER TABLE "Files" ADD "OneDriveSyncStatus" character varying(50) NOT NULL DEFAULT 'NotSynced';

ALTER TABLE "Files" ADD "OneDriveSyncedAt" timestamp with time zone;

ALTER TABLE "Files" ADD "OneDriveSyncError" character varying(2000);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20251209080853_AddOneDriveFieldsToFile', '10.0.0');

COMMIT;

