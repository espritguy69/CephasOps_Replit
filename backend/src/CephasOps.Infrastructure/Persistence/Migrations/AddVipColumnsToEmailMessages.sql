-- Migration: Add VIP columns to EmailMessages table
-- Date: 2025-11-26
-- Description: Adds IsVip, MatchedRuleId, and MatchedVipEmailId columns for VIP email tracking

-- Add VIP columns to EmailMessages
ALTER TABLE "EmailMessages" ADD COLUMN IF NOT EXISTS "IsVip" boolean NOT NULL DEFAULT false;
ALTER TABLE "EmailMessages" ADD COLUMN IF NOT EXISTS "MatchedRuleId" uuid NULL;
ALTER TABLE "EmailMessages" ADD COLUMN IF NOT EXISTS "MatchedVipEmailId" uuid NULL;

-- Add index for VIP emails
CREATE INDEX IF NOT EXISTS "IX_EmailMessages_CompanyId_IsVip" ON "EmailMessages" ("CompanyId", "IsVip");

