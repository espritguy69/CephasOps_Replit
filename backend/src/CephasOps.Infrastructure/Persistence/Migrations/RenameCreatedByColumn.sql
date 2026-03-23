-- Rename CreatedByUserId to CreatedById to match entity definition
-- Date: 2025-11-23

ALTER TABLE "Files" RENAME COLUMN "CreatedByUserId" TO "CreatedById";

