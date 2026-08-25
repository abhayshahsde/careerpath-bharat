# CareerPath Bharat Backend

## Stack

- ASP.NET Core 8 Web API (Minimal APIs)
- C# + Dapper (no Entity Framework)
- SQL Server (15 schemas)
- MediatR + FluentValidation
- Serilog (structured logging)
- JWT Bearer authentication
- Scalar UI (OpenAPI)
- Docker Compose (SQL Server + RabbitMQ)

## Quick Start (local dev)

### Prerequisites

- .NET 8 SDK
- Docker Desktop

### 1. Start dependencies

```bash
docker compose up -d
```

Wait for SQL Server health check to pass (~30 seconds).

### 2. Run the API

```bash
dotnet run --project apps/api/CareerPath.Api
```

Migrations run automatically on first startup in Development mode.

### 3. Explore the API

- Scalar UI: http://localhost:5000/scalar
- OpenAPI JSON: http://localhost:5000/openapi/v1.json
- Health (live): http://localhost:5000/health/live
- Health (ready): http://localhost:5000/health/ready
- Careers: http://localhost:5000/api/v1/careers

### 4. Run tests

```bash
# Unit tests (no Docker needed)
dotnet test apps/api/CareerPath.Tests.Unit

# Integration tests (Docker SQL Server must be running)
dotnet test apps/api/CareerPath.Tests.Integration
```

## Project Structure

```
backend/
  apps/
    api/
      CareerPath.Api            ← Web API (endpoints, middleware, DI composition root)
      CareerPath.Application    ← Use cases, validators, MediatR handlers
      CareerPath.Domain         ← Entities, value objects, permissions (no infra deps)
      CareerPath.Infrastructure ← Dapper, SQL, repository implementations
      CareerPath.Contracts      ← Versioned request/response models
      CareerPath.Migrations     ← Numbered SQL scripts (embedded resources)
      CareerPath.Tests.Unit     ← xUnit unit tests
      CareerPath.Tests.Integration ← xUnit integration tests (Testcontainers)
    worker/
      CareerPath.Worker         ← Background job worker host
  docker-compose.yml
  CareerPath.sln
```

## Architecture Rules

- No Entity Framework — Dapper only
- No `SELECT *` — explicit column lists in all queries
- No raw exception responses — Problem Details (RFC 7807)
- No secrets in code or repository
- Cancellation tokens on all I/O
- UTC DateTimeOffset everywhere
- Parameterized SQL only

## Implemented Phases

- [x] Phase 1: Foundation — solution, infrastructure, identity skeleton, career APIs, student profile, health checks, Docker
- [ ] Phase 2: SQL Server (full schema)
- [ ] Phase 3: Identity (registration, login, refresh-token rotation)
- [ ] Phase 4: Catalog (courses, exams, skills, scholarships)
- [ ] Phase 5–16: See architecture pack

## Default Seed Data (dev only)

Three published careers are seeded: Software Engineer, Medical Doctor, IAS Officer.
Access them at `/api/v1/careers`.
