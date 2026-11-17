# Acxess

![.NET](https://img.shields.io/badge/.NET_9-000000?style=flat-square&logo=dotnet&logoColor=512BD4)
![SQL Server](https://img.shields.io/badge/SQL_Server-000000?style=flat-square&logo=microsoftsqlserver&logoColor=CC2927)
![Docker](https://img.shields.io/badge/Docker-000000?style=flat-square&logo=docker&logoColor=2496ED)
![GitHub Actions](https://img.shields.io/badge/GitHub_Actions-000000?style=flat-square&logo=githubactions&logoColor=2088FF)
![Ansible](https://img.shields.io/badge/Ansible-000000?style=flat-square&logo=ansible&logoColor=EE0000)

**Acxess** is a multi-tenant web app for gyms. It manages members, subscriptions, payments and expiration reminders. Two real gyms use it every day.

## How it works

| Part | In short | More |
|---|---|---|
| **App** | .NET 9 modular monolith. Razor Pages with HTMX and Alpine.js. SQL Server and Redis. | [Architecture](docs/architecture.md) |
| **Server** | One Ubuntu VPS. Docker Compose with Caddy (HTTPS), the app, SQL Server and Redis. | [Infrastructure](docs/infrastructure.md) |
| **Deploy** | A GitHub Release builds the image, runs the tests and deploys by SSH. Rollback = deploy an older tag. | [Infrastructure](docs/infrastructure.md#how-a-change-gets-to-production) |
| **Server setup** | Ansible playbook, secrets in Ansible Vault. Tested in a lab VM. | [infrastructure/](infrastructure/README.md) |
| **Backups** | Nightly database backup to Cloudflare R2. A new encrypted design with alerts is in testing. | [Backups](docs/infrastructure.md#backups-and-recovery) |
| **Decisions** | Why we chose each thing, with its cost. | [ADRs](docs/adrs/) |

All documentation: [docs/](docs/README.md).

## Repository

```
src/              application code (.NET), Docker Compose files
infrastructure/   server setup (Ansible), backup scripts, Caddy
docs/             architecture, infrastructure, decisions, runbooks
.github/          CI (pull requests) and CD (releases)
```

## Local development

You need Docker and the [.NET 9 SDK](https://dotnet.microsoft.com/download).

**1. Clone**

```bash
git clone https://github.com/mendevz/Acxess.git
cd Acxess/src
```

**2. Configure**

```bash
cp .env.template .env      # then set your local passwords
```

Create `Acxess.Web/appsettings.Localhost.json`:

```json
{
  "ConnectionStrings": {
    "Default": "Data Source=localhost;Database=AcxessDB;User ID=sa;Password={your_password_from_env};Trust Server Certificate=True;"
  }
}
```

**3. Start the database, Redis and migrations**

```bash
docker compose up -d acxess-db acxess-redis seq
docker compose up --build acxess-migrator
```

**4. Run the app**

```bash
ASPNETCORE_ENVIRONMENT=Localhost dotnet run --project ./Acxess.Web/
```

More commands (migrations, tests, Tailwind): [docs/development.md](docs/development.md).

## Diagrams

**Context (C4)**

<img width="1186" height="270" alt="C4 context diagram of Acxess" src="https://github.com/user-attachments/assets/c0c060fd-6170-4d4d-a58d-78ab8f9a91cd" />

**Deployment**

<img width="1166" height="586" alt="Deployment diagram of Acxess" src="https://github.com/user-attachments/assets/353adbfa-dbda-4896-ad2c-302c2413c8ab" />

The current deployment diagram, as code, is in [docs/infrastructure.md](docs/infrastructure.md).
