---
name: project-finledger-overview
description: FinLedger project purpose, tech stack, and high-level architecture
metadata:
  type: project
---

FinLedger is a portfolio-grade financial ledger API + React frontend demonstrating production-grade database patterns (ACID transfers, CQRS, optimistic concurrency, audit log, soft delete, window functions, JSONB, connection pooling) used by fintechs and banks.

**Tech Stack:**
- Backend: C# .NET 9, ASP.NET Core Web API
- ORM (writes): EF Core 9 + Npgsql
- SQL (reads): Dapper
- Mediator: MediatR 12
- Validation: FluentValidation
- Database: PostgreSQL 16
- Frontend: React 18, TypeScript, Vite, TanStack React Query, React Hook Form, Zod, Tailwind CSS, Axios
- Testing: xUnit, FluentAssertions, Testcontainers
- Infrastructure: Docker Compose, pgAdmin 4, Nginx

**Architecture:** Clean Architecture with CQRS read/write split across 4 backend projects:
- FinLedger.API (controllers, middleware, Program.cs)
- FinLedger.Application (MediatR handlers, validators)
- FinLedger.Domain (entities, enums, exceptions, repo interfaces — zero external deps)
- FinLedger.Infrastructure (EF Core, Dapper, interceptors, repositories)

**Why:** Portfolio project demonstrating patterns senior engineers use daily.
**How to apply:** Frame all architectural discussions around the 13 database patterns documented in README.md and docs/database-patterns.md.
