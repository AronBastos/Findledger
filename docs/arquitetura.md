# Arquitetura do FinLedger

Visão geral do sistema: como as camadas se encaixam, como os dados fluem e por que cada decisão foi tomada assim.
Para os padrões de banco em detalhe veja [`database-patterns.md`](database-patterns.md).
Para o roteiro de aprendizado em C# veja [`guia-de-estudos.md`](guia-de-estudos.md).

---

## O que o sistema faz

FinLedger é um ledger financeiro: permite criar contas (Corrente ou Poupança), fazer depósitos, saques e transferências entre contas, e consultar o extrato com saldo corrente acumulado por transação. Todos os dados são de um único usuário fixo (`demo-user`) — não há sistema de autenticação.

---

## Arquitetura geral

O backend segue **Clean Architecture** com quatro projetos em anel. A regra fundamental: as dependências só apontam para dentro — nunca uma camada interna conhece uma externa.

```
┌─────────────────────────────────────┐
│           FinLedger.API             │  HTTP, controllers, middleware
│  ┌──────────────────────────────┐   │
│  │    FinLedger.Application     │   │  Casos de uso, MediatR, validação
│  │  ┌───────────────────────┐   │   │
│  │  │   FinLedger.Domain    │   │   │  Entidades, exceções, interfaces
│  │  └───────────────────────┘   │   │
│  └──────────────────────────────┘   │
│  FinLedger.Infrastructure           │  EF Core, Dapper, migrations
└─────────────────────────────────────┘
```

O frontend é um SPA React separado, servido pelo Nginx, que reverse-proxeia `/api/` para o container da API.

---

## Stack tecnológica

| Camada | Tecnologia |
|---|---|
| Backend | C# .NET 9 / ASP.NET Core Web API |
| Mediator / CQRS | MediatR 12 |
| Validação | FluentValidation 11 |
| ORM (escrita) | EF Core 9 + Npgsql |
| SQL (leitura) | Dapper |
| Banco de dados | PostgreSQL 16 |
| Documentação API | Swashbuckle / Swagger |
| Frontend | React 18 + TypeScript + Vite |
| Estado de servidor | TanStack React Query v5 |
| Formulários | React Hook Form + Zod |
| Estilo | Tailwind CSS |
| Gráficos | Recharts |
| Testes unitários | xUnit + FluentAssertions |
| Testes de integração | xUnit + Testcontainers (PostgreSQL real em Docker) |
| Infraestrutura | Docker Compose |
| Servidor frontend | Nginx |

---

## Estrutura de diretórios

```
finledger/
├── docker-compose.yml              ← Orquestra api, frontend, db, pgadmin
├── .env.example                    ← Variável DB_PASSWORD
├── docs/
│   ├── arquitetura.md              ← este arquivo
│   ├── database-patterns.md        ← deep-dive nos 13 padrões de banco
│   └── guia-de-estudos.md          ← roteiro de aprendizado C# + estruturas de dados
├── database/
│   └── seeds/001_seed_demo.sql     ← 3 contas + 6 transações para "demo-user"
│
├── backend/
│   ├── Dockerfile                  ← Multi-stage: sdk:9.0 build → aspnet:9.0 runtime
│   └── src/
│       ├── FinLedger.Domain/
│       │   ├── Entities/           ← Account, Transaction, AuditLog
│       │   ├── Enums/              ← AccountType, TransactionType
│       │   ├── Exceptions/         ← InsufficientFundsException, AccountNotFoundException, ConcurrencyConflictException
│       │   └── Interfaces/         ← IAccountRepository, ITransactionRepository, IStatementRepository
│       ├── FinLedger.Application/
│       │   ├── Accounts/Commands/  ← CreateAccount, CloseAccount
│       │   ├── Accounts/Queries/   ← GetAllAccounts
│       │   ├── Transactions/Commands/ ← Deposit, Withdraw, Transfer
│       │   ├── Transactions/Queries/  ← GetStatement
│       │   └── Common/             ← IUnitOfWork, PagedResult, pipeline behaviors
│       ├── FinLedger.Infrastructure/
│       │   └── Persistence/
│       │       ├── Configurations/ ← AccountConfiguration, TransactionConfiguration
│       │       ├── Interceptors/   ← AuditInterceptor
│       │       ├── Migrations/     ← migrations geradas pelo EF Core
│       │       ├── Repositories/   ← AccountRepository, StatementRepository
│       │       └── UnitOfWork.cs
│       └── FinLedger.API/
│           ├── Controllers/        ← AccountsController, TransactionsController
│           ├── Middleware/         ← ExceptionHandlingMiddleware
│           └── Program.cs
│   └── tests/
│       └── FinLedger.Tests/
│           ├── Unit/               ← Testes de domínio puro (sem banco)
│           └── Integration/        ← Testes com Testcontainers (PostgreSQL real)
│
└── frontend/
    ├── Dockerfile                  ← Multi-stage: node:20 build → nginx:alpine serve
    ├── nginx.conf                  ← SPA fallback + reverse proxy /api/
    └── src/
        ├── types/                  ← Zod schemas + tipos TypeScript inferidos
        ├── lib/api.ts              ← Instância Axios com interceptor de erro
        ├── services/               ← Wrappers HTTP por recurso
        ├── hooks/                  ← Hooks React Query (useAccounts, useStatement, etc.)
        ├── components/             ← ui/ (primitivos) + accounts/ + transactions/ + layout/
        └── pages/                  ← Dashboard, Accounts, AccountDetail
```

