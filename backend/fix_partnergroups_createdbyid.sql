-- Make CreatedById nullable in PartnerGroups table
-- This field is not currently used in the application

ALTER TABLE "PartnerGroups" 
ALTER COLUMN "CreatedById" DROP NOT NULL;

