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

# Create /data directory — Railway Volume will be mounted here.
# If no volume is attached, the DB falls back to /data/election.db inside the container
# (data won't persist across redeploys without the Volume — see Railway dashboard).
RUN mkdir -p /data

ENV ASPNETCORE_ENVIRONMENT=Production
# DATABASE_PATH must be set in Railway dashboard ? Variables: DATABASE_PATH=/data/election.db
# A Railway Volume must be mounted at /data in Railway dashboard ? Volumes.
# DO NOT hardcode DATABASE_PATH here — if it defaults to /app or a container path,
# the database is on the ephemeral filesystem and all data is lost on every redeploy.

# Use sh so $PORT is evaluated at container start time
ENTRYPOINT ["sh", "-c", "ASPNETCORE_URLS=http://+:${PORT:-8080} dotnet Nirvachak_AI.dll"]