---

## Modelo de dados

### `accounts`

| Coluna | Tipo | Detalhe |
|---|---|---|
| `id` | UUID | PK |
| `owner_name` | varchar(150) | Escopo do usuário — fixo em `demo-user` |
| `name` | varchar(100) | Nome da conta |
| `type` | varchar(20) | `Checking` ou `Savings` |
| `balance` | numeric(18,2) | Saldo armazenado — leitura O(1) |
| `xmin` | xid | Token de concorrência do PostgreSQL (sem coluna extra) |
| `created_at` | timestamp | |
| `deleted_at` | timestamp? | `NULL` = ativa; preenchido = encerrada (soft delete) |

Índice parcial `ix_accounts_owner_active` em `(owner_name) WHERE deleted_at IS NULL`.
Global Query Filter no EF Core garante que toda query LINQ exclui automaticamente contas encerradas.

### `transactions`

| Coluna | Tipo | Detalhe |
|---|---|---|
| `id` | UUID | PK |
| `account_id` | UUID | FK → accounts (Restrict) |
| `counterparty_account_id` | UUID? | Liga os dois lados de uma transferência |
| `type` | varchar(20) | `Deposit`, `Withdrawal`, `TransferDebit`, `TransferCredit` |
| `amount` | numeric(18,2) | Sempre positivo — `CHECK (amount > 0)` no banco |
| `description` | varchar(500)? | |
| `created_at` | timestamp | |

Transações são **insert-only** — nunca recebem UPDATE. Índice composto `ix_transactions_account_created` em `(account_id, created_at)`.

### `audit_logs`

| Coluna | Tipo | Detalhe |
|---|---|---|
| `id` | bigint | Auto-increment PK |
| `table_name` | varchar(100) | |
| `action` | varchar(10) | `INSERT`, `UPDATE` ou `DELETE` |
| `entity_id` | varchar(100) | PK da entidade alterada |
| `old_values` | jsonb? | Snapshot dos campos antes da operação |
| `new_values` | jsonb? | Snapshot dos campos depois da operação |
| `created_at` | timestamp | |

Escrito automaticamente pelo `AuditInterceptor` na mesma transação de banco — sem nenhum código nos handlers.

---

## Arquitetura do backend em detalhe

### Split CQRS: EF Core para escrita, Dapper para leitura

Toda escrita passa pelo EF Core: change tracking detecta o que mudou, o `AuditInterceptor` captura o diff e o `UnitOfWork` confirma tudo em uma transação atômica.

