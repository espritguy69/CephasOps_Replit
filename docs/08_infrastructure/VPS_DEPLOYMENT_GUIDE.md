# VPS Deployment Guide (Native — No Docker)

**Date:** April 2026  
**Target OS:** Debian 13 (Hostinger VPS)  
**Script:** `infra/scripts/deploy-vps-native.sh`

---

## Prerequisites

- Debian 13 VPS with root/sudo access
- Domain name configured (DNS A record pointing to VPS IP)
- SSH access

---

## 1. Deployment Phases

The deployment script (`deploy-vps-native.sh`) runs in discrete phases. Execute them in order:

```bash
bash deploy-vps-native.sh install     # Phase 1: System packages
bash deploy-vps-native.sh setup-db    # Phase 2: PostgreSQL user + database
bash deploy-vps-native.sh setup       # Phase 3: Clone repo + generate .env
bash deploy-vps-native.sh migrate     # Phase 4: Schema + seeds
bash deploy-vps-native.sh build-backend   # Phase 5: .NET publish
bash deploy-vps-native.sh build-frontend  # Phase 6: Frontend builds
bash deploy-vps-native.sh setup-service   # Phase 7: systemd service
bash deploy-vps-native.sh setup-nginx     # Phase 8: Nginx reverse proxy
bash deploy-vps-native.sh setup-ssl       # Phase 9: Let's Encrypt (optional)
```

---

## 2. Packages Installed (Phase 1: `install`)

| Category | Packages |
|----------|----------|
| **Runtimes** | .NET 10 SDK (via Microsoft repo), Node.js 22 (via NodeSource) |
| **Database** | `postgresql`, `postgresql-contrib` |
| **Web Server** | `nginx` |
| **Utilities** | `git`, `curl`, `wget`, `gnupg`, `apt-transport-https`, `ca-certificates` |
| **SSL** | `certbot`, `python3-certbot-nginx` |
| **System Libraries** | `libicu72`, `libssl3`, `zlib1g` |

---

## 3. Required Environment Variables

Generated in `/opt/cephasops/.env` during `setup` phase. **Must be edited manually after generation.**

| Variable | Description | Example |
|----------|-------------|---------|
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string | `"Host=localhost;Port=5432;Database=cephasops;Username=cephasops_app;Password=SECURE_PASSWORD"` |
| `ASPNETCORE_ENVIRONMENT` | Runtime environment | `Production` |
| `Jwt__Key` | JWT signing key (64+ chars) | `your-secure-64-char-key-here...` |
| `Jwt__Issuer` | JWT issuer | `CephasOps` |
| `Jwt__Audience` | JWT audience | `CephasOpsUsers` |
| `Encryption__Key` | Data encryption key (32 chars) | `your-32-char-encryption-key` |
| `Encryption__IV` | Encryption IV (16 bytes) | `your-16-byte-iv` |
| `SYNCFUSION_LICENSE_KEY` | Syncfusion component license | `your-syncfusion-key` |
| `Cors__AllowedOrigins__0` | First allowed CORS origin | `https://yourdomain.com` |
| `Carbone__ApiKey` | Carbone reporting API key | `your-carbone-key` |

> **IMPORTANT:** Values containing semicolons (like connection strings) MUST be quoted in the `.env` file:  
> `ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=cephasops;..."`

---

## 4. File System Layout

```
/opt/cephasops/
├── .env                    # Environment variables (chmod 600)
├── app/                    # Git repository clone
├── publish/                # .NET published output (API DLL)
├── frontend-dist/          # Admin Portal static files
├── si-dist/                # SI App static files
└── logs/                   # Application logs
    ├── api-stdout.log
    ├── api-stderr.log
    └── cephasops-*.log     # Serilog structured logs (7-day retention)
```

---

## 5. Database Setup (Phase 2: `setup-db`)

Creates:
- PostgreSQL user: `cephasops_app` (with password)
- Database: `cephasops` (owned by `cephasops_app`)

---

## 6. Migration (Phase 4: `migrate`)

Runs in order:
1. `infra/scripts/pre-migration.sql` — Pre-migration setup
2. `backend/scripts/migrations-idempotent-latest.sql` — Full schema (idempotent)
3. All seed files from `backend/scripts/postgresql-seeds/`:
   - `seed-roles.sql`
   - `seed-permissions.sql`
   - `seed-role-permissions.sql`
   - `seed-order-statuses.sql`
   - `seed-workflow-definitions.sql`
   - `seed-system-settings.sql`
   - `seed-super-admin.sql`

> **Known Issue:** The migrate phase currently uses `ON_ERROR_STOP=0`. See `docs/11_known_gaps/DATABASE_GAPS.md#DG-4` for details.

---

## 7. Systemd Service (Phase 7: `setup-service`)

Service file: `/etc/systemd/system/cephasops-api.service`

| Setting | Value |
|---------|-------|
| **User** | `www-data` |
| **WorkingDirectory** | `/opt/cephasops/publish` |
| **Command** | `/usr/bin/dotnet CephasOps.Api.dll` |
| **EnvironmentFile** | `/opt/cephasops/.env` |
| **Restart** | `always` (10s delay) |
| **Logging** | stdout/stderr → `/opt/cephasops/logs/` |

### Common Commands

```bash
sudo systemctl start cephasops-api
sudo systemctl stop cephasops-api
sudo systemctl restart cephasops-api
sudo systemctl status cephasops-api
sudo journalctl -u cephasops-api -f    # Follow logs
```

---

## 8. Nginx Configuration (Phase 8: `setup-nginx`)

Config: `/etc/nginx/sites-available/cephasops`

| Path | Handler | Backend |
|------|---------|---------|
| `/` | Static files | `/opt/cephasops/frontend-dist` (SPA, `try_files $uri /index.html`) |
| `/si/` | Static files | `/opt/cephasops/si-dist/` (SI App alias) |
| `/api/` | Reverse proxy | `127.0.0.1:8080` |
| `/hubs/` | WebSocket proxy | `127.0.0.1:8080` (SignalR with Upgrade headers) |

Features:
- Gzip compression for text, CSS, JS
- Proper `X-Forwarded-For` and `X-Forwarded-Proto` headers
- WebSocket support for SignalR

---

## 9. SSL Setup (Phase 9: `setup-ssl`)

Uses Let's Encrypt via Certbot:

```bash
bash deploy-vps-native.sh setup-ssl
```

Certbot automatically:
- Obtains certificates for your domain
- Configures Nginx for HTTPS
- Sets up auto-renewal via systemd timer

---

## 10. Updating After Code Changes

```bash
cd /opt/cephasops/app
git fetch origin && git reset --hard origin/main
source /opt/cephasops/.env
bash infra/scripts/deploy-vps-native.sh build-backend
bash infra/scripts/deploy-vps-native.sh build-frontend
sudo systemctl restart cephasops-api
```

If schema changes were made:
```bash
bash infra/scripts/deploy-vps-native.sh migrate
```

---

## 11. Troubleshooting

| Issue | Check |
|-------|-------|
| API not starting | `sudo systemctl status cephasops-api` → check logs |
| 502 Bad Gateway | Is the API process running on port 8080? |
| Database connection failure | Verify `.env` connection string is quoted correctly |
| Frontend 404 errors | Check `/opt/cephasops/frontend-dist/index.html` exists |
| SignalR not connecting | Check Nginx has WebSocket upgrade headers configured |
