#!/bin/sh
set -eu
umask 077

mkdir -p "$PGHOST" "$RABBITMQ_MNESIA_BASE" "$RABBITMQ_LOG_BASE"
if [ ! -s "$PGDATA/PG_VERSION" ]; then
    initdb -D "$PGDATA" -U rabbitmq --auth-local=peer --auth-host=reject --encoding=UTF8 --no-locale
    cat > "$PGDATA/pg_hba.conf" <<'EOF'
local gavel_identity gavel_identity peer map=gavel
local gavel_auctions gavel_auctions peer map=gavel
local gavel_notifications gavel_notifications peer map=gavel
local all rabbitmq peer
EOF
    cat > "$PGDATA/pg_ident.conf" <<'EOF'
gavel rabbitmq gavel_identity
gavel rabbitmq gavel_auctions
gavel rabbitmq gavel_notifications
EOF
    cat >> "$PGDATA/postgresql.conf" <<EOF
listen_addresses = ''
unix_socket_directories = '$PGHOST'
unix_socket_permissions = 0700
shared_buffers = '16MB'
max_connections = 35
work_mem = '1MB'
maintenance_work_mem = '16MB'
EOF
    pg_ctl -D "$PGDATA" -l /tmp/gavel-postgres.log -w start
    for service in identity auctions notifications; do
        psql -U rabbitmq -d postgres -v ON_ERROR_STOP=1 -c "CREATE ROLE gavel_$service LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE;"
        createdb -U rabbitmq -O "gavel_$service" "gavel_$service"
        psql -U rabbitmq -d postgres -v ON_ERROR_STOP=1 -c "REVOKE CONNECT ON DATABASE gavel_$service FROM PUBLIC; GRANT CONNECT ON DATABASE gavel_$service TO gavel_$service;"
    done
    pg_ctl -D "$PGDATA" -m fast -w stop
fi

if [ -s "$RABBITMQ_CONFIG_FILE.conf" ]; then
    broker_password=$(sed -n 's/^default_pass = //p' "$RABBITMQ_CONFIG_FILE.conf")
else
    broker_password=$(openssl rand -hex 24)
fi
export ConnectionStrings__rabbitmq="amqp://gavel:$broker_password@127.0.0.1:5672/gavel"

cat > "$RABBITMQ_CONFIG_FILE.conf" <<EOF
listeners.tcp.1 = 127.0.0.1:5672
distribution.listener.interface = 127.0.0.1
default_user = gavel
default_pass = $broker_password
default_vhost = gavel
loopback_users.gavel = true
vm_memory_high_watermark.absolute = 128MiB
disk_free_limit.absolute = 128MiB
log.console = true
log.console.level = warning
EOF
sed \
    -e 's/worker_processes auto;/worker_processes 1;/' \
    -e 's|include /etc/nginx/conf.d/\*.conf;|include /tmp/gavel-nginx-server.conf;|' \
    /app/deploy/nginx/nginx.conf > /tmp/gavel-nginx.conf
sed \
    -e 's/server gateway:8080;/server 127.0.0.1:5100;/' \
    -e 's/listen 8080;/listen ${PORT};/' \
    -e 's|root /usr/share/nginx/html;|root /app/wwwroot;|' \
    -e 's|/etc/nginx/snippets/|/app/deploy/nginx/snippets/|g' \
    -e '/location \/api\//i\    location = /health { proxy_pass http://gateway/health/ready; }' \
    /app/deploy/nginx/conf.d/default.conf \
    | envsubst '${PORT}' > /tmp/gavel-nginx-server.conf

echo 'Starting Gavel with simulated auctions and disposable sample data.'
exec supervisord -c /app/deploy/supervisord.conf
