# syntax=docker/dockerfile:1.6
# Multi-stage .NET 8 API — layer caching + non-root runtime (official `app` user)
ARG DOTNET_SDK_IMAGE=mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim
ARG DOTNET_RUNTIME_IMAGE=mcr.microsoft.com/dotnet/aspnet:8.0-bookworm-slim

FROM ${DOTNET_SDK_IMAGE} AS build
WORKDIR /src

COPY ["apps/api/api.csproj", "apps/api/"]
RUN --mount=type=cache,id=nuget-clinicrest,target=/root/.nuget/packages \
    dotnet restore "apps/api/api.csproj"

COPY . .
RUN --mount=type=cache,id=nuget-clinicrest,target=/root/.nuget/packages \
    dotnet publish "apps/api/api.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false \
    /p:PublishTrimmed=false \
    --no-restore

FROM ${DOTNET_RUNTIME_IMAGE} AS runtime
USER root
WORKDIR /app

LABEL org.opencontainers.image.title="Clinicrest API" \
      org.opencontainers.image.description="ASP.NET Core API"

RUN apt-get update \
    && apt-get install -y --no-install-recommends wget ca-certificates \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

# Microsoft aspnet images ship with non-root user `app` (UID 1654)
RUN chown -R app:app /app
USER app

ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

EXPOSE 8080
HEALTHCHECK --interval=30s --timeout=5s --start-period=25s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://127.0.0.1:8080/health || exit 1

ENTRYPOINT ["dotnet", "api.dll"]