Toda leitura que exige expressividade SQL (window functions, CTEs, paginação em um roundtrip) vai direto ao banco via Dapper com SQL escrito à mão — sem overhead de change tracking, sem limitações do LINQ.

O `IDbConnection` é registrado como `Scoped`: EF Core e Dapper compartilham o mesmo slot de pool de conexões por request HTTP.

### Fluxo de um request (exemplo: depósito)

```
Browser
  → Axios POST /api/transactions/deposit
  → Nginx (reverse proxy /api/ → finledger-api:8080)
  → ExceptionHandlingMiddleware (try/catch global)
  → TransactionsController.Deposit()
  → IMediator.Send(DepositCommand)
    → FluentValidation (DepositCommandValidator) — roda antes do handler
    → DepositCommandHandler.Handle()
      → AccountRepository.GetByIdAsync()     ← SELECT via EF Core
      → account.Credit(amount)               ← regra de domínio
      → TransactionRepository.AddAsync()     ← rastreia nova Transaction
      → UnitOfWork.CommitAsync()
          → AuditInterceptor.SavingChangesAsync()  ← adiciona AuditLog ao contexto
          → SaveChangesAsync()                      ← UPDATE account + INSERT transaction + INSERT audit_log
          → transaction.CommitAsync()               ← COMMIT
  → 201 Created
```

---

## Fluxo de transferência (o caminho mais complexo)

A transferência é o caso mais rico do sistema: envolve dois locks, dois UPDATEs, dois INSERTs e N registros de auditoria, tudo em uma transação ACID.

```
TransferCommandHandler.Handle()
  1. Calcula qual dos dois GUIDs é menor → define a ordem de lock
  2. _uow.BeginTransactionAsync()          ← abre transação explícita no banco
  3. GetByIdForUpdateAsync(firstId)        ← SELECT ... FOR UPDATE (lock pessimista)
  4. GetByIdForUpdateAsync(secondId)       ← idem para a segunda conta
  5. source.Debit(amount)                  ← lança InsufficientFundsException se faltar saldo
  6. destination.Credit(amount)
  7. Cria dois Transaction (TransferDebit + TransferCredit ligados por CounterpartyAccountId)
  8. _uow.CommitAsync()
      → AuditInterceptor captura diff das 2 contas + 2 transações
      → SaveChangesAsync() — 2 UPDATEs + 2 INSERTs + N audit INSERTs
      → Se xmin mudou desde o SELECT → DbUpdateConcurrencyException → ConcurrencyConflictException → 409
      → CommitAsync() — COMMIT atômico
  9. Retorna TransferResult(debitTxId, creditTxId)
```

**Por que a ordem de lock pelo GUID menor?** Se `Transfer(A→B)` e `Transfer(B→A)` rodarem ao mesmo tempo, sem ordem definida, cada uma pode travar sua própria origem e esperar a outra — deadlock. Com a ordenação, ambas disputam o mesmo primeiro lock: uma espera, a outra termina. Ver também [`database-patterns.md` — Padrão 4](database-patterns.md).

---

## Arquitetura do frontend

Três páginas principais:

| Página | Rota | O que faz |
|---|---|---|
| Dashboard | `/` | Lista todas as contas com saldo total consolidado; botões para criar conta e transferir |
| Accounts | `/accounts` | Visão alternativa em lista |
| AccountDetail | `/accounts/:id` | Saldo da conta + extrato paginado com saldo corrente por linha |

**Camadas:**

```
pages/      ← componentes inteligentes: compõem hooks + componentes de feature
hooks/      ← wrappers React Query (useAccounts, useStatement, useDeposit, useTransfer…)
services/   ← funções que chamam api.ts e retornam dados tipados
lib/api.ts  ← instância Axios; base URL de VITE_API_BASE_URL ou '/api'
types/      ← schemas Zod + tipos TypeScript via z.infer<>
components/ ← ui/ (Button, Card, Badge, Spinner) + components de feature
```

