SELECT
  t.table_name,
  (SELECT count(*) FROM pg_indexes i WHERE i.schemaname = 'public' AND i.tablename = t.table_name) AS index_count
FROM information_schema.tables t
WHERE t.table_schema = 'public'
  AND t.table_name IN (
    'ConnectorDefinitions',
    'ConnectorEndpoints',
    'ExternalIdempotencyRecords',
    'OutboundIntegrationAttempts'
  )
ORDER BY t.table_name;
