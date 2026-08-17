# DispatchArc Railway Deployment

DispatchArc is deployed as a Dockerized ASP.NET Core API with a
PostgreSQL database.

## Production services

The Railway project contains:

1. DispatchArc API
2. PostgreSQL

The API is built from the repository root Dockerfile.

## Required environment variables

Set these on the API service:

    ASPNETCORE_ENVIRONMENT=Production

    ConnectionStrings__Database=<Railway PostgreSQL connection string>

    Jwt__Issuer=DispatchArc.Api

    Jwt__Audience=DispatchArc.Client

    Jwt__Key=<cryptographically random key at least 48 characters>

    Jwt__ExpirationMinutes=60

Do not commit the real database password or JWT signing key.

## PostgreSQL

Provision PostgreSQL inside the same Railway project.

The API should use a Railway reference variable rather than copying
the password manually whenever possible.

DispatchArc expects an Npgsql-style PostgreSQL connection string.

## Database migrations

Normal API startup never automatically modifies the production
database schema.

Railway runs this explicit pre-deploy command:

    dotnet DispatchArc.Api.dll --migrate

The command:

1. loads production configuration
2. opens a scoped DispatchArcDbContext
3. applies pending EF Core migrations
4. exits successfully
5. allows Railway to continue the deployment

If migration fails, the deployment must not proceed.

## Health check

Railway uses:

    /api/health/ready

The endpoint returns HTTP 200 only when DispatchArc can connect to
PostgreSQL.

## Production image

The root Dockerfile is the production image source.

The final container runs as the built-in non-root `app` user.

The service listens on Railway's injected `PORT`.

## Deployment flow

A normal production release is:

    GitHub main push
          |
          v
    DispatchArc GitHub CI
          |
          v
    Railway Docker build
          |
          v
    Pre-deploy EF migrations
          |
          v
    Start API container
          |
          v
    /api/health/ready
          |
          v
    New deployment becomes active

## Smoke checks

After deployment verify:

    GET /api/health/live
    GET /api/health/ready
    GET /api/health/database

Expected result:

    HTTP 200

Swagger remains disabled in Production.

## Rollback

Application rollback can use Railway deployment rollback/redeploy.

Database migrations must be treated separately. Do not blindly
reverse a production schema migration when newer application data
may already depend on it.