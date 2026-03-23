-- Add UpdatedAt column to PartnerGroups table
-- This column was missing from the original migration

ALTER TABLE "PartnerGroups" 
ADD COLUMN IF NOT EXISTS "UpdatedAt" timestamp with time zone NOT NULL DEFAULT now();

-- Update existing records to have the same timestamp as CreatedAt
UPDATE "PartnerGroups" 
SET "UpdatedAt" = "CreatedAt" 
WHERE "UpdatedAt" IS NULL OR "UpdatedAt" < "CreatedAt";