**Padrões-chave:**
- O mesmo schema Zod valida o formulário (via `@hookform/resolvers/zod`) e define o tipo TypeScript — sem duplicação.
- Convenção de cache key: `['accounts', ownerName]` e `['statement', accountId, page]`. Mutations chamam `queryClient.invalidateQueries()` ao ter sucesso.
- O interceptor do Axios em `lib/api.ts` extrai `data.detail ?? data.title` dos erros — formato que casa com o `ExceptionHandlingMiddleware` do backend.
- `DEMO_OWNER = 'demo-user'` está hardcoded em `useAccounts.ts` — não há sistema de auth.

---

## Mapeamento de exceções → HTTP

O `ExceptionHandlingMiddleware` é o único lugar que converte exceções de domínio em respostas HTTP. As camadas de Domain e Application não conhecem HTTP.

| Exceção de domínio | Status HTTP | Cenário |
|---|---|---|
| `ValidationException` (FluentValidation) | 400 Bad Request | Campos inválidos na entrada |
| `AccountNotFoundException` | 404 Not Found | ID de conta inexistente |
| `InsufficientFundsException` | 422 Unprocessable Entity | Saldo insuficiente para débito/transferência |
| `ConcurrencyConflictException` | 409 Conflict | `xmin` mudou entre o SELECT e o UPDATE |

---

## Infraestrutura e deploy

```
docker-compose.yml
  ├── api       → backend/Dockerfile   → porta 5000:8080
  ├── frontend  → frontend/Dockerfile  → porta 3000:80
  ├── db        → postgres:16-alpine   → porta 5432, volume finledger-db-data
  └── pgadmin   → dpage/pgadmin4       → porta 5050 (admin@finledger.dev)
```

- A API só sobe depois que o `db` passa no healthcheck (`pg_isready`).
- Migrations rodam automaticamente via `db.Database.MigrateAsync()` no `Program.cs` ao iniciar.
- Pool de conexões Npgsql: mínimo 1, máximo 20, idle lifetime 300s.

```bash
# Subir tudo
docker compose up -d --build

# Acessos
# Swagger:  http://localhost:5000/swagger
# Frontend: http://localhost:3000
# pgAdmin:  http://localhost:5050
```

---

## Decisões de design em resumo

| Decisão | Alternativa descartada | Motivo |
|---|---|---|
| Saldo armazenado em coluna | Calcular com `SUM(transactions)` | O(1) leitura independente do histórico |
| `xmin` como token de concorrência | Coluna `row_version` | Zero custo — o PostgreSQL já mantém |
| `SaveChangesInterceptor` para auditoria | Domain events | Captura tudo automaticamente, uma única classe testável, mesma transação |
| Ordem de lock pelo GUID menor | Lock em qualquer ordem | Previne deadlock em transferências concorrentes |
| Global Query Filter para soft delete | `WHERE deleted_at IS NULL` em cada query | Impossível esquecer o filtro — aplicado pelo EF Core em toda query |
| Exception → HTTP só no middleware | Try/catch em cada controller | Domínio e Application ficam livres de dependências de HTTP |
| Dapper para leitura de extrato | EF Core + `FromSqlRaw` | SQL mais legível; window functions não são expressáveis em LINQ |
| Testcontainers (PostgreSQL real) | SQLite in-memory | Testa constraints, `xmin`, window functions — comportamentos que emuladores não replicam |

---

## Por onde começar a ler o código

| Ordem | Arquivo | Por quê |
|---|---|---|
| 1 | `Domain/Entities/Account.cs` | O agregado central: como o saldo é protegido, como o soft delete funciona, onde o `xmin` é declarado |
| 2 | `Application/Transactions/Commands/Transfer/TransferCommandHandler.cs` | O caso de uso mais complexo — cada passo comentado |
| 3 | `Infrastructure/Persistence/Interceptors/AuditInterceptor.cs` | Como o audit log é escrito de forma transparente |
| 4 | `Infrastructure/Persistence/Repositories/StatementRepository.cs` | Dapper + window function — a leitura CQRS |
| 5 | `API/Middleware/ExceptionHandlingMiddleware.cs` | Como exceções de domínio viram respostas HTTP |
