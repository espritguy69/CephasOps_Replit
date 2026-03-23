-- Migration: Add new fields for Excel parser support
-- Date: 2024-11-27
-- Description: Adds OldAddress, PackageName, Bandwidth, OnuSerialNumber, VoipServiceId fields to Orders
--              and corresponding fields to ParsedOrderDrafts for TIME Excel parsing

-- =============================================
-- Add new columns to Orders table
-- =============================================
ALTER TABLE "Orders" ADD COLUMN IF NOT EXISTS "OldAddress" VARCHAR(500);
ALTER TABLE "Orders" ADD COLUMN IF NOT EXISTS "PackageName" VARCHAR(255);
ALTER TABLE "Orders" ADD COLUMN IF NOT EXISTS "Bandwidth" VARCHAR(100);
ALTER TABLE "Orders" ADD COLUMN IF NOT EXISTS "OnuSerialNumber" VARCHAR(100);
ALTER TABLE "Orders" ADD COLUMN IF NOT EXISTS "VoipServiceId" VARCHAR(100);

COMMENT ON COLUMN "Orders"."OldAddress" IS 'Old/Previous address - required for Modification/Relocation orders';
COMMENT ON COLUMN "Orders"."PackageName" IS 'Package/Plan name from partner (e.g., TIME Fibre 600Mbps)';
COMMENT ON COLUMN "Orders"."Bandwidth" IS 'Bandwidth (e.g., 600 Mbps)';
COMMENT ON COLUMN "Orders"."OnuSerialNumber" IS 'ONU Serial Number';
COMMENT ON COLUMN "Orders"."VoipServiceId" IS 'VOIP Service ID if applicable';

-- =============================================
-- Add new columns to ParsedOrderDrafts table
-- =============================================
ALTER TABLE "ParsedOrderDrafts" ADD COLUMN IF NOT EXISTS "CustomerEmail" VARCHAR(255);
ALTER TABLE "ParsedOrderDrafts" ADD COLUMN IF NOT EXISTS "OldAddress" VARCHAR(500);
ALTER TABLE "ParsedOrderDrafts" ADD COLUMN IF NOT EXISTS "OrderTypeCode" VARCHAR(100);
ALTER TABLE "ParsedOrderDrafts" ADD COLUMN IF NOT EXISTS "PackageName" VARCHAR(255);
ALTER TABLE "ParsedOrderDrafts" ADD COLUMN IF NOT EXISTS "Bandwidth" VARCHAR(100);
ALTER TABLE "ParsedOrderDrafts" ADD COLUMN IF NOT EXISTS "OnuSerialNumber" VARCHAR(100);
ALTER TABLE "ParsedOrderDrafts" ADD COLUMN IF NOT EXISTS "VoipServiceId" VARCHAR(100);
ALTER TABLE "ParsedOrderDrafts" ADD COLUMN IF NOT EXISTS "Remarks" TEXT;
ALTER TABLE "ParsedOrderDrafts" ADD COLUMN IF NOT EXISTS "SourceFileName" VARCHAR(255);

COMMENT ON COLUMN "ParsedOrderDrafts"."CustomerEmail" IS 'Customer email address';
COMMENT ON COLUMN "ParsedOrderDrafts"."OldAddress" IS 'Old/Previous address - required for Modification orders';
COMMENT ON COLUMN "ParsedOrderDrafts"."OrderTypeCode" IS 'Order type code detected from file (e.g., MODIFICATION_OUTDOOR, ACTIVATION)';
COMMENT ON COLUMN "ParsedOrderDrafts"."PackageName" IS 'Package/Plan name from partner';
COMMENT ON COLUMN "ParsedOrderDrafts"."Bandwidth" IS 'Bandwidth';
COMMENT ON COLUMN "ParsedOrderDrafts"."OnuSerialNumber" IS 'ONU Serial Number';
COMMENT ON COLUMN "ParsedOrderDrafts"."VoipServiceId" IS 'VOIP Service ID';
COMMENT ON COLUMN "ParsedOrderDrafts"."Remarks" IS 'Raw remarks from the source document';
COMMENT ON COLUMN "ParsedOrderDrafts"."SourceFileName" IS 'Source filename for tracking';

-- =============================================
-- Add indexes for commonly queried fields
-- =============================================
CREATE INDEX IF NOT EXISTS "IX_ParsedOrderDrafts_OrderTypeCode" ON "ParsedOrderDrafts" ("OrderTypeCode");
CREATE INDEX IF NOT EXISTS "IX_ParsedOrderDrafts_SourceFileName" ON "ParsedOrderDrafts" ("SourceFileName");

-- =============================================
-- Verify the changes
-- =============================================
DO $$
BEGIN
    RAISE NOTICE 'Migration 20241127_AddParserExcelFields completed successfully';
END $$;

