-- Add Issue and Solution fields to Orders table for Assurance orders
ALTER TABLE "Orders"
ADD COLUMN "Issue" character varying(1000) NULL,
ADD COLUMN "Solution" character varying(2000) NULL;

-- Add AdditionalContactNumber and Issue fields to ParsedOrderDrafts table
ALTER TABLE "ParsedOrderDrafts"
ADD COLUMN "AdditionalContactNumber" character varying(100) NULL,
ADD COLUMN "Issue" character varying(1000) NULL;

-- Add comments for documentation
COMMENT ON COLUMN "Orders"."Issue" IS 'Issue description for Assurance orders (e.g., "Link Down", "LOSi", "LOBi"). Extracted from email/parser.';
COMMENT ON COLUMN "Orders"."Solution" IS 'Solution/resolution for Assurance orders. Entered by SI/Admin after meeting customer (status >= MetCustomer).';
COMMENT ON COLUMN "ParsedOrderDrafts"."AdditionalContactNumber" IS 'Additional contact number for Assurance orders';
COMMENT ON COLUMN "ParsedOrderDrafts"."Issue" IS 'Issue description for Assurance orders (e.g., "Link Down", "LOSi", "LOBi")';

