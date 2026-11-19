# API image for Render (ADR 005, section 3). Build from backend/: docker build -t prepround-api .
# Framework-dependent and without a RID or --platform, so the image builds natively on any architecture.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore in its own layer: it is rebuilt only when the build settings or a project file change, not on every .cs edit.
COPY global.json nuget.config Directory.Build.props Directory.Packages.props ./
COPY src/Prepround.Domain/Prepround.Domain.csproj src/Prepround.Domain/
COPY src/Prepround.Application/Prepround.Application.csproj src/Prepround.Application/
COPY src/Prepround.Infrastructure/Prepround.Infrastructure.csproj src/Prepround.Infrastructure/
COPY src/Prepround.Api/Prepround.Api.csproj src/Prepround.Api/
RUN dotnet restore src/Prepround.Api/Prepround.Api.csproj

COPY src/ src/
RUN dotnet publish src/Prepround.Api/Prepround.Api.csproj -c Release -o /app --no-restore

# Ubuntu-based runtime with ICU: -alpine and -chiseled have none, and StringComparer.InvariantCulture
# would turn ordinal there, changing the topic order in the catalog.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_ENVIRONMENT=Production
USER $APP_UID
EXPOSE 8080

# Render passes the port in PORT (10000 by default); without it the API listens on 8080.
ENTRYPOINT ["/bin/sh", "-c", "ASPNETCORE_HTTP_PORTS=${PORT:-8080} exec dotnet Prepround.Api.dll"]
