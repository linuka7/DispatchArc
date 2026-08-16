# DispatchArc

DispatchArc is a multi-tenant field-service operations platform built with ASP.NET Core, Entity Framework Core and PostgreSQL.

The backend covers the full service workflow from customer intake and job creation through technician assignment, scheduling, quotes, invoicing, payments, dashboards and operational alerts.

## Core capabilities

- Multi-tenant business isolation
- JWT authentication
- Role-based authorization
- Owner, Dispatcher, Technician and Finance roles
- Team-member management
- Customer management
- Service-job workflow
- Technician assignment
- Conflict-aware scheduling
- Job notes and technician work updates
- Quotes and pricing line items
- Invoice management
- Payment tracking
- Concurrency-safe payment processing
- Business dashboard metrics
- Operational alerts
- Swagger / OpenAPI documentation
- PostgreSQL persistence
- Entity Framework Core migrations
- Integration test coverage
- GitHub Actions CI

## Technology stack

| Layer | Technology |
| --- | --- |
| API | ASP.NET Core Web API |
| Runtime | .NET 10 |
| Application | C# |
| ORM | Entity Framework Core |
| Database | PostgreSQL 17 |
| Authentication | JWT Bearer |
| Password hashing | ASP.NET Core Identity PasswordHasher |
| API documentation | Swagger / OpenAPI |
| Containers | Docker Compose |
| Testing | xUnit + WebApplicationFactory |
| CI | GitHub Actions |

## Architecture

DispatchArc follows a layered architecture:

    DispatchArc.Api
          |
          v
    DispatchArc.Application
          |
          v
    DispatchArc.Domain

    DispatchArc.Infrastructure
          |
          +---- implements persistence contracts
          |
          v
       PostgreSQL

The API layer owns HTTP concerns such as authentication, authorization, routes, request contracts and response contracts.

The Application layer coordinates use cases and business workflows.

The Domain layer contains core entities, enums and domain rules.

The Infrastructure layer implements repositories, Entity Framework Core persistence and PostgreSQL integration.

For more detail, see `docs/ARCHITECTURE.md`.

## Repository structure

    DispatchArc/
    |
    +-- src/
    |   +-- DispatchArc.Api/
    |   +-- DispatchArc.Application/
    |   +-- DispatchArc.Domain/
    |   +-- DispatchArc.Infrastructure/
    |
    +-- tests/
    |   +-- DispatchArc.IntegrationTests/
    |
    +-- docs/
    |   +-- API.md
    |   +-- ARCHITECTURE.md
    |
    +-- .github/workflows/
    |   +-- ci.yml
    |
    +-- compose.yml
    +-- DispatchArc.sln

## Prerequisites

Install:

- .NET 10 SDK
- Docker Desktop
- Git
- PowerShell

## Local database

Copy the example environment file:

    Copy-Item .env.example .env

Then start PostgreSQL:

    docker compose up -d

Check the container:

    docker compose ps

The development PostgreSQL service listens on:

    localhost:5432

## Application configuration

The application expects the database connection string and JWT signing key to be supplied securely.

From the repository root:

    dotnet user-secrets set `
      --project src\DispatchArc.Api `
      "ConnectionStrings:Database" `
      "Host=localhost;Port=5432;Database=dispatcharc;Username=dispatcharc_dev;Password=CHANGE_ME"

Set a development JWT key:

    dotnet user-secrets set `
      --project src\DispatchArc.Api `
      "Jwt:Key" `
      "DispatchArc_Local_Development_Jwt_Key_Change_This_Immediately_2026"

The non-secret JWT issuer and audience defaults are already defined in application configuration.

Never commit production database passwords or JWT signing keys.

## Database migrations

Apply migrations:

    dotnet ef database update `
      --project src\DispatchArc.Infrastructure `
      --startup-project src\DispatchArc.Api

List migrations:

    dotnet ef migrations list `
      --project src\DispatchArc.Infrastructure `
      --startup-project src\DispatchArc.Api

## Run the API

    dotnet run --project src\DispatchArc.Api

Development endpoints:

- HTTPS API: `https://localhost:7145`
- HTTP API: `http://localhost:5006`
- Swagger UI: `https://localhost:7145/swagger`
- OpenAPI JSON: `https://localhost:7145/swagger/v1/swagger.json`
- Database health: `https://localhost:7145/api/health/database`

## Authentication flow

A typical development flow is:

1. Create a tenant.
2. Register the tenant owner.
3. Login to receive a JWT access token.
4. Open Swagger.
5. Click **Authorize**.
6. Enter the JWT access token.
7. Call tenant-scoped endpoints.

JWTs contain the authenticated user's identity, tenant and role.

DispatchArc also validates the current user against the database when an authenticated token is used. Tokens belonging to inactive, removed or role-changed users are rejected.

## Roles

DispatchArc currently defines four roles:

| Role | Primary responsibility |
| --- | --- |
| Owner | Full business oversight and privileged administration |
| Dispatcher | Operational job, customer, scheduling and team workflows |
| Technician | Assigned field-work and technician-update workflows |
| Finance | Invoicing, payments and financial operations |

Authorization policies are intentionally narrower than simple role names. See `docs/API.md` for the access model.

## Service-job lifecycle

The primary workflow is:

    New
      |
      v
    Quoted
      |
      v
    Approved
      |
      v
    Scheduled
      |
      v
    InProgress
      |
      v
    Completed
      |
      v
    Invoiced

Jobs may also move to:

    Cancelled

Workflow transitions are enforced by domain and application rules rather than trusting client-provided status values.

## Payment safety

Payment processing includes:

- remaining-balance validation
- append-only payment records
- invoice payment-state updates
- PostgreSQL transaction boundaries
- invoice row locking
- concurrent overpayment prevention
- normalized reference matching
- database-level duplicate reference protection
- payment timestamp validation

## API documentation

Interactive documentation is available in Development through Swagger.

Detailed endpoint groups and authorization information are documented in:

    docs/API.md

## Tests

Run the full solution test suite:

    dotnet test DispatchArc.sln

Run integration tests directly:

    dotnet test `
      tests\DispatchArc.IntegrationTests\DispatchArc.IntegrationTests.csproj

Integration tests require a PostgreSQL database through:

    DISPATCHARC_TEST_DATABASE

The test host applies Entity Framework Core migrations automatically before integration tests execute.

## Continuous integration

GitHub Actions runs automatically for:

- pull requests
- pushes to `main`

CI provisions PostgreSQL, restores dependencies, builds the solution and runs the complete test suite.

## Health check

Database connectivity can be checked through:

    GET /api/health/database

A healthy response indicates that DispatchArc can connect to PostgreSQL.

## Security notes

- Do not commit JWT signing keys.
- Do not commit database passwords.
- Tenant-scoped routes validate the JWT tenant against the route tenant.
- Role-based policies protect privileged operations.
- Existing tokens are checked against current user state.
- Financial writes use transaction and concurrency safeguards.

## Current backend scope

The current repository contains the backend platform and API.

A dispatcher-facing frontend/dashboard is a separate future phase.

## License

No open-source license has been declared for this repository.