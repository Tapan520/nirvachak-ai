# ?? Stage 1: Build ???????????????????????????????????????????????
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore dependencies first (layer-cached)
COPY Nirvachak_AI.csproj .
RUN dotnet restore

# Copy the rest of the source and publish
COPY . .
RUN dotnet publish Nirvachak_AI.csproj -c Release -o /app/publish --no-restore

# ?? Stage 2: Runtime ?????????????????????????????????????????????
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

# Optional local folder for backup dumps / settings (not the primary DB).
RUN mkdir -p /data/backups

ENV ASPNETCORE_ENVIRONMENT=Production
# Required Railway/production variable:
#   MYSQL_CONNECTION_STRING=Server=...;Port=3306;Database=nirvachak_ai;User=...;Password=...;CharSet=utf8mb4;
# Optional:
#   Backup__Directory=/data/backups

# Use sh so $PORT is evaluated at container start time
ENTRYPOINT ["sh", "-c", "ASPNETCORE_URLS=http://+:${PORT:-8080} dotnet Nirvachak_AI.dll"]
