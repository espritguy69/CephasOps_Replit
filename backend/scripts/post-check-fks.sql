SELECT
  c.conname AS fk_name,
  (SELECT relname FROM pg_class WHERE oid = c.conrelid) AS from_table,
  (SELECT relname FROM pg_class WHERE oid = c.confrelid) AS to_table
FROM pg_constraint c
JOIN pg_namespace n ON n.oid = c.connamespace
WHERE n.nspname = 'public'
  AND c.contype = 'f'
  AND (
    c.conrelid IN (
      'public."ConnectorDefinitions"'::regclass,
      'public."ConnectorEndpoints"'::regclass,
      'public."ExternalIdempotencyRecords"'::regclass,
      'public."OutboundIntegrationAttempts"'::regclass
    )
    OR c.confrelid IN (
      'public."ConnectorDefinitions"'::regclass,
      'public."ConnectorEndpoints"'::regclass,
      'public."ExternalIdempotencyRecords"'::regclass,
      'public."OutboundIntegrationAttempts"'::regclass
    )
  )
ORDER BY from_table, to_table;
