# Subscriptions Management System

![.NET](https://img.shields.io/badge/.NET_9-000000?style=flat-square&logo=dotnet&logoColor=512BD4)
![Docker](https://img.shields.io/badge/Docker-000000?style=flat-square&logo=docker&logoColor=2496ED)
![SQL Server](https://img.shields.io/badge/SQL_Server-000000?style=flat-square&logo=microsoftsqlserver&logoColor=CC2927)
![GitHub Actions](https://img.shields.io/badge/GitHub_Actions-000000?style=flat-square&logo=githubactions&logoColor=2088FF)


**Acxess** is a multi-tenant SaaS application designed for businesses that relies on subscriptions to manage memberships, and streamline their daily operations.

Built with a focus on good practices of programing such as: SOLID principles, design patterns, Clean Architecture & Domain Driven Design.  

## Tech Stack

### Architecture
The web application is built using .NET 9 Razor Pages Web Apps and **Modular Monolith** as its software architecture, ensuring clear boundaries between entities domains while keeping development process simple.

### Backend / Frontend (Server Side Rendering)
- **Framework:** .NET 9 (C#, HTML, CSS, Javascript)
- **User interface:** App Web Razor Pages 
- **Architecture:** Clean Architecture Vertical Slice with Domain Driven Design (DDD) principles.
- **Database:** SQL Server
- **ORM:** Entity Framework Core
- **Authentication:** ASP.NET Core Identity
- **Libraries:** Tailwind CSS, HTMX & Alpine.js

### Infrastructure
- **Server:** VPS Server
- **OS:** Ubuntu

### CI/CD Github Actions, GitHub Container Registry
- **Immutable Releases:** Deployments are triggered automatically when a new GitHub Release is published.
- **Workflow:** Builds and deploys the image to GCR, connects to the VPS via SSH, and runs the Docker containers using bash scripts.
- **Rollback Strategy:** Manual rollbacks can be executed instantly via GitHub Actions Workflow Dispatch by providing a previous version tag.

## Local Development Setup

To run this project locally, you will need the Docker and [.NET 9 SDK](https://dotnet.microsoft.com/download).

### 1. Clone the repository
```bash
git clone https://github.com/Maumedev/Acxess.git
cd Acxess/src
```

### 2. Environment Configuration

* Copy the `.env.template` file to `.env` and fill in your local database credentials.

```bash
cp .env.template .env
```

* Create an appsettings.Localhost.json file in /Acxess.Web/ with the following content:

```json
{
   "ConnectionStrings": {
        "Default": "Data Source=localhost;Database=AcxessDB;User ID=sa;Password={your_password_from_env};Trust Server Certificate=True;"
    }
}
```

### 3 Run docker-compose.yml
Execute the following command to create the SQL Server container and run database migrations:

```bash
docker compose --profile tools up --build -d
```

### 4. Run the application
Use the .NET CLI to run the application or targeting Localhost environment configuration:

```bash
ASPNETCORE_ENVIRONMENT=Localhost dotnet run --project ./Acxess.Web/
```
