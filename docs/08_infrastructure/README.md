# 🏗️ Infrastructure Documentation

This folder contains all infrastructure, DevOps, deployment, and operational guides for CephasOps.

---

## 📚 Available Guides

### Development & Operations

| Document | Purpose | When to Use |
|----------|---------|-------------|
| **[PC_SYNC_GUIDE.md](./PC_SYNC_GUIDE.md)** | Complete guide for syncing work between multiple development PCs | When setting up a new PC or syncing daily work |
| **[SYNC_QUICK_REFERENCE.md](./SYNC_QUICK_REFERENCE.md)** | One-page quick reference for PC sync commands | Keep handy for daily use, print if needed |
| **[SYNC_TROUBLESHOOTING.md](./SYNC_TROUBLESHOOTING.md)** | Solutions to common sync problems | When sync fails or services won't start |
| **[FAST_RESTART_GUIDE.md](./FAST_RESTART_GUIDE.md)** | Quick restart strategies for backend and frontend during development | When you need to restart services efficiently |
| **[HOW_TO_USE_FAST_RESTART.md](./HOW_TO_USE_FAST_RESTART.md)** | Practical usage instructions for fast restart | Daily development workflow |
| **[TESTING_SETUP.md](./TESTING_SETUP.md)** | Setting up testing infrastructure | When configuring test environments |

### Deployment & Versioning

| Document | Purpose | When to Use |
|----------|---------|-------------|
| **[VERSIONING_STRATEGY.md](./VERSIONING_STRATEGY.md)** | SemVer strategy and release process | Before creating a release |
| **[VERSIONING_QUICK_START.md](./VERSIONING_QUICK_START.md)** | Quick reference for versioning | Daily version management |

### Infrastructure Components

| Document | Purpose | When to Use |
|----------|---------|-------------|
| **[background_jobs_infrastructure.md](./background_jobs_infrastructure.md)** | Background job processing setup | When implementing scheduled tasks |
| **[SERVER_STATUS.md](./SERVER_STATUS.md)** | Server health monitoring | Checking system status |

### Implementation Summaries

| Document | Purpose |
|----------|---------|
| **[FAST_RESTART_IMPLEMENTATION_SUMMARY.md](./FAST_RESTART_IMPLEMENTATION_SUMMARY.md)** | Technical details of fast restart implementation |

---

## 🚀 Quick Start Commands

### Daily Development Sync

```powershell
# Full sync (code + dependencies + database)
.\sync-pc.ps1

# Quick code-only sync
.\quick-code-sync.ps1

# Quick database-only sync
.\quick-db-sync.ps1
```

### Starting Services

```powershell
# Backend with hot reload
cd backend
dotnet watch run --project src/CephasOps.Api

# Frontend with Vite HMR
cd frontend
npm run dev

# Or use automated scripts
.\backend\start.ps1
.\frontend\start.ps1
```

---

## 📋 Common Scenarios

### Scenario 1: First Time Setup on New PC

1. Clone repository
2. Install prerequisites (.NET 10, Node.js, PostgreSQL)
3. Configure environment secrets
4. Run `.\sync-pc.ps1`
5. Start services

**See**: [PC_SYNC_GUIDE.md](./PC_SYNC_GUIDE.md) for detailed steps

---

### Scenario 2: Daily Work on Second PC

1. Pull latest code: `.\quick-code-sync.ps1`
2. If migrations exist: `.\quick-db-sync.ps1`
3. Start with watch mode (auto-reload)

**See**: [FAST_RESTART_GUIDE.md](./FAST_RESTART_GUIDE.md)

---

### Scenario 3: After Pulling Major Changes

1. Full sync: `.\sync-pc.ps1`
2. Verify environment settings
3. Test startup

---

### Scenario 4: Creating a Release

1. Review [VERSIONING_STRATEGY.md](./VERSIONING_STRATEGY.md)
2. Update version numbers
3. Create release tag
4. Deploy

---

## 🔧 Troubleshooting

### Services Won't Start

1. Check PostgreSQL: `Get-Service postgresql*`
2. Verify ports: `netstat -ano | findstr :5000`
3. Review logs in terminal output

### Database Migration Fails

1. Verify connection string: `dotnet user-secrets list`
2. Check database exists
3. Try manual migration: `cd backend && .\migrate.ps1`

### Git Conflicts

1. Stash changes: `git stash`
2. Pull: `git pull`
3. Restore: `git stash pop`
4. Resolve conflicts manually

---

## 🔗 Related Documentation

- [System Architecture](../01_system/)
- [Module Specifications](../02_modules/)
- [API Documentation](../04_api/)
- [Frontend Documentation](../07_frontend/)

---

## 📞 Need Help?

If you encounter issues not covered here:

1. Check the specific guide for detailed troubleshooting
2. Review error messages carefully
3. Consult the development team
4. Update this documentation with solutions you find

---

**Last Updated**: December 3, 2025  
**Maintained By**: CephasOps DevOps Team

