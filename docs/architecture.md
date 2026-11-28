# Architecture

How the Acxess application is built. For servers, deploys and backups, see [infrastructure.md](infrastructure.md).

## In one paragraph

Acxess is a **modular monolith**: one .NET 9 web app, split into modules with clear borders. Each module owns its data. Pages are rendered on the server (Razor Pages) and made interactive with HTMX and Alpine.js. One app serves many gyms (**multi-tenant**): each gym is a tenant, and its data is filtered automatically.

## Modules

| Module | Owns |
|---|---|
| `Identity` | Users, login, tenants (gyms) |
| `Catalog` | Plans, access tiers, add-ons |
| `Membership` | Members, subscriptions, dashboard, daily expiration reminders |
| `Billing` | Payments and transactions |
| `Marketing` | Promotions and coupons |

Code: [`src/modules/`](../src/modules). The web host is [`src/Acxess.Web`](../src/Acxess.Web).

### Rules between modules

- A module **never references another module**. They only share contracts in [`src/shared/Acxess.Shared`](../src/shared/Acxess.Shared).
- Each module has its **own `DbContext` and its own SQL schema**. One database, separate schemas.
- Modules talk in two ways:
  - **Direct call** through an integration service interface (`ICatalogIntegrationService`, `IBillingIntegrationService`, `IIdentityIntegrationService`).
  - **Event** in the same process (MediatR notification):

| Event | Sent by | Handled by |
|---|---|---|
| `SubscriptionPurchased` | Membership (new member, renewal) | Billing: creates the transaction |
| `TenantCreated` | Identity (a new gym) | Catalog: creates the default add-ons (inscription and visit) |

## Inside a module

```
Domain/          entities and rules (no framework code)
Application/
  Features/<Feature>/
    Commands/    change data
    Queries/     read data
Infrastructure/  DbContext, migrations, external services
```

Each feature is a **vertical slice**: one request and one MediatR handler, with everything it needs in one folder. Errors are returned as `Result`, not thrown as exceptions.

Every request goes through the same pipeline (MediatR behaviors in [`src/shared/Acxess.Infrastructure`](../src/shared/Acxess.Infrastructure)): logging, tenant, tracing, idempotency (Redis), database errors and transactions.

## In production

The same Docker image runs in two containers ([`docker-compose.prod.yml`](../src/docker-compose.prod.yml)):

| Container | What it does |
|---|---|
| `acxess-migrator` | Starts with `--migrate-only`: applies the migrations of every module and runs the seeders, then stops. |
| `acxess-web` | Starts only after the migrator ends with success (`RUN_MIGRATIONS=false`). Serves the app. |

**Background job.** `DailyExpirationReportWorker` (Membership) runs inside `acxess-web`. Every day at 08:30, in the time zone of each gym, it sends the expiration reminders by SMS. Only **one** `acxess-web` may run: two servers with the app up would send every SMS twice.

**Configuration.** `appsettings.json`, then environment variables on top. In production the variables come from `/opt/acxess/.env`, written by Ansible from the Vault ([ADR 0006](adrs/0006-ansible-vault-is-the-only-source-of-secrets.md)), and `docker-compose.prod.yml` maps them to .NET names (for example `ConnectionStrings__Default`). Known gap: the BetterStack token is still in `appsettings.json`.

## Technology

| Area | Choice |
|---|---|
| Web | .NET 9, Razor Pages, HTMX, Alpine.js, Tailwind CSS |
| Data | SQL Server 2022, Entity Framework Core |
| Cache | Redis (idempotency keys and ASP.NET Data Protection keys) |
| Files | Cloudflare R2 (S3 API) for member photos |
| Messages | Twilio SMS ([ADR 0004](adrs/0004-replace-unofficial-whatsapp-with-twilio-sms.md)) |
| Logs and traces | Serilog + BetterStack, OpenTelemetry ([ADR 0003](adrs/0003-use-cloud-based-telemetry-for-production.md)) |
| Tests | xUnit: [`src/tests/Acxess.UnitTests`](../src/tests/Acxess.UnitTests) and [`Acxess.IntegrationTests`](../src/tests/Acxess.IntegrationTests) with Testcontainers (real SQL Server in Docker) |

## Diagrams

Source files (draw.io) are in [`diagrams/`](diagrams/). Open them with [draw.io](https://app.diagrams.net) or the VS Code extension.

| File | Shows |
|---|---|
| `c4-context.drawio` | Acxess, its users and external systems |
| `database.drawio` | Database tables |
