# Phase 2 Settings Migrations

This directory contains database migration scripts for Phase 1 and Phase 2 Settings entities.

## Migration Files

### Phase 1 Settings Entities
- **`AddPhase1SettingsEntities.sql`** - Creates tables for:
  - `SlaProfiles` - SLA configuration profiles
  - `automation_rules` - Automation rules for auto-assignment and escalation

### Phase 2 Settings Entities
- **`AddPhase2SettingsEntities.sql`** - Creates tables for:
  - `approval_workflows` - Multi-step approval workflows
  - `approval_steps` - Individual steps within approval workflows
  - `business_hours` - Operating hours configuration
  - `public_holidays` - Public holidays for SLA calculations
  - `escalation_rules` - Auto-escalation rules

## Applying Migrations

### Option 1: Using PowerShell Script (Recommended)

```powershell
cd backend/scripts
.\apply-phase2-migrations.ps1
```

The script will:
1. Apply Phase 1 Settings migrations (if not already applied)
2. Apply Phase 2 Settings migrations
3. Report success/failure for each step

### Option 2: Manual Application

#### Using psql command line:

```bash
# Phase 1 Settings
psql -h db.jgahsbfoydwdgipcjvxe.supabase.co -p 5432 -U postgres -d postgres -f AddPhase1SettingsEntities.sql

# Phase 2 Settings
psql -h db.jgahsbfoydwdgipcjvxe.supabase.co -p 5432 -U postgres -d postgres -f AddPhase2SettingsEntities.sql
```

#### Using pgAdmin or other PostgreSQL client:
1. Open the SQL file
2. Execute the entire script
3. Verify all tables were created

## Verifying Migrations

Run the verification script to check that all tables and indexes were created:

```bash
psql -h db.jgahsbfoydwdgipcjvxe.supabase.co -p 5432 -U postgres -d postgres -f ../../scripts/verify-phase2-migrations.sql
```

Or using PowerShell:

```powershell
cd backend/scripts
$env:PGPASSWORD = "J@saw007"
psql -h db.jgahsbfoydwdgipcjvxe.supabase.co -p 5432 -U postgres -d postgres -f verify-phase2-migrations.sql
```

## Tables Created

### Phase 1
1. **SlaProfiles** - 22 columns, 3 indexes
2. **automation_rules** - 28 columns, 7 indexes

### Phase 2
1. **approval_workflows** - 22 columns, 5 indexes
2. **approval_steps** - 18 columns, 3 indexes
3. **business_hours** - 25 columns, 4 indexes
4. **public_holidays** - 12 columns, 6 indexes
5. **escalation_rules** - 30 columns, 7 indexes

## Important Notes

1. **Idempotent**: All migrations use `CREATE TABLE IF NOT EXISTS` and `CREATE INDEX IF NOT EXISTS`, so they can be run multiple times safely.

2. **Foreign Keys**: 
   - `approval_steps.ApprovalWorkflowId` → `approval_workflows.Id` (CASCADE DELETE)

3. **Default Values**: 
   - All tables have default values for boolean flags
   - Timestamps default to `now()`
   - JSON fields default to empty arrays `[]`

4. **Indexes**: 
   - All tables have indexes on `CompanyId` for multi-tenant isolation
   - Composite indexes for common query patterns
   - Indexes on date ranges for effective date filtering

## Rollback

If you need to rollback these migrations, you can drop the tables:

```sql
-- Phase 2 Settings (drop in reverse order due to foreign keys)
DROP TABLE IF EXISTS "escalation_rules" CASCADE;
DROP TABLE IF EXISTS "public_holidays" CASCADE;
DROP TABLE IF EXISTS "business_hours" CASCADE;
DROP TABLE IF EXISTS "approval_steps" CASCADE;
DROP TABLE IF EXISTS "approval_workflows" CASCADE;

-- Phase 1 Settings
DROP TABLE IF EXISTS "automation_rules" CASCADE;
DROP TABLE IF EXISTS "SlaProfiles" CASCADE;
```

**⚠️ WARNING**: Dropping tables will permanently delete all data. Make sure to backup your database first!

## Next Steps

After applying migrations:

1. **Verify Tables**: Run the verification script
2. **Test Backend**: Start the backend and verify no EF Core errors
3. **Test Frontend**: Navigate to Settings pages and verify they load
4. **Create Test Data**: Add sample records through the UI to verify CRUD operations

## Troubleshooting

### Error: "relation already exists"
- This is normal if migrations were already applied
- The `IF NOT EXISTS` clauses prevent errors
- You can safely ignore these messages

### Error: "permission denied"
- Ensure you're using the correct database user credentials
- Check that the user has CREATE TABLE and CREATE INDEX permissions

### Error: "column does not exist"
- Verify the migration script ran completely
- Check that all tables were created using the verification script
- Re-run the migration if needed

## Support

For issues or questions:
1. Check the verification script output
2. Review the migration SQL files for syntax errors
3. Check PostgreSQL logs for detailed error messages

