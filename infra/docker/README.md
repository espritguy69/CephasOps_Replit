# CephasOps Docker

## Build (from repo root)

```bash
docker build -f infra/docker/Dockerfile -t cephasops/api:latest .
```

## Run with Docker Compose

From repo root:

```bash
# Copy and set secrets
export ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=cephasops;Username=postgres;Password=YOUR_PASSWORD;SslMode=Disable"
export Jwt__SecretKey="your-secret-key-at-least-16-chars"
export POSTGRES_PASSWORD=YOUR_PASSWORD

docker compose -f infra/docker/docker-compose.yml up -d
```

Or use an `.env` file and:

```bash
docker compose -f infra/docker/docker-compose.yml --env-file .env up -d
```

- **api:** HTTP on port 5000; no background workers (ProductionRoles worker flags false).
- **worker:** Same image; runs all hosted services (job workers, Guardian, schedulers, etc.).
- **db:** PostgreSQL 16; run migrations separately (e.g. `dotnet ef database update` from host or init job).
- **redis:** Rate limit and optional cache; API and worker use `ConnectionStrings__Redis=redis:6379`.

## Deploy / rollback scripts

See `infra/scripts/deploy.ps1` and `infra/scripts/rollback.ps1` for Docker Compose and Kubernetes usage.
