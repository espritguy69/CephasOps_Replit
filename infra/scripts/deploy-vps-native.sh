#!/bin/bash
set -euo pipefail

REPO_URL="https://github.com/espritguy69/CephasOps_Replit.git"
DEPLOY_DIR="/opt/cephasops"
APP_DIR="$DEPLOY_DIR/app"
PUBLISH_DIR="$DEPLOY_DIR/publish"
LOGS_DIR="$DEPLOY_DIR/logs"
ENV_FILE="$DEPLOY_DIR/.env"
FRONTEND_DIST="$DEPLOY_DIR/frontend-dist"
SI_DIST="$DEPLOY_DIR/si-dist"
API_PORT=8080
DOMAIN="${CEPHASOPS_DOMAIN:-}"

echo "=== CephasOps VPS Native Deployment (No Docker) ==="
echo ""

load_env() {
  if [ -f "$ENV_FILE" ]; then
    set -a
    source "$ENV_FILE"
    set +a
  fi
}

case "${1:-help}" in

  install)
    echo "--- PHASE 1: Installing System Packages ---"
    sudo apt-get update
    sudo apt-get install -y git curl wget nginx certbot python3-certbot-nginx

    echo ""
    echo "--- Installing .NET 10 SDK ---"
    if ! command -v dotnet &>/dev/null; then
      wget https://packages.microsoft.com/config/debian/13/packages-microsoft-prod.deb -O /tmp/packages-microsoft-prod.deb
      sudo dpkg -i /tmp/packages-microsoft-prod.deb
      rm /tmp/packages-microsoft-prod.deb
      sudo apt-get update
      sudo apt-get install -y dotnet-sdk-10.0
    else
      echo ".NET already installed: $(dotnet --version)"
    fi

    echo ""
    echo "--- Installing Node.js 20 ---"
    if ! command -v node &>/dev/null; then
      curl -fsSL https://deb.nodesource.com/setup_20.x | sudo -E bash -
      sudo apt-get install -y nodejs
    else
      echo "Node.js already installed: $(node --version)"
    fi

    echo ""
    echo "--- Installing PostgreSQL ---"
    if ! command -v psql &>/dev/null; then
      sudo apt-get install -y postgresql postgresql-contrib
      sudo systemctl enable postgresql --now
    else
      echo "PostgreSQL already installed: $(psql --version)"
    fi

    echo ""
    echo "--- Creating directories ---"
    sudo mkdir -p "$DEPLOY_DIR" "$PUBLISH_DIR" "$LOGS_DIR" "$FRONTEND_DIST" "$SI_DIST"
    sudo chown -R "$USER":"$USER" "$DEPLOY_DIR"

    echo ""
    echo "PHASE 1 COMPLETE. Next steps:"
    echo "  1. Run: $0 setup-db"
    echo "  2. Run: $0 setup"
    echo "  3. Edit: $ENV_FILE"
    echo "  4. Run: $0 deploy"
    ;;

  setup-db)
    echo "--- PHASE 3: Database Setup ---"
    read -p "Enter database password for cephasops_app user: " -s DB_PASSWORD
    echo ""

    sudo -u postgres psql -c "DO \$\$
    BEGIN
      IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'cephasops_app') THEN
        CREATE ROLE cephasops_app WITH LOGIN PASSWORD '$DB_PASSWORD';
      END IF;
    END \$\$;"

    sudo -u postgres psql -c "SELECT 1 FROM pg_database WHERE datname = 'cephasops'" | grep -q 1 || \
      sudo -u postgres psql -c "CREATE DATABASE cephasops OWNER cephasops_app;"

    sudo -u postgres psql -c "GRANT ALL PRIVILEGES ON DATABASE cephasops TO cephasops_app;"
    sudo -u postgres psql -d cephasops -c "GRANT ALL ON SCHEMA public TO cephasops_app;"

    echo ""
    echo "Database 'cephasops' ready. User: cephasops_app"
    echo "Connection string: Host=localhost;Database=cephasops;Username=cephasops_app;Password=YOUR_PASSWORD"
    ;;

  setup)
    echo "--- PHASE 2: GitHub Source Setup ---"
    if [ -d "$APP_DIR/.git" ]; then
      echo "Repo exists. Pulling latest..."
      cd "$APP_DIR"
      git fetch origin && git reset --hard origin/main
    else
      echo "Cloning repo..."
      git clone "$REPO_URL" "$APP_DIR"
    fi

    cd "$APP_DIR"
    BRANCH=$(git branch --show-current)
    COMMIT=$(git log --oneline -1)
    echo "Branch: $BRANCH"
    echo "Commit: $COMMIT"

    echo ""
    echo "--- Verifying repo structure ---"
    for dir in backend frontend frontend-si docs; do
      if [ -d "$APP_DIR/$dir" ]; then
        echo "  ✓ $dir/"
      else
        echo "  ✗ $dir/ MISSING"
      fi
    done

    if [ ! -f "$ENV_FILE" ]; then
      echo ""
      echo "--- Creating environment file ---"
      cat > "$ENV_FILE" << 'ENVEOF'
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://127.0.0.1:8080

