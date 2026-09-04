# SQLite ? MySQL Migration Notes

## What changed

- EF provider switched from `Microsoft.EntityFrameworkCore.Sqlite` to `Pomelo.EntityFrameworkCore.MySql`
- Old SQLite migrations removed
- Fresh MySQL migration added: `Migrations/20260904123037_InitialMySql.cs`
- Startup now uses `Database.MigrateAsync()` against MySQL
- Health check is connection-based (no `election.db` file checks)
- Backup service now creates `.sql` dumps via `mysqldump`
- Railway env var is now `MYSQL_CONNECTION_STRING` (not `DATABASE_PATH`)

## Local validation (required before Railway)

1. Ensure MySQL 8 is running locally.
2. Run setup script with your MySQL root password:

```powershell
.\scripts\setup-mysql-local.ps1 -MySqlPassword "YOUR_ROOT_PASSWORD"
```

This will:
- create database `nirvachak_ai`
- store connection string in user-secrets
- apply EF migrations

3. Start app:

```powershell
dotnet run --project Nirvachak_AI.csproj
```

4. Verify:
- Login page loads
- Seed users work
- `/health` returns `"provider": "mysql"` and `"status": "ok"`

## Connection string format

```text
Server=127.0.0.1;Port=3306;Database=nirvachak_ai;User=root;Password=YOUR_PASSWORD;CharSet=utf8mb4;
```

## Railway (after local validation)

1. Add MySQL service in Railway project
2. Set app variable:

```text
MYSQL_CONNECTION_STRING=Server=<host>;Port=3306;Database=nirvachak_ai;User=<user>;Password=<password>;CharSet=utf8mb4;
```

3. Deploy app (do **not** use old `DATABASE_PATH=/data/election.db`)
4. Optional: `Backup__Directory=/data/backups` if you attach a volume for dump files

## Data migration from existing SQLite (optional)

If you need existing SQLite production/local data moved into MySQL:

1. Export from SQLite (`election.db`)
2. Transform/import into MySQL `nirvachak_ai`
3. Or start fresh with seed data for first MySQL validation

This code migration creates schema + seed path for MySQL. Existing SQLite data is **not automatically imported**.
