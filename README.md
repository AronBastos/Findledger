# FinLedger

> API de ledger financeiro demonstrando padrões de banco de dados usados em empresas reais.  
> **C# .NET 9** · **TypeScript React** · **PostgreSQL 16** · **Clean Architecture** · **CQRS**

A maioria dos projetos de portfólio demonstra CRUD básico. **FinLedger demonstra como dados financeiros são gerenciados com consistência, concorrência e auditabilidade** — os mesmos problemas que fintechs, bancos e ERPs resolvem diariamente.

---

## Padrões de banco de dados implementados

| # | Padrão | Arquivo | O que ensina |
|---|--------|---------|-------------|
| 1 | **Migrations** | `Infrastructure/Migrations/` | Schema como código, versionado e reproduzível |
| 2 | **Transação ACID** | `TransferCommandHandler.cs` | Atomicidade entre duas contas |
| 3 | **Concorrência otimista (xmin)** | `AccountConfiguration.cs` | Previne double-spend sem lock em leitura |
| 4 | **SELECT FOR UPDATE** | `AccountRepository.cs` | Lock pessimista para transferências |
| 5 | **Audit Log** | `AuditInterceptor.cs` | Rastreio automático de mudanças via interceptor |
| 6 | **Soft Delete** | `AccountConfiguration.cs` | Global query filter — DELETE lógico |
| 7 | **Índice parcial** | `AccountConfiguration.cs` | Índice apenas em contas ativas (menor e mais rápido) |
| 8 | **Índice composto** | `TransactionConfiguration.cs` | `(account_id, created_at)` para extrato eficiente |
| 9 | **Check Constraint** | `TransactionConfiguration.cs` | `amount > 0` — defesa em profundidade no banco |
| 10 | **CQRS (EF Core + Dapper)** | `StatementRepository.cs` | Escrita via ORM, leitura via SQL direto |
| 11 | **Window Function SQL** | `StatementRepository.cs` | `SUM() OVER (ORDER BY...)` para saldo corrente |
| 12 | **JSONB** | `AuditLogConfiguration.cs` | Snapshots JSON consultáveis no PostgreSQL |
| 13 | **Connection Pool** | `appsettings.json` + DI | Configuração do pool Npgsql |

Cada padrão tem um comentário explicativo no próprio arquivo apontando **por quê** ele existe.  
Para a versão aprofundada: [docs/database-patterns.md](docs/database-patterns.md)

Estudando lógica de programação, C# ou estrutura de dados? Veja o
[docs/guia-de-estudos.md](docs/guia-de-estudos.md) — mapeia cada conceito para o código exato
deste projeto, com exercícios práticos.

---

## Arquitetura

```
┌──────────────────────────────────────────────────────────┐
│                      Frontend (React)                    │
│  TypeScript · React Query · Zod · Tailwind CSS           │
└────────────────────────┬─────────────────────────────────┘
                         │ HTTP / REST
┌────────────────────────▼─────────────────────────────────┐
│                    FinLedger.API                          │
│  ASP.NET Core · Controllers · ExceptionMiddleware        │
├──────────────────────────────────────────────────────────┤
│                 FinLedger.Application                    │
│  MediatR · CQRS Commands/Queries · FluentValidation      │
├──────────────────────────────────────────────────────────┤
│                  FinLedger.Domain                        │
│  Entities · Enums · Exceptions · Repository Interfaces  │
│  (zero dependências externas — C# puro)                  │
├──────────────────────────────────────────────────────────┤
│               FinLedger.Infrastructure                   │
│  EF Core 9 · Dapper · Npgsql · AuditInterceptor         │
└────────────────────────┬─────────────────────────────────┘
                         │
                 ┌───────▼────────┐
                 │  PostgreSQL 16 │
                 │  + pgAdmin 4   │
                 └────────────────┘
```

**CQRS Read/Write split:**
```
Command (escrita)          Query (leitura)
─────────────────          ───────────────
MediatR IRequest    →      MediatR IRequest
EF Core (tracked)          Dapper (raw SQL)
Change tracking            Window functions
Audit interceptor          Sem overhead de ORM
```

---

## Stack

| Camada | Tecnologia |
|--------|-----------|
| Backend | C# .NET 9 · ASP.NET Core Web API |
| Banco | PostgreSQL 16 |
| ORM (escrita) | Entity Framework Core 9 + Npgsql |
| SQL (leitura) | Dapper |
| Mediator | MediatR |
| Validação | FluentValidation |
| Frontend | React 18 · TypeScript · Vite |
| Estado servidor | TanStack React Query |
| Forms | React Hook Form + Zod |
| Estilos | Tailwind CSS |
| Testes | xUnit · FluentAssertions · Testcontainers |
| Infra | Docker Compose · pgAdmin |

