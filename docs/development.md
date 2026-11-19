# Development

Daily commands for working on the code. First setup is in the main [README](../README.md#local-development).

All commands run from `src/`.

## Run the app

Option A: everything in Docker (SQL Server, Redis, migrator, Seq and the web app on http://localhost:5000):

```bash
docker compose up -d --build
```

Option B: only the dependencies in Docker, and the app with hot reload:

```bash
docker compose up -d acxess-db acxess-redis seq
docker compose up --build acxess-migrator                          # apply migrations, then it stops
ASPNETCORE_ENVIRONMENT=Localhost dotnet watch run --project ./Acxess.Web/
```

Tailwind CSS, in another terminal (from `src/Acxess.Web/`):

```bash
npx @tailwindcss/cli -i ./wwwroot/css/input.css -o ./wwwroot/css/site.css --watch
```

## Docker Compose

| Command | What it does |
|---|---|
| `docker compose up --build` | Build and start everything |
| `docker compose logs -f acxess-web` | Follow the web app logs |
| `docker compose down` | Stop, **keep** the data |
| `docker compose down -v` | Stop and **delete** the data (volumes) |

Local logs: Seq runs at http://localhost:8081.

## Migrations

Each module has its own `DbContext`. Always pass the module project and its context.

Add a migration (example: Membership):

```bash
dotnet ef migrations add <Name> \
  -p ./modules/Acxess.Membership/ -s Acxess.Web \
  -c MembershipModuleContext -o Infrastructure/Persistence/Migrations
```

Apply it:

```bash
ASPNETCORE_ENVIRONMENT=Localhost dotnet ef database update \
  --project ./modules/Acxess.Membership/ --startup-project Acxess.Web \
  --context MembershipModuleContext
```

| Module | Context |
|---|---|
| Identity | `IdentityModuleContext` |
| Catalog | `CatalogModuleContext` |
| Membership | `MembershipModuleContext` |
| Billing | `BillingModuleContext` |
| Marketing | `MarketingModuleContext` |

In production you do not run these. The `acxess-migrator` container applies all migrations on every deploy.

If `dotnet ef` is not found:

```bash
dotnet tool install --global dotnet-ef
export PATH="$PATH:$HOME/.dotnet/tools"
```

## Useful commands

Query the database from the terminal. The function reads the password **inside** the container, so it does not go to your terminal or your shell history:

```bash
sql() {
  docker exec sql_acxess_container bash -c \
    '/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -W -Q "$1"' _ "$1"
}

sql "SELECT name, state_desc FROM sys.databases"
sql "SELECT COUNT(*) FROM AcxessDB.Membership.Members"
```

`Identity` is a reserved word in T-SQL. Write it with brackets: `AcxessDB.[Identity].Tenants`.

| Command | What it does |
|---|---|
| `docker compose ps -a` | All services, also the stopped ones (for example, the migrator after it finishes) |
| `docker stats --no-stream` | Memory and CPU of each container, one time |
| `docker inspect acxess_web --format '{{range .Config.Env}}{{println .}}{{end}}' \| grep '^Name='` | The value of one variable that the running container really got. Careful: without `grep` it prints all variables, secrets too. |

## Tests

```bash
dotnet test Acxess.sln
```

Integration tests start a real SQL Server with Testcontainers, so Docker must be running.

## Docker without sudo (Linux)

```bash
sudo usermod -aG docker $USER
newgrp docker
```
