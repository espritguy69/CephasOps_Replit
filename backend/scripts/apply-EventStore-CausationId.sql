-- Idempotent: add CausationId to EventStore (Phase 1 distributed platform)
DO $$
BEGIN
  IF NOT EXISTS (
    SELECT 1 FROM information_schema.columns
    WHERE table_schema = current_schema() AND table_name = 'EventStore' AND column_name = 'CausationId'
  ) THEN
    ALTER TABLE "EventStore" ADD COLUMN "CausationId" uuid NULL;
  END IF;
END $$;

-- Record migration so EF sees it as applied
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260309200000_AddEventStoreCausationId', '10.0.3')
ON CONFLICT ("MigrationId") DO NOTHING;
