#!/bin/sh
set -eu
service="$1"

wait_http() {
    attempts=0
    until curl --fail --silent --max-time 3 "$1" > /dev/null; do
        attempts=$((attempts + 1))
        if [ "$attempts" -ge 180 ]; then
            echo "Timed out waiting for $1" >&2
            exit 1
        fi
        sleep 1
    done
}

if [ "$service" = identity ]; then
    until pg_isready -q -h "$PGHOST" -U gavel_identity -d gavel_identity; do sleep 1; done
    until timeout 1 bash -c 'exec 3<>/dev/tcp/127.0.0.1/5672' 2>/dev/null; do sleep 1; done
    port=5201
    assembly=Gavel.Identity.Api
elif [ "$service" = auctions ]; then
    wait_http http://127.0.0.1:5201/health/ready
    port=5202
    assembly=Gavel.Auctions.Api
elif [ "$service" = notifications ]; then
    wait_http http://127.0.0.1:5202/health/ready
    port=5203
    assembly=Gavel.Notifications.Api
elif [ "$service" = gateway ]; then
    wait_http http://127.0.0.1:5203/health/ready
    port=5100
    assembly=Gavel.Gateway
elif [ "$service" = simulator ]; then
    wait_http http://127.0.0.1:5100/health/ready
    cd /app/simulator
    exec dotnet Gavel.Simulator.dll
else
    echo 'Unknown service' >&2
    exit 1
fi

cd "/app/$service"
export ASPNETCORE_URLS="http://127.0.0.1:$port"
exec dotnet "$assembly.dll"
