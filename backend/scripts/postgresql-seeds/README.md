# PostgreSQL Seed Data Scripts

**Purpose:** Direct PostgreSQL management of all seed data (replaces C# DatabaseSeeder)

---

## Execution Order

**CRITICAL:** Execute scripts in this exact order due to foreign key dependencies:

1. `01_system_data.sql` - Foundation (Companies, Roles, Users)
2. `02_reference_data.sql` - Reference data (OrderTypes, BuildingTypes, etc.)
3. `03_master_data.sql` - Master data (Departments, Materials)
4. `04_configuration_data.sql` - Configuration (ParserTemplates, Settings)
5. `05_inventory_data.sql` - Inventory types (MovementTypes, LocationTypes)
6. `06_document_placeholders.sql` - Document placeholders (no dependencies)

---

## How to Run

### Option 1: Using psql Command Line

```bash
# Set connection details
export PGHOST=localhost
export PGPORT=5432
export PGDATABASE=cephasops
export PGUSER=postgres
export PGPASSWORD=J@saw007

# Run scripts in order
psql -f 01_system_data.sql
psql -f 02_reference_data.sql
psql -f 03_master_data.sql
psql -f 04_configuration_data.sql
psql -f 05_inventory_data.sql
psql -f 06_document_placeholders.sql
```

### Option 2: Using Connection String

```bash
psql "Host=localhost;Port=5432;Database=cephasops;Username=postgres;Password=J@saw007" -f 01_system_data.sql
```

### Option 3: Using pgAdmin or DBeaver

1. Open SQL editor
2. Load and execute each script in order
3. Verify execution success

---

## Prerequisites

1. **Database must be migrated** - All tables must exist
2. **pgcrypto extension** - Required for password hashing (auto-created in scripts)
3. **No existing seed data** - Scripts use `ON CONFLICT DO NOTHING` but verify first

---

## Verification

After running all scripts, verify data:

```sql
-- Quick verification
SELECT 
    (SELECT COUNT(*) FROM "Companies") as companies,
    (SELECT COUNT(*) FROM "Roles") as roles,
    (SELECT COUNT(*) FROM "Users") as users,
    (SELECT COUNT(*) FROM "OrderTypes") as order_types,
    (SELECT COUNT(*) FROM "Materials") as materials,
    (SELECT COUNT(*) FROM "ParserTemplates") as parser_templates,
    (SELECT COUNT(*) FROM "GlobalSettings") as global_settings,
    (SELECT COUNT(*) FROM "DocumentPlaceholderDefinitions") as document_placeholders;
```

**Expected Results:**
- Companies: 1
- Roles: 4
- Users: 2
- OrderTypes: 5
- Materials: ~50+
- ParserTemplates: 9+
- GlobalSettings: ~30+
- DocumentPlaceholderDefinitions: ~158

---

## Password Hashes

**Default Admin User:**
- Email: `simon@cephas.com.my`
- Password: `J@saw007`
- Hash: Calculated using SHA256 + salt "CephasOps_Salt_2024"

**Finance HOD User:**
- Email: `finance@cephas.com.my`
- Password: `E5pr!tg@L`
- Hash: Calculated using SHA256 + salt "CephasOps_Salt_2024"

**Note:** Password hashes are pre-calculated in the scripts. To change passwords, update the hash values in `01_system_data.sql`.

---

## Idempotency

All scripts are **idempotent** - safe to run multiple times:
- Uses `ON CONFLICT DO NOTHING` for unique constraints
- Checks for existing data before inserting
- No duplicate data will be created

---

## Troubleshooting

### Error: Extension pgcrypto does not exist
```sql
CREATE EXTENSION IF NOT EXISTS pgcrypto;
```

### Error: Foreign key constraint violation
- Ensure scripts are run in correct order
- Check that parent records exist (e.g., Company before Roles)

### Error: Duplicate key violation
- Scripts use `ON CONFLICT DO NOTHING` - this should not occur
- If it does, check for existing data and remove if needed

---

## Updating Seed Data

### Adding New Data
1. Edit appropriate SQL script
2. Add INSERT statements with `ON CONFLICT DO NOTHING`
3. Test in development
4. Commit to version control

### Updating Existing Data
1. Create new SQL file: `YYYYMMDD_Update_Description.sql`
2. Use UPDATE statements
3. Test in development
4. Run in production

---

## Version Control

All scripts are version controlled in Git. Changes should be:
- Documented in commit messages
- Tested in development first
- Reviewed before production deployment

---

**Last Updated:** 2025-01-05

