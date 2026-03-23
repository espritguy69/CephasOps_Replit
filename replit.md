# CephasOps Admin Portal

## Project Overview
CephasOps is a comprehensive operations platform for single-company ISP (Internet Service Provider) subcontracting workflows. Reconstructed from the `single_company` branch of the original CephasOps repo.

## Architecture

### Frontend (Admin Portal)
- **Location:** `frontend/`
- **Framework:** React 18 + TypeScript + Vite 6
- **Styling:** Tailwind CSS v4 + ShadCN UI components
- **State:** React Query (TanStack Query v5) + React Context
- **Routing:** React Router v6 with auth-protected routes
- **UI Components:** Syncfusion Enterprise (grids, charts, schedule, PDF viewer, etc.) + custom ShadCN components
- **Forms:** React Hook Form + Zod validation
- **Dev Port:** 5000 (host: 0.0.0.0, allowedHosts: true for Replit proxy)
- **API Proxy:** `/api` -> backend target (default: `localhost:5001`, override via `VITE_API_TARGET`)

### Backend (ASP.NET Core)
- **Location:** `backend/`
- **Stack:** .NET 10.0, ASP.NET Core (minimal hosting), Entity Framework Core + Npgsql (PostgreSQL)
- **Architecture:** Clean Architecture — Api → Application → Infrastructure → Domain
- **Not running in Replit** — requires .NET 10 SDK, PostgreSQL, optionally Redis
- **Entrypoint:** `backend/src/CephasOps.Api/Program.cs`
- **Dockerfile:** `infra/docker/Dockerfile` (multi-stage, exposes port 8080)
- **Docker Compose:** `infra/docker/docker-compose.yml` (api + worker + postgres + redis)
- **Migrations:** 211 EF Core migrations in `backend/src/CephasOps.Infrastructure/Persistence/Migrations/`
- **Health checks:** `/health`, `/health/ready`, `/health/platform`

### Frontend SI (Service Installer PWA)
- **Location:** `frontend-si/`
- **Stack:** React + TypeScript + Vite + Tailwind v4

### SI Mobile (Expo/React Native)
- **Location:** `si-mobile/`
- **Stack:** Expo 55 + React Native

### Documentation
- `docs/` — Architecture, modules, API contracts, data models, UI specs
- `cursor/` + `cursor-guides/` — AI-assisted code generation prompts and guides
- `environments/` — Environment variable examples per tier

## Key Files
- `frontend/src/main.tsx` — React entry point
- `frontend/src/App.tsx` — Root app with full routing
- `frontend/src/api/client.ts` — Centralized HTTP client with auth token injection
- `frontend/src/api/config.ts` — API URL resolution (VITE_API_BASE_URL > relative `/api`)
- `frontend/vite.config.ts` — Vite config with path aliases, API proxy, chunk splitting
- `backend/src/CephasOps.Api/Program.cs` — Backend entrypoint
- `backend/src/CephasOps.Api/appsettings.json` — Backend config template
- `backend/src/CephasOps.Infrastructure/Persistence/ApplicationDbContext.cs` — EF Core DbContext
- `infra/docker/Dockerfile` — Backend Docker build
- `infra/docker/docker-compose.yml` — Full stack docker-compose

## Frontend Environment Variables
- `VITE_SYNCFUSION_LICENSE_KEY` — Syncfusion Enterprise license (set in `frontend/.env`)
- `VITE_API_BASE_URL` — Override API base URL for production (e.g., `https://api.cephasops.com/api`)
- `VITE_API_TARGET` — Vite dev proxy target (default: `http://localhost:5001`)

## Backend Environment Variables

### Required (secrets)
- `ConnectionStrings__DefaultConnection` — PostgreSQL connection string
- `Jwt__Key` — JWT signing key (min 32 chars). Note: `Jwt__SecretKey` also accepted by startup validator; docker-compose uses `Jwt__SecretKey`
- `Encryption__Key` — AES encryption key (32 chars)
- `Encryption__IV` — AES initialization vector (16 chars)

### Required (non-secret)
- `ASPNETCORE_ENVIRONMENT` — `Development` / `Staging` / `Production`
- `Jwt__Issuer` — JWT issuer (default: `CephasOps`)
- `Jwt__Audience` — JWT audience (default: `CephasOps`)
- `Cors__AllowedOrigins__0` — Frontend origin URL (e.g., `https://your-replit.replit.app`)

### Optional
- `ConnectionStrings__Redis` — Redis connection (falls back to in-memory rate limiting)
- `Tenant__DefaultCompanyId` — Default company GUID for single-tenant mode
- `SYNCFUSION_LICENSE_KEY` — Backend Syncfusion license for Excel/PDF processing

### Optional Third-Party Integrations
- `Carbone__ApiKey` — Carbone.io document generation
- `WhatsAppCloudApi__AccessToken` / `PhoneNumberId` / `BusinessAccountId` — Meta WhatsApp
- `SMTP_HOST` / `SMTP_PORT` / `SMTP_USER` / `SMTP_PASS` — Email
- OneDrive, MyInvois, Twilio SMS — stored in GlobalSettings DB table

