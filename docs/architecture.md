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
  - **Event** in the same process (MediatR notification). Example: `SubscriptionPurchased` in Membership → Billing creates the transaction.

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

## Technology

| Area | Choice |
|---|---|
| Web | .NET 9, Razor Pages, HTMX, Alpine.js, Tailwind CSS |
| Data | SQL Server 2022, Entity Framework Core |
| Cache | Redis (idempotency keys and ASP.NET Data Protection keys) |
| Files | Cloudflare R2 (S3 API) for member photos |
| Messages | Twilio SMS ([ADR 0004](adrs/0004-replace-unofficial-whatsapp-with-twilio-sms.md)) |
| Logs and traces | Serilog + BetterStack, OpenTelemetry ([ADR 0003](adrs/0003-use-cloud-based-telemetry-for-production.md)) |
| Tests | xUnit, integration tests with Testcontainers (real SQL Server in Docker) |

## Diagrams

Source files (draw.io) are in [`diagrams/`](diagrams/). Open them with [draw.io](https://app.diagrams.net) or the VS Code extension.

| File | Shows |
|---|---|
| `c4-context.drawio` | Acxess, its users and external systems |
| `database.drawio` | Database tables |