---

## Rodando localmente

### Opção 1 — Docker Compose (recomendado)

```bash
git clone https://github.com/seu-usuario/finledger
cd finledger
docker compose up -d --build
```

| Serviço | URL |
|---------|-----|
| API (Swagger) | http://localhost:5000/swagger |
| Frontend | http://localhost:3000 |
| pgAdmin | http://localhost:5050 |

As migrations rodam automaticamente no startup da API.

### Opção 2 — Desenvolvimento local

**Pré-requisitos:** .NET 9 SDK, Node 20+, PostgreSQL 16 (ou Docker)

```bash
# 1. Banco de dados
docker run -d --name finledger-db \
  -e POSTGRES_USER=finledger \
  -e POSTGRES_PASSWORD=finledger123 \
  -e POSTGRES_DB=finledger \
  -p 5432:5432 postgres:16-alpine

# 2. Backend
cd backend
dotnet restore
dotnet run --project src/FinLedger.API

# 3. Frontend (em outro terminal)
cd frontend
npm install
npm run dev
```

```bash
# Migrations (após mudanças no domínio)
cd backend
dotnet ef migrations add NomeDaMigracao --project src/FinLedger.Infrastructure --startup-project src/FinLedger.API

# Testes
dotnet test
```

---

## Endpoints

```
POST   /api/accounts                         Criar conta
GET    /api/accounts?ownerName=demo-user     Listar contas
DELETE /api/accounts/{id}                    Encerrar conta (soft delete)
GET    /api/accounts/{id}/statement          Extrato com saldo corrente (paginado)

POST   /api/transactions/deposit             Depósito
POST   /api/transactions/withdraw            Saque
POST   /api/transactions/transfer            Transferência ACID
```

---

## Estrutura do projeto

```
finledger/
├── backend/
│   ├── src/
│   │   ├── FinLedger.API/          ← Controllers, middleware, Program.cs
│   │   ├── FinLedger.Application/  ← MediatR handlers, validators, interfaces
│   │   ├── FinLedger.Domain/       ← Entities, enums, exceptions, repo interfaces
│   │   └── FinLedger.Infrastructure/ ← EF Core, Dapper, interceptors, repositories
│   └── tests/
│       └── FinLedger.Tests/        ← xUnit + Testcontainers (PostgreSQL real)
├── frontend/                       ← React + TypeScript + Tailwind
├── database/seeds/                 ← SQL de dados demo
├── docs/database-patterns.md       ← Guia aprofundado dos padrões
└── docker-compose.yml
```

---

## Decisões de design

**Por que saldo armazenado em vez de calculado?**  
Calcular `SUM(transactions)` a cada `/balance` vira full table scan conforme o histórico cresce. Armazenar o saldo na linha da conta dá leitura O(1). A consistência é garantida pelo xmin (concorrência otimista) e SELECT FOR UPDATE (lock pessimista no transfer).

**Por que EF Core para escrita e Dapper para leitura?**  
EF Core não consegue expressar window functions (`SUM() OVER (ORDER BY...)`) em LINQ — você usaria `FromSqlRaw` de qualquer jeito. Usar Dapper na leitura torna o split explícito, elimina overhead de change tracking desnecessário e deixa o SQL de janela legível e versionado no código.

**Por que xmin em vez de uma coluna RowVersion?**  
O PostgreSQL mantém `xmin` (transaction ID) automaticamente em cada linha — zero coluna extra, zero trigger. O Npgsql mapeia via `UseXminAsConcurrencyToken()`. Funciona igual, custa menos.

**Por que interceptor para audit log e não domain events?**  
Interceptor captura toda mudança automaticamente, sem precisar emitir evento por entidade. A lógica fica em C# (versionável, testável), e os logs entram na mesma transação de banco que a operação auditada — atomicidade garantida.

---

## Testes

```bash
dotnet test backend/
```

- **Unit:** Domínio puro — `Account.Debit`, `Account.Credit`, soft delete
- **Integration:** Testcontainers sobe PostgreSQL 16 real, roda migrations, testa:
  - Transferência ACID (saldos atualizados em ambas as contas)
  - Rollback em saldo insuficiente
  - Audit log gerado automaticamente
  - Check constraint `amount > 0` no banco

---

*Construído como projeto de portfólio para demonstrar padrões de banco de dados em nível pleno.*
