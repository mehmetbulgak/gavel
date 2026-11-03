#!/bin/sh
set -eu

create_service_database() {
    name="$1"
    password="$2"

    psql -v ON_ERROR_STOP=1 -v name="$name" -v password="$password" --username "$POSTGRES_USER" --dbname postgres <<'SQL'
SELECT format('CREATE ROLE %I LOGIN PASSWORD %L', :'name', :'password')
WHERE NOT EXISTS (SELECT FROM pg_roles WHERE rolname = :'name')
\gexec
SELECT format('CREATE DATABASE %I OWNER %I', :'name', :'name')
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = :'name')
\gexec
SELECT format('REVOKE ALL ON DATABASE %I FROM PUBLIC', :'name')
\gexec
SQL
}

create_service_database gavel_identity "$IDENTITY_DB_PASSWORD"
create_service_database gavel_auctions "$AUCTIONS_DB_PASSWORD"
create_service_database gavel_notifications "$NOTIFICATIONS_DB_PASSWORD"
