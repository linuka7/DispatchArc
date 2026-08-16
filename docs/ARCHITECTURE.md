# DispatchArc Architecture

## Overview

DispatchArc is structured as a layered ASP.NET Core application with explicit separation between HTTP delivery, application workflows, domain rules and infrastructure.

## Projects

### DispatchArc.Api

Responsibilities:

- HTTP endpoints
- request and response contracts
- JWT authentication
- authorization policies
- Swagger / OpenAPI
- ProblemDetails responses
- dependency registration
- application startup
- health checks

The API should not own persistence logic or core business rules.

### DispatchArc.Application

Responsibilities:

- application use cases
- orchestration
- tenant-aware service methods
- workflow coordination
- repository abstractions
- response models

Representative areas include:

- customers
- service jobs
- quotes
- invoices
- payments
- dashboard metrics
- operational alerts
- team members

### DispatchArc.Domain

Responsibilities:

- entities
- enums
- lifecycle rules
- business invariants
- state transitions

Representative entities include:

- Tenant
- AppUser
- Customer
- ServiceJob
- JobNote
- JobLineItem
- Invoice
- InvoiceLineItem
- Payment

### DispatchArc.Infrastructure

Responsibilities:

- Entity Framework Core
- PostgreSQL persistence
- repository implementations
- entity configurations
- migrations
- transactional database operations

## Dependency direction

The intended direction is:

    API
     |
     v
    Application
     |
     v
    Domain

Infrastructure supplies persistence implementations required by the Application layer.

    Application contracts
            ^
            |
    Infrastructure
            |
            v
        PostgreSQL

Domain logic remains independent of HTTP and database concerns.

## Multi-tenancy

Tenant identity is carried in JWT claims and tenant-scoped API routes.

Protected tenant routes use:

    /api/tenants/{tenantId}/...

The `TenantAccess` authorization requirement compares:

- the `tenant_id` JWT claim
- the `{tenantId}` route value

A request succeeds only when both refer to the same tenant.

Repository and service methods also receive the tenant ID so persistence queries remain tenant-scoped.

## Authentication

JWT access tokens contain:

- user ID
- tenant ID
- full name
- email
- role
- token ID

Authentication validates:

- issuer
- audience
- lifetime
- signing key

DispatchArc additionally validates the current database user after token validation.

An existing token is rejected when:

- the user no longer exists
- the user is inactive
- the user's current role no longer matches the token role

## Authorization policies

### TenantAccess

Ensures the authenticated tenant matches the route tenant.

### OwnerOnly

Owner-only business functionality.

### DispatchManagement

Roles:

- Owner
- Dispatcher

Used for operational management workflows.

### TechnicianAccess

Roles:

- Owner
- Dispatcher
- Technician

Used for technician-capable workflow actions.

### FinanceAccess

Roles:

- Owner
- Finance

Used for invoice and payment operations.

### OperationalAlertsAccess

Roles:

- Owner
- Dispatcher
- Finance

The alert service then filters the alert audience according to the authenticated role.

## Job workflow

Service jobs use explicit transitions:

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

Cancellation is handled separately.

Clients do not directly assign arbitrary job status values.

## Scheduling

Scheduling includes:

- technician assignment validation
- active technician validation
- tenant validation
- schedule interval validation
- overlap detection
- back-to-back appointment support

The overlap rule is based on:

    requestedStart < existingEnd
    AND
    requestedEnd > existingStart

## Quotes and invoices

Job pricing is stored as quote line items.

Once the business workflow reaches the relevant locked state, pricing changes are rejected.

Invoices snapshot quote line items so future changes cannot rewrite historical invoice pricing.

Invoice creation transitions the completed service job to `Invoiced`.

## Payments

Payment creation executes inside an explicit database transaction.

Before calculating the remaining balance, DispatchArc acquires a PostgreSQL row lock on the invoice using:

    FOR UPDATE

This serializes concurrent writes to the same invoice.

Payments also use a normalized non-empty reference value with database-level uniqueness per tenant and invoice.

## Operational alerts

Operational alerts are derived live rather than stored as notification rows.

Operations examples:

- approved job requiring scheduling
- scheduled job starting soon
- scheduled job with missed start time

Finance examples:

- completed job requiring invoice
- invoice due soon
- overdue invoice

This keeps the alert feed tied directly to current system state.

## Dashboard

The dashboard derives business metrics from source-of-truth records including:

- customers
- technicians
- jobs
- invoices
- payments

Current date-based dashboard calculations use UTC.

## Persistence

Entity Framework Core maps the domain to PostgreSQL.

Schema changes are tracked through migrations under:

    src/DispatchArc.Infrastructure/Persistence/Migrations

Integration tests run migrations against the configured test database.

## Testing architecture

Integration tests use:

    WebApplicationFactory<Program>

The test application starts the real ASP.NET Core pipeline and uses PostgreSQL rather than replacing the database with an in-memory implementation.

This allows tests to cover behavior such as:

- EF migrations
- PostgreSQL indexes
- row locking
- tenant isolation
- authorization
- full HTTP serialization
- concurrency

## CI

GitHub Actions provisions PostgreSQL 17 and executes:

    dotnet restore
    dotnet build
    dotnet test

This verifies both compilation and integration behavior before changes reach `main`.