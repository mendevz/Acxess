# Acxess - Subscriptions Management System

![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![Docker](https://img.shields.io/badge/docker-%230db7ed.svg?style=for-the-badge&logo=docker&logoColor=white)
![SQL Server](https://img.shields.io/badge/SQL_Server-CC2927?style=for-the-badge&logo=microsoft-sql-server&logoColor=white)
![GitHub Actions](https://img.shields.io/badge/github%20actions-%232671E5.svg?style=for-the-badge&logo=githubactions&logoColor=white)


**Acxess** is a SaaS multi-tenant designed for business that relies on subscriptions to manage memberships, and streamline their daily operations.

Built with a focus on good practices of programing how: SOLID principles, design patterns, Clean Architecture & Domain Driven Design.  

## Tech Stack

### Architecture
The web application is build using .NET 9 Razor Pages Web Apps and **Modular Monolith** has software architecture, ensuring clear boundaries between entities domains while keeping deployment.

### Backend / Frontend (Server Side Rendering)
**Framework:** .NET 9 (C#, HTML, CSS, Javascript)
**User interface:*App Web Razor Pages* 
**Architecture:** Clean Architecture Vertical Slice with Domain Driven Design (DDD) principles.
**Database:** SQL Server
**ORM:** Entity Framework Core
**Authentication:** ASP.NET Core Identity
**Libraries:**Tailwind CSS, HTMX & Alpine.js

### Infrastructure
**Server:** VPS Server
**SO:** Ubuntu

### CI/CD Github Actions, GitHub Container Registry
**Immutable Releases:** Deployments are triggered automatically when a new GitHub Release is published.
**Principals:** Build and deploy image to GCR, ssh connection to VPS by bash and run docker.
**Rollback Strategy:** Manual rollbacks can be executed instantly via GitHub Actions Workflow Dispatch by providing a previous version tag.

## Local Development Setup

To run this project locally, you will need the Docker and [.NET 9 SDK](https://dotnet.microsoft.com/download).

### 1. Clone the repository
```bash
git clone [https://github.com/Maumedev/Acxess.git](https://github.com/Maumedev/Acxess.git)
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
