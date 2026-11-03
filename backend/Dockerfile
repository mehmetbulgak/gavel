FROM mcr.microsoft.com/dotnet/sdk:10.0-noble AS build
ARG PROJECT
WORKDIR /src
COPY . .
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    test -n "$PROJECT" \
    && dotnet restore "$PROJECT" --locked-mode \
    && dotnet publish "$PROJECT" --no-restore -c Release -o /app \
    && mv "/app/$(basename "$PROJECT" .csproj)" /app/gavel-service

FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled AS runtime
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_EnableDiagnostics=0
COPY --from=build /app .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["./gavel-service"]
