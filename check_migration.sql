-- Check if migration is in history
SELECT "MigrationId" FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251207154424_AddOrderStatusChecklist';

-- Check if tables exist
SELECT table_name 
FROM information_schema.tables 
WHERE table_schema = 'public' 
  AND table_name IN ('OrderStatusChecklistItems', 'OrderStatusChecklistAnswers');

