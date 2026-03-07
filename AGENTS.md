# AGENTS.md

## Cursor Cloud specific instructions

### Project overview

Başkent Enerji is a foreign currency exchange accounting system with two parallel .NET 8 backends (`AnasıTAS_Deniz.*` at the repo root, `SmileMedical.*` under `SmileMedical/`) and a Vue 3 frontend (`SmileMedical/frontend/`). See `README.md` and `SmileMedical/README.md` for full details.

### Prerequisites

- **.NET 8 SDK** — installed at `/usr/share/dotnet` (symlinked to `/usr/local/bin/dotnet`). `dotnet-ef` global tool is also installed.
- **Node.js 18+** — pre-installed in the VM.
- **Docker** — needed only if you want to run the backend API against a real SQL Server database. The frontend works in demo mode without the API.

### Running services

| Service | Command | Port | Notes |
|---------|---------|------|-------|
| SmileMedical API | `cd SmileMedical/SmileMedical.API && dotnet run --launch-profile http` | 5093 | Requires Docker + SQL Server for full operation |
| AnasıTAS_Deniz API | `cd AnasıTAS_Deniz.API && dotnet run --launch-profile http` | 5093 | Same port — run only one at a time |
| Vue 3 Frontend | `cd SmileMedical/frontend && npm run dev` | 5173 | Has "Demo giriş (API yok)" button for API-less login |

### Database setup (SQL Server via Docker)

The backend APIs require SQL Server. To set up locally:

1. Start Docker daemon: `sudo dockerd &`
2. Run SQL Server: `docker run -d --name sqlserver -e 'ACCEPT_EULA=Y' -e 'MSSQL_SA_PASSWORD=DevP@ss2024!' -p 1433:1433 mcr.microsoft.com/mssql/server:2022-latest`
3. Create DB: `docker exec sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'DevP@ss2024!' -C -Q "CREATE DATABASE SmileMedical_Dev"`
4. Create schema: `docker exec sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'DevP@ss2024!' -C -d SmileMedical_Dev -Q "CREATE SCHEMA mtturkey_exchange"`
5. Apply migrations (skip the last SQLite migration): `cd SmileMedical && dotnet ef database update 20251114105334_AddLastPasswordChangeDateToUser --project SmileMedical.Data --startup-project SmileMedical.API`
6. Transfer tables to the correct schema:
   ```
   docker exec sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'DevP@ss2024!' -C -d SmileMedical_Dev -Q "DECLARE @sql NVARCHAR(MAX) = ''; SELECT @sql = @sql + 'ALTER SCHEMA mtturkey_exchange TRANSFER dbo.' + QUOTENAME(TABLE_NAME) + '; ' FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo'; EXEC sp_executesql @sql;"
   ```

### Key gotchas

- **`appsettings.json` is gitignored** — both API projects need it created manually with `ConnectionStrings:SQL`, `JwtSecretKey`, `JwtIssuer`, `JwtAudience`. See the setup session for example values.
- **`InitialCreateSQLite` migration** (`20260220035527`) uses `TEXT` column types incompatible with SQL Server. When applying EF migrations against SQL Server, stop at `20251114105334_AddLastPasswordChangeDateToUser`.
- **DbContext default schema** is `mtturkey_exchange` but older migrations create tables in `dbo`. After running migrations you must transfer all tables from `dbo` to `mtturkey_exchange`.
- **SmileMedical.API.Tests** has a pre-existing build error (`CS0060`: `SmileMedicalWebApplicationFactory` is public but `Program` is internal). The API project itself builds and runs fine.
- The frontend Vite proxy forwards `/api` requests to `http://localhost:5000` by default (see `vite.config.js`), but the API runs on port **5093** per `launchSettings.json`. For proxied requests to work, either override `VITE_API_BASE_URL=http://localhost:5093` in `SmileMedical/frontend/.env` or update the proxy target.

### Tests

- **AnasıTAS_Deniz unit tests**: `dotnet test AnasıTAS_Deniz.Tests/AnasıTAS_Deniz.Tests.csproj` (4 tests, uses xUnit, no DB required)
- **Frontend unit tests**: `cd SmileMedical/frontend && npm run test:run` (7 tests via Vitest, no API required)
- **Frontend E2E**: `cd SmileMedical/frontend && npm run test:e2e` (Playwright, auto-starts dev server)
