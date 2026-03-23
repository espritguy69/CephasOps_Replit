-- Create dedicated application database user (non-superuser) for production.
-- Run as postgres or a superuser. Replace <app_password> with a strong secret.
-- Do not commit the password. Set it via secret store or environment.

-- Create user
CREATE USER cephasops_app WITH PASSWORD '<app_password>';

-- Grant connect to database
GRANT CONNECT ON DATABASE cephasops TO cephasops_app;

-- Grant usage on public schema
GRANT USAGE ON SCHEMA public TO cephasops_app;

-- Grant table rights (existing tables)
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO cephasops_app;

-- Grant sequence usage (for identity columns)
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO cephasops_app;

-- Default privileges for future tables (run as same user that creates tables, typically postgres)
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO cephasops_app;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT USAGE, SELECT ON SEQUENCES TO cephasops_app;

-- Optional: revoke postgres from application connection in production; use cephasops_app only.
