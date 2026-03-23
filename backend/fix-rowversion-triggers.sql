-- Fix RowVersion auto-generation for PostgreSQL
-- PostgreSQL doesn't have SQL Server's rowversion type, so we need triggers

-- Create function to generate random RowVersion
CREATE OR REPLACE FUNCTION generate_rowversion()
RETURNS TRIGGER AS $$
BEGIN
    NEW."RowVersion" = gen_random_bytes(8);
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Add triggers to ParseSessions
DROP TRIGGER IF EXISTS trg_parsesessions_rowversion ON "ParseSessions";
CREATE TRIGGER trg_parsesessions_rowversion
    BEFORE INSERT OR UPDATE ON "ParseSessions"
    FOR EACH ROW
    EXECUTE FUNCTION generate_rowversion();

-- Add triggers to ParsedOrderDrafts
DROP TRIGGER IF EXISTS trg_parsedorderdrafts_rowversion ON "ParsedOrderDrafts";
CREATE TRIGGER trg_parsedorderdrafts_rowversion
    BEFORE INSERT OR UPDATE ON "ParsedOrderDrafts"
    FOR EACH ROW
    EXECUTE FUNCTION generate_rowversion();

-- Add triggers to EmailMessages
DROP TRIGGER IF EXISTS trg_emailmessages_rowversion ON "EmailMessages";
CREATE TRIGGER trg_emailmessages_rowversion
    BEFORE INSERT OR UPDATE ON "EmailMessages"
    FOR EACH ROW
    EXECUTE FUNCTION generate_rowversion();

-- Add triggers to other critical tables
DROP TRIGGER IF EXISTS trg_orders_rowversion ON "Orders";
CREATE TRIGGER trg_orders_rowversion
    BEFORE INSERT OR UPDATE ON "Orders"
    FOR EACH ROW
    EXECUTE FUNCTION generate_rowversion();

DROP TRIGGER IF EXISTS trg_invoices_rowversion ON "Invoices";
CREATE TRIGGER trg_invoices_rowversion
    BEFORE INSERT OR UPDATE ON "Invoices"
    FOR EACH ROW
    EXECUTE FUNCTION generate_rowversion();

DROP TRIGGER IF EXISTS trg_splitters_rowversion ON "Splitters";
CREATE TRIGGER trg_splitters_rowversion
    BEFORE INSERT OR UPDATE ON "Splitters"
    FOR EACH ROW
    EXECUTE FUNCTION generate_rowversion();

