-- Add Code column to Partners if missing (fixes 42703: column p.Code does not exist).
-- Safe to run multiple times.
DO $$
BEGIN
  IF NOT EXISTS (
    SELECT 1 FROM information_schema.columns
    WHERE table_schema = 'public' AND table_name = 'Partners' AND column_name = 'Code'
  ) THEN
    ALTER TABLE "Partners" ADD COLUMN "Code" character varying(50) NULL;
  END IF;
END $$;
