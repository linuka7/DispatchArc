# DispatchArc API Guide

## Base URLs

Development HTTPS:

    https://localhost:7145

Development HTTP:

    http://localhost:5006

Swagger UI:

    https://localhost:7145/swagger

OpenAPI document:

    https://localhost:7145/swagger/v1/swagger.json

## Authentication

DispatchArc uses JWT Bearer authentication.

Typical sequence:

    POST /api/tenants
    POST /api/auth/register
    POST /api/auth/login

The login/register response includes an access token.

Login requires only the user's email and password. The API resolves the user's
tenant from the account and includes that tenant in the JWT; clients do not
need to submit a tenant ID when signing in.

For a raw HTTP request:

    Authorization: Bearer <access-token>

In Swagger, click **Authorize** and enter the token.

## Public endpoints

| Method | Route | Purpose |
| --- | --- | --- |
| POST | `/api/tenants` | Create a tenant |
| GET | `/api/tenants/{tenantId}` | Get a tenant |
| POST | `/api/auth/register` | Register the tenant owner |
| POST | `/api/auth/login` | Authenticate and receive JWT |

## Authentication endpoint

| Method | Route | Access |
| --- | --- | --- |
| GET | `/api/auth/me` | Authenticated user |

## Tenant-scoped route model

Most business endpoints use:

    /api/tenants/{tenantId}/...

The authenticated JWT tenant must match the `{tenantId}` route value.

A user cannot access another tenant by changing the route ID.

## Authorization policies

| Policy | Roles |
| --- | --- |
| `OwnerOnly` | Owner |
| `DispatchManagement` | Owner, Dispatcher |
| `TechnicianAccess` | Owner, Dispatcher, Technician |
| `FinanceAccess` | Owner, Finance |
| `OperationalAlertsAccess` | Owner, Dispatcher, Finance |

Tenant-scoped controllers additionally use `TenantAccess`.

## Customers

Base route:

    /api/tenants/{tenantId}/customers

| Method | Route | Purpose |
| --- | --- | --- |
| POST | `/customers` | Create customer |
| GET | `/customers` | List/search customers |
| GET | `/customers/{customerId}` | Get customer |

Customer creation requires dispatch-management access.

## Team members

Base route:

    /api/tenants/{tenantId}/team-members

| Method | Route | Purpose |
| --- | --- | --- |
| POST | `/team-members` | Create team member |
| GET | `/team-members` | List team members |
| GET | `/team-members/{userId}` | Get team member |

The controller requires dispatch-management access.

Additional owners cannot be created through the team-member endpoint.

## Service jobs

Base route:

    /api/tenants/{tenantId}/jobs

| Method | Route | Purpose |
| --- | --- | --- |
| POST | `/jobs` | Create service job |
| GET | `/jobs` | List jobs |
| GET | `/jobs/{jobId}` | Get job |
| POST | `/jobs/{jobId}/quote` | Mark job quoted |
| POST | `/jobs/{jobId}/approve` | Approve job |
| POST | `/jobs/{jobId}/assign-technician` | Assign technician |
| POST | `/jobs/{jobId}/schedule` | Schedule job |
| POST | `/jobs/{jobId}/start` | Start job |
| POST | `/jobs/{jobId}/complete` | Complete job |
| POST | `/jobs/{jobId}/cancel` | Cancel job |

Job-list requests may use status/search query filtering.

## Quote pricing

Base route:

    /api/tenants/{tenantId}/jobs/{jobId}/quote

| Method | Route | Purpose |
| --- | --- | --- |
| GET | `/quote` | Get quote |
| POST | `/quote/line-items` | Add line item |
| PUT | `/quote/line-items/{lineItemId}` | Update line item |
| DELETE | `/quote/line-items/{lineItemId}` | Delete line item |

Pricing changes require dispatch-management access and are limited by job workflow state.

## Job notes and technician updates

Job notes provide an append-only work timeline.

Two note categories are currently used:

- InternalNote
- TechnicianUpdate

Owner and Dispatcher users can work with the broader note timeline.

Technicians are restricted to technician updates on jobs assigned to them.

## Invoices

Routes:

    POST /api/tenants/{tenantId}/jobs/{jobId}/invoice
    GET  /api/tenants/{tenantId}/jobs/{jobId}/invoice
    GET  /api/tenants/{tenantId}/invoices/{invoiceId}

Invoice operations require `FinanceAccess`.

Invoice creation requires a valid completed job and quote data.

## Payments

Base route:

    /api/tenants/{tenantId}/invoices/{invoiceId}/payments

| Method | Route | Purpose |
| --- | --- | --- |
| GET | `/payments` | Get invoice payment summary |
| POST | `/payments` | Record payment |

Payment operations require `FinanceAccess`.

Payment writes enforce remaining-balance and duplicate-reference rules.

## Dashboard

    GET /api/tenants/{tenantId}/dashboard

Requires:

    OwnerOnly

The dashboard returns aggregate business metrics.

## Operational alerts

    GET /api/tenants/{tenantId}/alerts

Allowed roles:

- Owner
- Dispatcher
- Finance

The returned audience is derived from the authenticated role.

Owner receives both operations and finance alerts.

Dispatcher receives operations alerts.

Finance receives finance alerts.

## Health

    GET /api/health/database

Checks PostgreSQL connectivity.

## HTTP response conventions

DispatchArc uses standard HTTP response codes.

Common examples:

| Status | Meaning |
| --- | --- |
| 200 | Successful request |
| 201 | Resource created |
| 204 | Successful operation with no body |
| 400 | Invalid request |
| 401 | Authentication required or token rejected |
| 403 | Authenticated but not authorized |
| 404 | Tenant-scoped resource not found |
| 409 | Business-state conflict |

Validation and business errors are represented through RFC-style `ProblemDetails` / `ValidationProblemDetails` responses.

Typical shape:

    {
      "title": "Invalid payment request",
      "status": 400,
      "detail": "..."
    }

## OpenAPI operation IDs

Controller operations expose stable OpenAPI operation IDs based on:

    Controller_Action

Examples:

    Auth_Login
    Jobs_Create
    Payments_Record

These IDs make generated clients and API tooling more predictable.

## Tenant isolation

Tenant isolation exists at multiple layers:

1. JWT includes a tenant claim.
2. Tenant-scoped routes include `{tenantId}`.
3. `TenantAccess` compares the claim and route.
4. Application and repository calls receive tenant ID.
5. Queries filter tenant-owned records.

Do not remove tenant filtering merely because route authorization already exists.

## Time handling

Business timestamps are stored and processed as UTC `DateTimeOffset` values.

Clients should submit ISO 8601 timestamps with an explicit offset or UTC `Z`.

## Enum serialization

API enums are serialized using their names rather than raw numeric values.

Examples include:

    Owner
    Dispatcher
    Technician
    Finance

and job/payment/invoice status values.

## Swagger usage

Start the API:

    dotnet run --project src\DispatchArc.Api

Open:

    https://localhost:7145/swagger

Use the authentication endpoints to obtain a JWT, then click **Authorize**.

Swagger is enabled in the Development environment.