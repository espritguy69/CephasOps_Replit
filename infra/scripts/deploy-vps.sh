#!/bin/bash
set -euo pipefail

REPO_URL="https://github.com/espritguy69/CephasOps_Replit.git"
DEPLOY_DIR="/opt/cephasops"
COMPOSE_FILE="infra/docker/docker-compose.prod.yml"

echo "=== CephasOps VPS Deployment Script ==="
echo ""

case "${1:-help}" in
  install)
    echo "--- Installing Docker + Docker Compose ---"
    sudo apt-get update
    sudo apt-get install -y ca-certificates curl gnupg lsb-release
    sudo install -m 0755 -d /etc/apt/keyrings
    curl -fsSL https://download.docker.com/linux/ubuntu/gpg | sudo gpg --dearmor -o /etc/apt/keyrings/docker.gpg
    sudo chmod a+r /etc/apt/keyrings/docker.gpg
    echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] https://download.docker.com/linux/ubuntu $(lsb_release -cs) stable" | sudo tee /etc/apt/sources.list.d/docker.list > /dev/null
    sudo apt-get update
    sudo apt-get install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin
    sudo usermod -aG docker "$USER"
    echo "Docker installed. Log out and back in for group changes."
    ;;

  setup)
    echo "--- Cloning repo and setting up ---"
    sudo mkdir -p "$DEPLOY_DIR"
    sudo chown "$USER":"$USER" "$DEPLOY_DIR"
    git clone "$REPO_URL" "$DEPLOY_DIR"
    cp "$DEPLOY_DIR/infra/docker/.env.example" "$DEPLOY_DIR/infra/docker/.env"
    echo ""
    echo "IMPORTANT: Edit $DEPLOY_DIR/infra/docker/.env with your production values!"
    echo "  nano $DEPLOY_DIR/infra/docker/.env"
    ;;

  build)
    echo "--- Building containers ---"
    cd "$DEPLOY_DIR"
    docker compose -f "$COMPOSE_FILE" build --no-cache
    ;;

  start)
    echo "--- Starting all services ---"
    cd "$DEPLOY_DIR"
    docker compose -f "$COMPOSE_FILE" up -d
    echo "Waiting for health check..."
    sleep 10
    docker compose -f "$COMPOSE_FILE" ps
    echo ""
    echo "Check API health:"
    curl -sf http://127.0.0.1:8080/health/ready || echo "API not ready yet — check logs"
    ;;

  stop)
    echo "--- Stopping all services ---"
    cd "$DEPLOY_DIR"
    docker compose -f "$COMPOSE_FILE" down
    ;;

  restart)
    echo "--- Restarting services ---"
    cd "$DEPLOY_DIR"
    docker compose -f "$COMPOSE_FILE" down
    docker compose -f "$COMPOSE_FILE" up -d
    ;;

  logs)
    echo "--- Container logs ---"
    cd "$DEPLOY_DIR"
    docker compose -f "$COMPOSE_FILE" logs -f --tail=100 "${2:-}"
    ;;

  update)
    echo "--- Pulling latest code and rebuilding ---"
    cd "$DEPLOY_DIR"
    git pull origin main
    docker compose -f "$COMPOSE_FILE" build
    docker compose -f "$COMPOSE_FILE" down
    docker compose -f "$COMPOSE_FILE" up -d
    echo "Update complete. Check health:"
    sleep 10
    curl -sf http://127.0.0.1:8080/health/ready || echo "API not ready yet"
    ;;

  backup-db)
    echo "--- Backing up PostgreSQL ---"
    TIMESTAMP=$(date +%Y%m%d_%H%M%S)
    BACKUP_DIR="$DEPLOY_DIR/backups"
    mkdir -p "$BACKUP_DIR"
    docker exec cephasops-db pg_dump -U cephasops_app -d cephasops --format=custom > "$BACKUP_DIR/cephasops_${TIMESTAMP}.dump"
    echo "Backup saved: $BACKUP_DIR/cephasops_${TIMESTAMP}.dump"
    ;;

  restore-db)
    if [ -z "${2:-}" ]; then
      echo "Usage: $0 restore-db <backup-file>"
      exit 1
    fi
    echo "--- Restoring PostgreSQL from $2 ---"
    docker exec -i cephasops-db pg_restore -U cephasops_app -d cephasops --clean --if-exists < "$2"
    echo "Restore complete."
    ;;

  migrate)
    echo "--- Running database migrations via SQL ---"
    echo ""
    echo "Migration order:"
    echo "  1. pre-migration.sql          (pgcrypto + __EFMigrationsHistory + InstallationMethods)"
    echo "  2. Supplementary SQL files     (tables/columns from Migrations/ directory)"
    echo "  3. Extra schema scripts        (tables from backend/scripts/apply-*.sql & add-*.sql)"
    echo "  4. migrations-idempotent-latest.sql  (EF Core consolidated idempotent script)"
    echo "  5. Seed data"
    echo ""
    cd "$DEPLOY_DIR"
    echo "Backing up database first..."
    $0 backup-db
    echo ""

    echo "=== Step 1/5: Pre-migration bootstrap ==="
    docker exec -i cephasops-db psql -v ON_ERROR_STOP=1 -U cephasops_app -d cephasops < "$DEPLOY_DIR/infra/scripts/pre-migration.sql"
    echo "  Done."
    echo ""

    echo "=== Step 2/5: Supplementary SQL files (create tables/columns before EF Core script) ==="
    MIGRATIONS_DIR="$DEPLOY_DIR/backend/src/CephasOps.Infrastructure/Persistence/Migrations"
    MIGRATION_FILES=(
      "AddPhase1Phase2Entities.sql"
      "AddPhase1SettingsEntities.sql"
      "AddPhase2SettingsEntities.sql"
      "AddPhase3Entities.sql"
      "AddPhase4Entities.sql"
      "AddPhase5Entities.sql"
      "FixPhase5Tables.sql"
      "AddPhase6WorkflowEntities.sql"
      "FixPhase6WorkflowTables.sql"
      "AddPnlTypesAssetsAndAccounting.sql"
      "AddInstallationMethodsTable.sql"
      "AddFilesTable.sql"
      "AddBillingRatecardTable.sql"
      "AddCompanyDocumentsTable.sql"
      "AddVipEmailsAndParserTemplates.sql"
      "AddVipColumnsToEmailMessages.sql"
      "AddDefaultParserTemplateToEmailAccounts.sql"
      "AddDepartmentIdToVipEmailsAndEmailMessages.sql"
      "AddDepartmentAndInstallationMethodToRates.sql"
      "RenameCreatedByColumn.sql"
      "UpdateFilesTable.sql"
      "RemoveCompanyFeature.sql"
      "FixBuildingRulesDuplicateIndex.sql"
      "20241127_AddBuildingDefaultMaterials.sql"
      "20241127_AddDepartmentIdToInstallationMethods.sql"
      "20251207_AddMaterialPartnersTable.sql"
      "20251207_MigrateMaterialPartnerIdToJoinTable.sql"
      "20251216120000_AddFullEmailBodyToEmailMessages.sql"
      "20251216130000_UpdateParserTemplatesFlexiblePatterns.sql"
      "20251216140000_UpdateTimeAssuranceTemplateForEDocket.sql"
      "20251216200000_RemoveTimeModificationAndUpdateModificationTemplates.sql"
      "20251216210000_AddEmailSendingTemplates.sql"
      "20251216220000_AddDirectionToEmailTemplates.sql"
      "20251216230000_UpdateExistingEmailTemplatesDirection.sql"
      "20251216240000_EnsureRescheduleEmailTemplatesExist.sql"
      "20251217000000_AddEmailViewerFeatures.sql"
      "20251219000000_AddIssueAndSolutionToOrders.sql"
      "20250106_SeedAllReferenceData.sql"
      "Apply_DedupeOrderTypeParents.sql"
      "Apply_DedupeOrderTypeParents_fix_subtypes.sql"
    )
    SUPP_OK=0
    SUPP_SKIP=0
    for f in "${MIGRATION_FILES[@]}"; do
      if [ -f "$MIGRATIONS_DIR/$f" ]; then
        echo "  Applying $f..."
        docker exec -i cephasops-db psql -v ON_ERROR_STOP=0 -U cephasops_app -d cephasops < "$MIGRATIONS_DIR/$f" 2>&1 | grep -i "error" || true
        SUPP_OK=$((SUPP_OK + 1))
      else
        echo "  Skipping $f (not found)"
        SUPP_SKIP=$((SUPP_SKIP + 1))
      fi
    done
    echo "  Supplementary: $SUPP_OK applied, $SUPP_SKIP skipped."
    echo ""

    echo "=== Step 3/5: Extra schema scripts (backend/scripts/) ==="
    EXTRA_SCRIPTS=(
      "apply-AddBaseWorkRates.sql"
      "apply-AddServiceProfiles.sql"
      "apply-AddRateModifiers.sql"
      "apply-AddEnterpriseSaaSColumnsAndTenantActivity.sql"
      "apply-AddExternalIntegrationBus-repair.sql"
      "apply-AddCompanyStatus-repair.sql"
      "apply-background-job-worker-ownership.sql"
      "apply-jobruns-migrations.sql"
      "apply-OperationalInsights-And-FeatureFlags.sql"
      "apply-EventStore-CausationId.sql"
      "apply-EventStorePhase7LeaseAndAttemptHistory.sql"
      "apply-lockout-fields.sql"
      "apply-payout-anomaly-review-migration.sql"
      "apply-schema-drift-remediation.sql"
      "apply-remediation-1.1-schema-repair.sql"
      "add-JobDefinitions-table.sql"
      "add-payout-anomaly-alerts-table.sql"
      "add-payout-anomaly-alert-runs-table.sql"
      "add-order-type-parent-column.sql"
      "add-partners-code-column.sql"
      "add-unique-constraint-splitter-types.sql"
      "add-invoice-rejection-loop-transitions.sql"
      "sla-migrations-idempotent.sql"
    )
    EXTRA_OK=0
    EXTRA_SKIP=0
    for f in "${EXTRA_SCRIPTS[@]}"; do
      if [ -f "$DEPLOY_DIR/backend/scripts/$f" ]; then
        echo "  Applying $f..."
        docker exec -i cephasops-db psql -v ON_ERROR_STOP=0 -U cephasops_app -d cephasops < "$DEPLOY_DIR/backend/scripts/$f" 2>&1 | grep -i "error" || true
        EXTRA_OK=$((EXTRA_OK + 1))
      else
        echo "  Skipping $f (not found)"
        EXTRA_SKIP=$((EXTRA_SKIP + 1))
      fi
    done
    echo "  Extra scripts: $EXTRA_OK applied, $EXTRA_SKIP skipped."
    echo ""

    echo "=== Step 4/5: EF Core idempotent migration script ==="
    echo "  (using ON_ERROR_STOP=0 — failed migrations are rolled back per-transaction and retryable)"
    docker exec -i cephasops-db psql -v ON_ERROR_STOP=0 -U cephasops_app -d cephasops < "$DEPLOY_DIR/backend/scripts/migrations-idempotent-latest.sql" 2>&1 | grep -i "error" | grep -v "already exists" || true
    echo "  Done."
    echo ""

    echo "=== Step 5/5: Seed data ==="
    for f in "$DEPLOY_DIR"/backend/scripts/postgresql-seeds/*.sql; do
      BASENAME=$(basename "$f")
      if echo "$BASENAME" | grep -qi "create.*user"; then
        echo "  Skipping $BASENAME (requires superuser — run manually if needed)"
        continue
      fi
      echo "  Applying $BASENAME..."
      docker exec -i cephasops-db psql -v ON_ERROR_STOP=1 -U cephasops_app -d cephasops < "$f" || echo "  Warning: $BASENAME had errors (may be idempotent, continuing)"
    done
    echo ""
    echo "=== Migrations and seeding complete. ==="
    ;;

  health)
    echo "--- Health check ---"
    curl -sf http://127.0.0.1:8080/health/ready && echo " OK" || echo " FAILED"
    curl -sf http://127.0.0.1:8080/health/platform && echo " OK" || echo " FAILED"
    ;;

  nginx)
    echo "--- Setting up Nginx + SSL ---"
    sudo apt-get install -y nginx certbot python3-certbot-nginx
    sudo mkdir -p /var/www/certbot
    echo ""
    echo "Step 1: Install HTTP-only config (for initial cert issuance)"
    sudo cp "$DEPLOY_DIR/infra/nginx/api.conf" /etc/nginx/sites-available/cephasops-api
    sudo ln -sf /etc/nginx/sites-available/cephasops-api /etc/nginx/sites-enabled/
    sudo rm -f /etc/nginx/sites-enabled/default
    echo ""
    echo "IMPORTANT: Edit the config file first:"
    echo "  sudo nano /etc/nginx/sites-available/cephasops-api"
    echo "  Replace 'api.yourdomain.com' with your actual domain"
    echo ""
    echo "Then run these commands in order:"
    echo "  1. sudo nginx -t && sudo systemctl reload nginx"
    echo "  2. sudo certbot --nginx -d api.yourdomain.com"
    echo ""
    echo "Step 2: After certbot succeeds, install SSL config:"
    echo "  sudo cp $DEPLOY_DIR/infra/nginx/api-ssl.conf /etc/nginx/sites-available/cephasops-api"
    echo "  Edit it to replace 'api.yourdomain.com' with your domain"
    echo "  sudo nginx -t && sudo systemctl reload nginx"
    echo ""
    echo "Certbot auto-renewal is enabled by default."
    ;;

  status)
    echo "--- Service status ---"
    cd "$DEPLOY_DIR"
    docker compose -f "$COMPOSE_FILE" ps
    echo ""
    $0 health
    ;;

  help|*)
    echo "Usage: $0 <command>"
    echo ""
    echo "Commands:"
    echo "  install     Install Docker + Docker Compose on Ubuntu"
    echo "  setup       Clone repo and create .env file"
    echo "  build       Build Docker images"
    echo "  start       Start all containers"
    echo "  stop        Stop all containers"
    echo "  restart     Restart all containers"
    echo "  update      Pull latest code, rebuild, restart"
    echo "  logs [svc]  View logs (optional: api, worker, db)"
    echo "  backup-db   Backup PostgreSQL database"
    echo "  restore-db  Restore from backup file"
    echo "  migrate     Run database migrations (SQL) + seed data"
    echo "  health      Check API health endpoints"
    echo "  nginx       Install Nginx and copy config"
    echo "  status      Show container status + health"
    echo ""
    ;;
esac
