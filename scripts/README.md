# Scripts Folder

Organized utility scripts for CephasOps project.

## Folder Structure

### restart/
Quick restart scripts for development:
- **restart-all.ps1** - Restart both backend and frontend with hot reload
- **restart-backend.ps1** - Restart backend only with dotnet watch
- **restart-frontend.ps1** - Restart frontend only with Vite HMR

Usage:
```powershell
.\scripts\restart\restart-all.ps1
```

### database/
Database diagnostic and check scripts:
- quick_check.sql - Quick database health check
- check_duplicates.sql - Find duplicate records
- find_duplicates.sql - Duplicate detection
- check_si_columns.sql - Verify SI table columns
- check_current_state.sql - Current database state
- analyze_duplicates.sql - Analyze duplicate patterns

### maintenance/
Maintenance and cleanup scripts:
- cleanup-project-root-docs.ps1 - Clean up loose documentation files
- reorganize-docs.ps1 - Reorganize documentation structure
- check-email-services.ps1 - Verify email services status
- run_check.ps1 - Run system checks

### dev/
Development utility scripts:
- start-services.ps1 - Start all services
- stop-services.ps1 - Stop all services  
- reset-services.ps1 - Reset services
- delete-companies.ps1 - Delete company data
- list-companies.ps1 - List companies
- list-companies-db.ps1 - List companies from database
- delete-company-by-email.ps1 - Delete company by email

## Quick Commands

**Start everything:**
```powershell
.\scripts\restart\restart-all.ps1
```

**Check database:**
```powershell
psql ... -f scripts\database\quick_check.sql
```

**Maintenance:**
```powershell
.\scripts\maintenance\reorganize-docs.ps1
```
