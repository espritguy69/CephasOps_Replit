-- Add lockout columns to Users if missing (20260308200000_AddLockoutFieldsToUser)
DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = current_schema() AND table_name = 'Users' AND column_name = 'FailedLoginAttempts') THEN
    ALTER TABLE "Users" ADD COLUMN "FailedLoginAttempts" integer NOT NULL DEFAULT 0;
  END IF;
  IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = current_schema() AND table_name = 'Users' AND column_name = 'LockoutEndUtc') THEN
    ALTER TABLE "Users" ADD COLUMN "LockoutEndUtc" timestamp with time zone NULL;
  END IF;
END $$;

-- Add LastLoginAtUtc and MustChangePassword if missing (20260308180000_AddLastLoginAndMustChangePasswordToUser)
DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = current_schema() AND table_name = 'Users' AND column_name = 'LastLoginAtUtc') THEN
    ALTER TABLE "Users" ADD COLUMN "LastLoginAtUtc" timestamp with time zone NULL;
  END IF;
  IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = current_schema() AND table_name = 'Users' AND column_name = 'MustChangePassword') THEN
    ALTER TABLE "Users" ADD COLUMN "MustChangePassword" boolean NOT NULL DEFAULT false;
  END IF;
END $$;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
SELECT '20260308200000_AddLockoutFieldsToUser', '10.0.0'
WHERE NOT EXISTS (SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260308200000_AddLockoutFieldsToUser');

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
SELECT '20260308180000_AddLastLoginAndMustChangePasswordToUser', '10.0.0'
WHERE NOT EXISTS (SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260308180000_AddLastLoginAndMustChangePasswordToUser');
