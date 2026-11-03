FROM node:24-alpine AS frontend
WORKDIR /src/frontend
COPY frontend/package.json frontend/package-lock.json ./
RUN npm ci --ignore-scripts --no-audit --no-fund
COPY frontend/ ./
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS backend
WORKDIR /src/backend
COPY backend/ ./
RUN dotnet publish src/Services/Identity/Gavel.Identity.Api/Gavel.Identity.Api.csproj -p:RestoreLockedMode=true -c Release -o /out/identity \
    && dotnet publish src/Services/Auctions/Gavel.Auctions.Api/Gavel.Auctions.Api.csproj -p:RestoreLockedMode=true -c Release -o /out/auctions \
    && dotnet publish src/Services/Notifications/Gavel.Notifications.Api/Gavel.Notifications.Api.csproj -p:RestoreLockedMode=true -c Release -o /out/notifications \
    && dotnet publish src/Gateway/Gavel.Gateway/Gavel.Gateway.csproj -p:RestoreLockedMode=true -c Release -o /out/gateway \
    && dotnet publish src/Simulator/Gavel.Simulator/Gavel.Simulator.csproj -p:RestoreLockedMode=true -c Release -o /out/simulator

FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble AS dotnet
FROM rabbitmq:4.1 AS demo
USER root
ENV DEBIAN_FRONTEND=noninteractive
RUN apt-get update \
    && apt-get install -y --no-install-recommends postgresql-16 redis-server nginx supervisor curl gettext-base libicu74 libssl3t64 zlib1g libstdc++6 tzdata \
    && rm -rf /var/lib/apt/lists/*
COPY --from=dotnet /usr/share/dotnet /usr/share/dotnet
WORKDIR /app
COPY --from=backend /out/ ./
COPY --from=frontend /src/frontend/dist ./wwwroot/
COPY frontend/nginx/ ./deploy/nginx/
COPY deploy/render/ ./deploy/
RUN chmod +x deploy/*.sh && chmod -R a-w /app
ENV PATH="/usr/share/dotnet:/usr/lib/postgresql/16/bin:${PATH}" \
    DOTNET_ROOT=/usr/share/dotnet \
    DOTNET_EnableDiagnostics=0 \
    DOTNET_PROCESSOR_COUNT=1 \
    DOTNET_gcServer=0 \
    DOTNET_GCConserveMemory=5 \
    DOTNET_GCHeapHardLimit=0x3000000 \
    Telemetry__Enabled=false \
    ASPNETCORE_ENVIRONMENT=Production \
    PORT=10000 \
    PGDATA=/tmp/gavel-postgres \
    PGHOST=/tmp/gavel-postgres-socket \
    RABBITMQ_CONFIG_FILE=/tmp/gavel-rabbitmq/rabbitmq \
    RABBITMQ_MNESIA_BASE=/tmp/gavel-rabbitmq/mnesia \
    RABBITMQ_LOG_BASE=/tmp/gavel-rabbitmq/log \
    RABBITMQ_NODENAME=rabbit@localhost \
    RABBITMQ_SERVER_ADDITIONAL_ERL_ARGS="+S 1:1 +A 2 +sbwt none +sbwtdcpu none +sbwtdio none" \
    RABBITMQ_CTL_ERL_ARGS="+S 1:1 +A 1" \
    RABBITMQ_LOGS=- \
    Database__MigrateOnStartup=true \
    Auth__MetadataAddress=http://127.0.0.1:5201/.well-known/openid-configuration \
    Auth__RequireHttpsMetadata=false \
    services__identity__http__0=http://127.0.0.1:5201 \
    services__auctions__http__0=http://127.0.0.1:5202 \
    services__notifications__http__0=http://127.0.0.1:5203 \
    ReverseProxy__Clusters__identity__Destinations__primary__Address=http://127.0.0.1:5201 \
    ReverseProxy__Clusters__auctions__Destinations__primary__Address=http://127.0.0.1:5202 \
    ReverseProxy__Clusters__notifications__Destinations__primary__Address=http://127.0.0.1:5203 \
    ForwardedHeaders__Enabled=true \
    ConnectionStrings__identitydb="Host=/tmp/gavel-postgres-socket;Database=gavel_identity;Username=gavel_identity;Maximum Pool Size=8" \
    ConnectionStrings__auctionsdb="Host=/tmp/gavel-postgres-socket;Database=gavel_auctions;Username=gavel_auctions;Maximum Pool Size=10" \
    ConnectionStrings__notificationsdb="Host=/tmp/gavel-postgres-socket;Database=gavel_notifications;Username=gavel_notifications;Maximum Pool Size=8" \
    Email__DispatchEnabled=false \
    Simulator__GatewayUrl=http://127.0.0.1:5100 \
    Simulator__BotCount=3 \
    Simulator__TargetLiveAuctions=6 \
    Simulator__MinBidIntervalMilliseconds=12000 \
    Simulator__MaxBidIntervalMilliseconds=20000
USER rabbitmq
EXPOSE 10000
ENTRYPOINT ["/app/deploy/start-demo.sh"]