### Worker Role Toggles (for split api/worker deployment)
- `ProductionRoles__RunJobWorkers` — Background job execution
- `ProductionRoles__RunGuardian` — Platform drift detection
- `ProductionRoles__RunSchedulers` — Cron-like schedulers (email, SLA, stock, etc.)
- `ProductionRoles__RunEventDispatcher` — Domain event dispatch
- `ProductionRoles__RunNotificationWorkers` — Notification delivery
- `ProductionRoles__RunIntegrationWorkers` — Outbound HTTP retry
- `ProductionRoles__RunEmailCleanup` — Email retention cleanup
- `ProductionRoles__RunStorageLifecycle` — Storage lifecycle management
- `ProductionRoles__RunMetricsAggregation` — Tenant metrics aggregation
- `ProductionRoles__RunWatchdog` — Worker heartbeat watchdog

## Backend Deployment

### Target: Hostinger VPS (Ubuntu + Docker + Nginx)
Production compose: `infra/docker/docker-compose.prod.yml` (api + worker + postgres, all on one VPS)
Nginx config: `infra/nginx/api.conf` (reverse proxy with SSL)
Deploy script: `infra/scripts/deploy-vps.sh` (all-in-one management)

### VPS Deployment Steps
```bash
# 1. SSH into Hostinger VPS
ssh root@your-vps-ip

# 2. Install Docker
bash deploy-vps.sh install

# 3. Clone repo and create .env
bash deploy-vps.sh setup
nano /opt/cephasops/infra/docker/.env   # fill in all values

# 4. Build and start
cd /opt/cephasops
bash infra/scripts/deploy-vps.sh build
bash infra/scripts/deploy-vps.sh start

# 5. Run migrations
bash infra/scripts/deploy-vps.sh migrate

# 6. Setup Nginx + SSL
bash infra/scripts/deploy-vps.sh nginx
# Edit /etc/nginx/sites-available/cephasops-api → replace api.yourdomain.com
sudo nginx -t && sudo systemctl reload nginx
sudo certbot --nginx -d api.yourdomain.com

# 7. Verify
curl https://api.yourdomain.com/health/ready

# 8. Set frontend env in Replit
# VITE_API_BASE_URL=https://api.yourdomain.com/api
```

### VPS Management Commands
```bash
bash infra/scripts/deploy-vps.sh status      # container status + health
bash infra/scripts/deploy-vps.sh logs api     # API container logs
bash infra/scripts/deploy-vps.sh logs worker  # worker logs
bash infra/scripts/deploy-vps.sh restart      # restart all containers
bash infra/scripts/deploy-vps.sh update       # pull + rebuild + restart
bash infra/scripts/deploy-vps.sh backup-db    # backup PostgreSQL
bash infra/scripts/deploy-vps.sh restore-db <file>  # restore from backup
```

### Rollback Steps
```bash
# 1. Backup current DB before rollback
bash infra/scripts/deploy-vps.sh backup-db
# 2. Stop API + worker (keep DB running)
cd /opt/cephasops
docker compose -f infra/docker/docker-compose.prod.yml stop api worker
# 3. Restore database from previous backup
bash infra/scripts/deploy-vps.sh restore-db /opt/cephasops/backups/<file>.dump
# 4. Checkout previous version
git checkout <previous-commit>
# 5. Rebuild and start all
bash infra/scripts/deploy-vps.sh build
bash infra/scripts/deploy-vps.sh start
```

### Health Check Verification
```bash
curl -sf https://api.yourdomain.com/health/ready   # readiness
curl -sf https://api.yourdomain.com/health/platform # platform services
```

## Frontend Deployment Configuration

### Replit Frontend + External Backend (Recommended)
1. Set `VITE_API_BASE_URL` to hosted backend URL (e.g., `https://api.yourdomain.com/api`)
2. Build: `cd frontend && npm run build`
3. Deploy as static site from `frontend/dist`

### Dev Mode in Replit
1. Set `VITE_API_TARGET` to external backend URL
2. Run: `cd frontend && npm run dev` (Vite proxy routes `/api` to target)

### What Works Without Backend
- Login page UI, all component rendering, sidebar, routing

### What Requires Backend
- Authentication, all data pages, dashboard, file uploads, email, reports, admin

## Workflow
- **Start application:** `cd frontend && npm run dev` → port 5000 (webview)

## Deployment
- **Target:** Static site
- **Build:** `cd frontend && npm run build`
- **Public directory:** `frontend/dist`

## Implemented Frontend Modules
- Auth (Login, Change Password, Forgot/Reset Password)
- Dashboard, Orders, Scheduler, Parser
- Inventory (Dashboard, List, Stock Summary, Ledger, Receive, Transfer, Allocate, Issue, Return, Reports)
- RMA, Billing, Payroll, P&L, Accounting
- Operations (Dockets, Installer Payout Breakdown)
- Settings (Email, Materials, Templates, Company, Departments, KPI, Rate Plans, Users, RBAC, Buildings)
- Workflow (Definitions, Guard Conditions, Side Effects)
- Tasks, Notifications, Documents, Buildings, Reports Hub
