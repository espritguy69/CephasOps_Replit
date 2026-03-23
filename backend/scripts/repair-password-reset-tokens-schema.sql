-- Idempotent repair: create PasswordResetTokens table and indexes if missing.
-- Use when __EFMigrationsHistory has a gap (e.g. 20260308163857 and 20260311120000 applied
-- but 20260308190000_AddPasswordResetTokens was never run or is not in the migration chain).
-- Safe to run multiple times. Does not insert into __EFMigrationsHistory.

CREATE TABLE IF NOT EXISTS "PasswordResetTokens" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "TokenHash" text NOT NULL,
    "ExpiresAtUtc" timestamp with time zone NOT NULL,
    "UsedAtUtc" timestamp with time zone NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_PasswordResetTokens" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_PasswordResetTokens_Users_UserId" FOREIGN KEY ("UserId")
        REFERENCES "Users" ("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_PasswordResetTokens_UserId"
ON "PasswordResetTokens" ("UserId");

CREATE INDEX IF NOT EXISTS "IX_PasswordResetTokens_TokenHash"
ON "PasswordResetTokens" ("TokenHash");
