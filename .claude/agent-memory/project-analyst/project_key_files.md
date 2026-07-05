---
name: project-key-files
description: Critical file locations in the FinLedger project for quick navigation
metadata:
  type: project
---

**Entry points:**
- Backend startup: `backend/src/FinLedger.API/Program.cs` — wires DI, auto-migrates on startup, configures middleware
- Frontend entry: `frontend/src/main.tsx` — React root with QueryClientProvider wrapping App

**Domain layer (zero external deps):**
- `backend/src/FinLedger.Domain/Entities/Account.cs` — aggregate with Credit/Debit/SoftDelete behaviors; xmin concurrency token
- `backend/src/FinLedger.Domain/Entities/Transaction.cs` — immutable insert-only ledger record
- `backend/src/FinLedger.Domain/Entities/AuditLog.cs` — written only by AuditInterceptor, never by app code
- `backend/src/FinLedger.Domain/Interfaces/IAccountRepository.cs` — GetByIdForUpdateAsync triggers SELECT FOR UPDATE

**Most important backend file:**
- `backend/src/FinLedger.Application/Transactions/Commands/Transfer/TransferCommandHandler.cs` — deadlock-safe ACID transfer via ordered GUID locking + SELECT FOR UPDATE + UoW transaction

**Infrastructure key files:**
- `backend/src/FinLedger.Infrastructure/Persistence/Interceptors/AuditInterceptor.cs` — SaveChanges interceptor that writes audit_logs in the same DB transaction
- `backend/src/FinLedger.Infrastructure/Persistence/Repositories/StatementRepository.cs` — Dapper + CTE + SUM() OVER window function for running balance
- `backend/src/FinLedger.Infrastructure/Persistence/Repositories/AccountRepository.cs` — GetByIdForUpdateAsync uses raw SQL "SELECT ... FOR UPDATE"
- `backend/src/FinLedger.Infrastructure/Persistence/UnitOfWork.cs` — wraps EF SaveChanges+CommitAsync; translates DbUpdateConcurrencyException → ConcurrencyConflictException
- `backend/src/FinLedger.Infrastructure/Persistence/Configurations/AccountConfiguration.cs` — xmin token, soft delete global filter, partial index on owner_name
- `backend/src/FinLedger.Infrastructure/Persistence/Configurations/TransactionConfiguration.cs` — check constraint amount>0, composite index (account_id, created_at)

**Frontend key files:**
- `frontend/src/lib/api.ts` — axios instance with error interceptor extracting title/detail
- `frontend/src/services/accounts.service.ts` / `transactions.service.ts` — thin HTTP wrappers
- `frontend/src/hooks/useAccounts.ts` / `useTransactions.ts` — React Query mutations with cache invalidation
- `frontend/src/pages/AccountDetail.tsx` — paginated statement with running balance display

**Infrastructure:**
- `docker-compose.yml` — 4 services: api (5000→8080), frontend (3000→80), db (5432), pgadmin (5050)
- `frontend/nginx.conf` — SPA fallback + `/api/` reverse-proxied to `finledger-api:8080`
- `backend/Dockerfile` — multi-stage SDK→runtime build
- `frontend/Dockerfile` — multi-stage Node build→nginx serve
- `database/seeds/001_seed_demo.sql` — 3 accounts + 6 transactions for demo-user

**Tests:**
- `backend/tests/FinLedger.Tests/Unit/AccountTests.cs` — pure domain tests (Credit/Debit/SoftDelete)
- `backend/tests/FinLedger.Tests/Integration/TransferIntegrationTests.cs` — Testcontainers PostgreSQL 16 real DB tests
