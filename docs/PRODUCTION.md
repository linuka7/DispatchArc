# DispatchArc Production Configuration

## Overview

Production configuration is supplied through environment
variables or the deployment platform's secret manager.

Real database passwords and JWT signing keys must never be
committed to the repository.

`production.env.example` documents the required environment
variable names without containing real credentials.

## Required settings

### Environment

    ASPNETCORE_ENVIRONMENT=Production

### PostgreSQL

    ConnectionStrings__Database

Example structure:

    Host=<host>;
    Port=5432;
    Database=dispatcharc;
    Username=<user>;
    Password=<secret>

Use the connection string supplied by the production PostgreSQL
provider whenever possible.

## JWT

Required values:

    Jwt__Issuer
    Jwt__Audience
    Jwt__Key
    Jwt__ExpirationMinutes

Production JWT signing keys must contain at least 48 characters.

A cryptographically random secret should be supplied by the
deployment platform's secret manager.

Accepted token expiration is between 5 and 1440 minutes.

## Production startup validation

DispatchArc fails startup when:

- the database connection string is missing
- JWT issuer is missing
- JWT audience is missing
- the JWT signing key is missing or too short
- JWT expiration is outside the accepted range
- known development/placeholder secret markers are detected

Failing startup is intentional. It prevents an incorrectly
configured production instance from accepting traffic.

## PostgreSQL resilience

Production enables EF Core/Npgsql transient database retries.

Current policy:

    maximum retries: 5
    maximum retry delay: 10 seconds
    command timeout: 30 seconds

Explicit financial transactions use the EF Core execution strategy,
so payment locking and transaction boundaries remain compatible
with transient retry behavior.

## Reverse proxy deployment

Many cloud platforms terminate TLS at a reverse proxy or load
balancer before forwarding traffic to the application.

DispatchArc supports trusted forwarded headers:

    X-Forwarded-For
    X-Forwarded-Proto

Enable this with:

    ReverseProxy__Enabled=true

Trusted proxies must then be explicitly configured:

    ReverseProxy__KnownProxies__0=<proxy-ip>

Additional addresses can be configured by increasing the numeric
index.

DispatchArc does not automatically trust arbitrary forwarded
headers because a client could otherwise spoof its original IP or
scheme.

Forwarded headers are processed before HTTPS redirection.

## HTTPS and HSTS

Outside Development, DispatchArc enables:

- centralized exception handling
- HTTP Strict Transport Security (HSTS)
- HTTPS redirection

When TLS terminates at a reverse proxy, configure that proxy
correctly so DispatchArc receives the original HTTPS scheme through
trusted forwarded headers.

## Health endpoints

### Liveness

    GET /api/health/live

Checks whether the application process is running.

It deliberately does not require PostgreSQL connectivity.

### Readiness

    GET /api/health/ready

Checks whether DispatchArc is ready to serve database-dependent
traffic.

Readiness currently verifies PostgreSQL connectivity.

### Database compatibility endpoint

The existing endpoint remains available:

    GET /api/health/database

## Deployment-platform secrets

Prefer the host platform's encrypted secret/environment manager.

Never place real production values in:

- `appsettings.json`
- `appsettings.Production.json`
- `production.env.example`
- source code
- Git commits

## Database migrations

Production database migrations should be executed as an explicit
deployment step rather than automatically from normal API startup.

The application should only begin serving traffic after required
schema migrations have succeeded.

The exact migration/deployment pipeline is implemented in the
deployment milestone.
## Production readiness checklist

Before the first live deployment, work through:

    docs/PRODUCTION-CHECKLIST.md

The checklist covers secrets, PostgreSQL, backups, migrations,
reverse-proxy configuration, health verification and rollback
planning.