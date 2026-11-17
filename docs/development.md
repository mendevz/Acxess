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
