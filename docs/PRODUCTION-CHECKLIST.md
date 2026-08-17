# DispatchArc Production Readiness Checklist

This checklist covers the database and environment requirements
that must be satisfied before deploying DispatchArc.

## Environment

- [ ] `ASPNETCORE_ENVIRONMENT` is set to `Production`
- [ ] no real production secrets exist in Git
- [ ] database connection string is supplied by the host platform
- [ ] JWT signing key is supplied by the host platform
- [ ] JWT signing key is cryptographically random
- [ ] JWT signing key is at least 48 characters
- [ ] JWT issuer is configured
- [ ] JWT audience is configured
- [ ] JWT expiration is between 5 and 1440 minutes

## PostgreSQL

- [ ] production PostgreSQL instance has been provisioned
- [ ] database credentials use a dedicated application user
- [ ] database is not publicly exposed unless explicitly required
- [ ] TLS/SSL is enabled when required by the database provider
- [ ] backups are enabled
- [ ] backup retention has been reviewed
- [ ] restore procedure is known before production launch
- [ ] database storage is persistent

## Schema migrations

DispatchArc intentionally does not automatically apply EF Core
migrations during normal API startup.

Before deploying a new application version:

1. back up the production database when appropriate
2. review pending EF Core migrations
3. generate or inspect the migration SQL
4. apply migrations as an explicit deployment step
5. confirm migration success
6. deploy/start the API
7. verify the readiness endpoint

Useful commands:

    dotnet ef migrations list `
      --project src\DispatchArc.Infrastructure `
      --startup-project src\DispatchArc.Api

Generate an idempotent migration script:

    dotnet ef migrations script `
      --idempotent `
      --project src\DispatchArc.Infrastructure `
      --startup-project src\DispatchArc.Api `
      --output artifacts\dispatcharc-migrations.sql

Apply migrations directly when appropriate:

    dotnet ef database update `
      --project src\DispatchArc.Infrastructure `
      --startup-project src\DispatchArc.Api

The final deployment pipeline will decide whether migration SQL,
an EF migration bundle, or another controlled migration mechanism
is used in production.

## Reverse proxy

If TLS terminates before the application:

- [ ] `ReverseProxy__Enabled=true`
- [ ] every trusted proxy IP is explicitly configured
- [ ] untrusted forwarded headers are not accepted
- [ ] `X-Forwarded-Proto` correctly reports HTTPS
- [ ] HTTPS redirection does not create a redirect loop

If the API terminates HTTPS itself:

- [ ] reverse proxy handling remains disabled unless required

## Health

Liveness:

    GET /api/health/live

Use this to determine whether the process is running.

Readiness:

    GET /api/health/ready

Use this to determine whether the API can currently reach
PostgreSQL and is ready for normal traffic.

Existing database health endpoint:

    GET /api/health/database

## Logging

Production logging should:

- avoid logging secrets
- avoid logging JWT access tokens
- avoid logging database passwords
- retain enough information for operational diagnosis
- use the deployment platform's log collection where available

## Production smoke test

After deployment verify:

- [ ] `/api/health/live` returns HTTP 200
- [ ] `/api/health/ready` returns HTTP 200
- [ ] authentication works
- [ ] authenticated tenant access works
- [ ] cross-tenant access is rejected
- [ ] authorized role access works
- [ ] unauthorized role access is rejected
- [ ] database writes persist
- [ ] payment workflow still behaves correctly
- [ ] application restart does not lose data

## Rollback

Before the first production release, define:

- application rollback procedure
- database rollback/forward-fix procedure
- backup restoration procedure
- responsible production operator

Do not automatically roll back a database migration without first
understanding whether the migration is destructive or whether newer
application writes depend on the new schema.