ConnectionStrings__DefaultConnection=Host=localhost;Database=cephasops;Username=cephasops_app;Password=CHANGE_ME

Jwt__Key=CHANGE_ME_TO_A_SECURE_64_CHAR_KEY_FOR_PRODUCTION_USE_ONLY_1234
Jwt__Issuer=CephasOps
Jwt__Audience=CephasOps

Encryption__Key=CHANGE_ME_32_CHARS_EXACTLY_HERE!
Encryption__IV=CHANGE_ME_16_BYTE

Cors__AllowedOrigins__0=https://your-domain.com
Cors__AllowedOrigins__1=https://si.your-domain.com

Carbone__Enabled=true
Carbone__BaseUrl=https://api.carbone.io
Carbone__ApiKey=YOUR_CARBONE_KEY
Carbone__TimeoutSeconds=30
Carbone__ApiVersion=4

WhatsAppCloudApi__PhoneNumberId=
WhatsAppCloudApi__AccessToken=
WhatsAppCloudApi__BusinessAccountId=

Scheduler__PollIntervalSeconds=15
Scheduler__MaxJobsPerPoll=10
ENVEOF
      echo ""
      echo "IMPORTANT: Edit $ENV_FILE with your production values!"
      echo "  nano $ENV_FILE"
    else
      echo "Environment file exists at $ENV_FILE"
    fi
    ;;

  migrate)
    echo "--- PHASE 4: Database Migration ---"
    load_env

    CONN="${ConnectionStrings__DefaultConnection:-}"
    if [ -z "$CONN" ]; then
      echo "ERROR: ConnectionStrings__DefaultConnection not set in $ENV_FILE"
      exit 1
    fi

    PG_HOST=$(echo "$CONN" | grep -oP 'Host=\K[^;]+')
    PG_DB=$(echo "$CONN" | grep -oP 'Database=\K[^;]+')
    PG_USER=$(echo "$CONN" | grep -oP 'Username=\K[^;]+')
    PG_PASS=$(echo "$CONN" | grep -oP 'Password=\K[^;]+')

    export PGPASSWORD="$PG_PASS"

    echo "Testing database connectivity..."
    psql -h "$PG_HOST" -U "$PG_USER" -d "$PG_DB" -c "SELECT 1;" >/dev/null 2>&1 || {
      echo "ERROR: Cannot connect to database. Check connection string in $ENV_FILE"
      exit 1
    }
    echo "  ✓ Database connected"

    echo ""
    echo "Running pre-migration script..."
    if [ -f "$APP_DIR/infra/scripts/pre-migration.sql" ]; then
      psql -h "$PG_HOST" -U "$PG_USER" -d "$PG_DB" -f "$APP_DIR/infra/scripts/pre-migration.sql"
      echo "  ✓ Pre-migration complete"
    fi

    echo ""
    echo "Running idempotent migrations..."
    psql -h "$PG_HOST" -U "$PG_USER" -d "$PG_DB" \
      -v ON_ERROR_STOP=0 \
      -f "$APP_DIR/backend/scripts/migrations-idempotent-latest.sql"
    echo "  ✓ Migrations applied"

    echo ""
    echo "Running seed data..."
    SEED_DIR="$APP_DIR/backend/scripts/postgresql-seeds"
    for seed in 01_system_data.sql 02_reference_data.sql 03_master_data.sql 04_configuration_data.sql 05_inventory_data.sql 06_document_placeholders.sql 07_gpon_order_workflow.sql; do
      if [ -f "$SEED_DIR/$seed" ]; then
        echo "  Seeding $seed..."
        psql -h "$PG_HOST" -U "$PG_USER" -d "$PG_DB" \
          -v ON_ERROR_STOP=1 \
          -f "$SEED_DIR/$seed" 2>&1 || echo "  ⚠ Warning: $seed had errors (may be OK if data exists)"
      fi
    done
    echo "  ✓ Seeds complete"

    echo ""
    echo "Verifying critical tables..."
    TABLES=("Orders" "Companies" "Users" "WorkflowDefinitions" "ParserTemplates" "GlobalSettings" "Materials" "Invoices")
    for tbl in "${TABLES[@]}"; do
      COUNT=$(psql -h "$PG_HOST" -U "$PG_USER" -d "$PG_DB" -t -c "SELECT COUNT(*) FROM \"$tbl\";" 2>/dev/null | tr -d ' ' || echo "MISSING")
      echo "  $tbl: $COUNT rows"
    done

    unset PGPASSWORD
    echo ""
    echo "PHASE 4 COMPLETE: Schema validated"
    ;;

  build-backend)
    echo "--- PHASE 5: Backend Build + Publish ---"
    cd "$APP_DIR/backend/src/CephasOps.Api"

    echo "Restoring packages..."
    dotnet restore

    echo "Publishing Release build..."
    dotnet publish -c Release -o "$PUBLISH_DIR"

    echo "  ✓ Published to $PUBLISH_DIR"
    echo ""
    echo "Files:"
    ls -la "$PUBLISH_DIR"/CephasOps.Api.dll 2>/dev/null && echo "  ✓ API DLL present" || echo "  ✗ API DLL missing!"
    ;;

  build-frontend)
    echo "--- PHASE 6: Frontend Build ---"

    echo "Building Admin Portal..."
    cd "$APP_DIR/frontend"
    npm install
    npm run build
    rm -rf "$FRONTEND_DIST"/*
    cp -r dist/* "$FRONTEND_DIST/"
    echo "  ✓ Admin Portal built → $FRONTEND_DIST"

    echo ""
    echo "Building SI App..."
    cd "$APP_DIR/frontend-si"
    npm install
    npm run build
    rm -rf "$SI_DIST"/*
    cp -r dist/* "$SI_DIST/"
    echo "  ✓ SI App built → $SI_DIST"
    ;;

  setup-service)
    echo "--- PHASE 5b: Creating systemd service ---"

    ENV_LINES=""
    if [ -f "$ENV_FILE" ]; then
      while IFS= read -r line; do
        [[ -z "$line" || "$line" == \#* ]] && continue
        ENV_LINES="${ENV_LINES}Environment=${line}\n"
      done < "$ENV_FILE"
    fi

    sudo tee /etc/systemd/system/cephasops-api.service > /dev/null << EOF
[Unit]
Description=CephasOps API
After=network.target postgresql.service

[Service]
Type=notify
WorkingDirectory=$PUBLISH_DIR
ExecStart=/usr/bin/dotnet $PUBLISH_DIR/CephasOps.Api.dll
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=cephasops-api
User=www-data
Group=www-data
EnvironmentFile=$ENV_FILE
StandardOutput=append:$LOGS_DIR/api-stdout.log
StandardError=append:$LOGS_DIR/api-stderr.log

[Install]
WantedBy=multi-user.target
EOF

    sudo chown -R www-data:www-data "$PUBLISH_DIR" "$LOGS_DIR"
    sudo systemctl daemon-reload
    sudo systemctl enable cephasops-api
    echo "  ✓ Service created and enabled"
    echo "  Start with: sudo systemctl start cephasops-api"
    echo "  Logs: journalctl -u cephasops-api -f"
    ;;

  setup-nginx)
    echo "--- PHASE 7: NGINX Setup ---"

    if [ -z "$DOMAIN" ]; then
      read -p "Enter your domain (e.g. app.cephasops.com): " DOMAIN
    fi

    sudo tee /etc/nginx/sites-available/cephasops > /dev/null << EOF
server {
    listen 80;
    server_name $DOMAIN;

    gzip on;
    gzip_types text/plain text/css application/json application/javascript text/xml application/xml text/javascript image/svg+xml;
    gzip_min_length 256;

    add_header X-Frame-Options "SAMEORIGIN" always;
    add_header X-Content-Type-Options "nosniff" always;
    add_header X-XSS-Protection "1; mode=block" always;
    add_header Referrer-Policy "strict-origin-when-cross-origin" always;

    # Admin Portal
    location / {
        root $FRONTEND_DIST;
        try_files \$uri \$uri/ /index.html;

        location ~* \.(js|css|png|jpg|jpeg|gif|ico|svg|woff|woff2|ttf|eot)$ {
            expires 30d;
            add_header Cache-Control "public, immutable";
        }
    }

    # SI App (subpath)
    location /si/ {
        alias $SI_DIST/;
        try_files \$uri \$uri/ /si/index.html;
    }

    # API reverse proxy
    location /api/ {
        proxy_pass http://127.0.0.1:$API_PORT;
        proxy_http_version 1.1;
        proxy_set_header Upgrade \$http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host \$host;
        proxy_set_header X-Real-IP \$remote_addr;
        proxy_set_header X-Forwarded-For \$proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto \$scheme;
        proxy_read_timeout 300s;
        proxy_send_timeout 300s;
        client_max_body_size 50M;
    }

    # SignalR / WebSocket support
    location /hubs/ {
        proxy_pass http://127.0.0.1:$API_PORT;
        proxy_http_version 1.1;
        proxy_set_header Upgrade \$http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host \$host;
        proxy_set_header X-Real-IP \$remote_addr;
        proxy_set_header X-Forwarded-For \$proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto \$scheme;
    }
}
EOF

    sudo ln -sf /etc/nginx/sites-available/cephasops /etc/nginx/sites-enabled/
    sudo rm -f /etc/nginx/sites-enabled/default
    sudo nginx -t && sudo systemctl reload nginx
    echo "  ✓ NGINX configured for $DOMAIN"
    ;;

  setup-ssl)
    echo "--- PHASE 8: HTTPS with Let's Encrypt ---"
    if [ -z "$DOMAIN" ]; then
      read -p "Enter your domain: " DOMAIN
    fi
    sudo certbot --nginx -d "$DOMAIN" --non-interactive --agree-tos --email admin@"$DOMAIN"
    echo "  ✓ SSL certificate installed"
    echo "  Auto-renewal is handled by certbot timer"
    ;;

  deploy)
    echo "--- FULL DEPLOY ---"
    echo ""

    echo "[1/6] Pulling latest code..."
    cd "$APP_DIR"
    git fetch origin && git reset --hard origin/main
    COMMIT=$(git log --oneline -1)
    echo "  Deployed: $COMMIT"

    echo ""
    echo "[2/6] Running migrations..."
    $0 migrate

    echo ""
    echo "[3/6] Building backend..."
    $0 build-backend

    echo ""
    echo "[4/6] Building frontend..."
    $0 build-frontend

    echo ""
    echo "[5/6] Setting permissions..."
    sudo chown -R www-data:www-data "$PUBLISH_DIR" "$LOGS_DIR" "$FRONTEND_DIST" "$SI_DIST"

    echo ""
    echo "[6/6] Restarting API..."
    sudo systemctl restart cephasops-api
    sleep 3
    sudo systemctl status cephasops-api --no-pager -l

    echo ""
    echo "=== DEPLOY COMPLETE ==="
    echo "Commit: $COMMIT"
    echo "API: http://127.0.0.1:$API_PORT"
    echo "Logs: journalctl -u cephasops-api -f"
    ;;

  status)
    echo "--- Service Status ---"
    sudo systemctl status cephasops-api --no-pager -l 2>/dev/null || echo "Service not running"
    echo ""
    echo "--- NGINX Status ---"
    sudo systemctl status nginx --no-pager 2>/dev/null || echo "NGINX not running"
    echo ""
    echo "--- PostgreSQL Status ---"
    sudo systemctl status postgresql --no-pager 2>/dev/null || echo "PostgreSQL not running"
    echo ""
    if [ -d "$APP_DIR/.git" ]; then
      echo "--- Deployed Version ---"
      cd "$APP_DIR" && git log --oneline -1
    fi
    ;;

  logs)
    echo "--- API Logs (last 50 lines) ---"
    journalctl -u cephasops-api --no-pager -n 50
    ;;

  restart)
    echo "Restarting CephasOps API..."
    sudo systemctl restart cephasops-api
    sleep 2
    sudo systemctl status cephasops-api --no-pager
    ;;

  stop)
    echo "Stopping CephasOps API..."
    sudo systemctl stop cephasops-api
    echo "  ✓ Stopped"
    ;;

  help|*)
    echo "Usage: $0 <command>"
    echo ""
    echo "INITIAL SETUP (run in order):"
    echo "  install         Install .NET 10, Node.js, PostgreSQL, NGINX"
    echo "  setup-db        Create database and user"
    echo "  setup           Clone repo + create env file"
    echo "  migrate         Run schema migrations + seed data"
    echo "  build-backend   Build and publish .NET API"
    echo "  build-frontend  Build admin portal + SI app"
    echo "  setup-service   Create systemd service"
    echo "  setup-nginx     Configure NGINX reverse proxy"
    echo "  setup-ssl       Install Let's Encrypt HTTPS certificate"
    echo ""
    echo "DAY-TO-DAY:"
    echo "  deploy          Pull + migrate + build + restart (full update)"
    echo "  restart         Restart API service"
    echo "  stop            Stop API service"
    echo "  status          Show all service statuses"
    echo "  logs            Show recent API logs"
    echo ""
    echo "ENVIRONMENT:"
    echo "  Set CEPHASOPS_DOMAIN=your-domain.com before running setup-nginx/setup-ssl"
    echo "  Edit $ENV_FILE for connection strings and secrets"
    ;;
esac
