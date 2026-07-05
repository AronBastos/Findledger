# Padrões de banco de dados em profundidade

Este documento explica cada padrão de banco de dados implementado no FinLedger,
com o problema que resolve, como foi implementado e quando usar em projetos reais.

---

## 1. Migrations (Schema como código)

**Problema:** Como evoluir o schema do banco sem perder dados e de forma reproduzível em todos os ambientes?

**Solução:** Entity Framework Core Code-First Migrations.

```bash
# Criar uma migration após mudar uma entidade
dotnet ef migrations add NomeDaMigracao \
  --project src/FinLedger.Infrastructure \
  --startup-project src/FinLedger.API

# Aplicar ao banco
dotnet ef database update
```

O arquivo de migration gerado (`YYYYMMDDHHMMSS_NomeDaMigracao.cs`) contém os métodos `Up()` (aplicar) e `Down()` (reverter). Junto com o código, ele fica no controle de versão.

**Código relevante:** `Infrastructure/Persistence/Migrations/`

**Quando usar:** Sempre. Migrations tornam o schema versionável, rastreável e auditável — igual ao código da aplicação.

---

## 2. Transação ACID (o problema da transferência)

**Problema:** Uma transferência envolve dois UPDATEs e dois INSERTs. Se o sistema falhar no meio, o dinheiro some (débito feito, crédito não) ou aparece do nada (crédito feito, débito não).

```
❌ Sem transação:
   UPDATE accounts SET balance -= 100 WHERE id = source   ← ok
   [crash aqui!]
   UPDATE accounts SET balance += 100 WHERE id = dest     ← nunca executou

✅ Com transação:
   BEGIN
     UPDATE accounts SET balance -= 100 WHERE id = source
     UPDATE accounts SET balance += 100 WHERE id = dest
     INSERT INTO transactions ...  (débito)
     INSERT INTO transactions ...  (crédito)
     INSERT INTO audit_logs ...    (via interceptor)
   COMMIT  ← tudo ou nada
```

**Código relevante:** `Application/Transactions/Commands/Transfer/TransferCommandHandler.cs`

**Nota sobre atomicidade:** O `UnitOfWork.CommitAsync()` chama `SaveChangesAsync` + `transaction.CommitAsync()`. O `AuditInterceptor` adiciona os registros de audit_log antes do SaveChanges — eles entram na mesma transação.

---

## 3. Concorrência otimista via xmin do PostgreSQL

**Problema:** Dois requests leem a conta com saldo=R$100. Ambos tentam debitar R$80. Sem controle, ambos passam (saldo fica negativo).

```
Request A: SELECT balance = 100  →  balance - 80 = 20  →  UPDATE SET balance = 20
Request B: SELECT balance = 100  →  balance - 80 = 20  →  UPDATE SET balance = 20
                                                                       ↑ ERRADO!
```

**Solução:** O PostgreSQL mantém `xmin` (transaction ID) em cada linha. O EF Core/Npgsql inclui `xmin` no WHERE do UPDATE:

```sql
-- O que EF Core gera internamente:
UPDATE accounts
SET balance = 20
WHERE id = 'abc' AND xmin = 12345  ← se xmin mudou, 0 rows affected = conflito
```

Se 0 linhas foram afetadas → `DbUpdateConcurrencyException` → rollback → cliente retry.

**Código relevante:** `Infrastructure/Persistence/Configurations/AccountConfiguration.cs`

```csharp
builder.UseXminAsConcurrencyToken();
```

**Custo:** Zero. Nenhuma coluna extra, nenhum trigger. O PostgreSQL já mantém o xmin.

**Quando usar:** Alta concorrência com conflitos raros. Se conflitos são frequentes, use lock pessimista (Padrão 4).

---

## 4. SELECT FOR UPDATE (lock pessimista)

**Problema:** A concorrência otimista retorna erro e exige retry. Para transferências, prefere-se que uma transação espere a outra terminar em vez de falhar.

```sql
-- Bloqueia a linha até o COMMIT ou ROLLBACK desta transação
SELECT * FROM accounts WHERE id = 'abc' FOR UPDATE;
```

**Código relevante:** `Infrastructure/Persistence/Repositories/AccountRepository.cs`

```csharp
public Task<Account?> GetByIdForUpdateAsync(Guid id, CancellationToken ct = default)
    => _context.Accounts
        .FromSqlRaw("SELECT * FROM accounts WHERE id = {0} AND deleted_at IS NULL FOR UPDATE", id)
        .FirstOrDefaultAsync(ct);
```

**Por que SQL raw?** O EF Core não gera `FOR UPDATE` via LINQ. `FromSqlRaw` é o caminho correto.

**Prevenção de deadlock:** O TransferCommandHandler sempre adquire o lock na ordem do Guid menor. Isso previne que `Transfer(A→B)` e `Transfer(B→A)` concorrentes se bloqueiem mutuamente.

---

## 5. Audit Log via SaveChanges Interceptor

**Problema:** Como registrar toda mudança de estado (quem mudou o quê e quando) sem poluir cada handler com lógica de auditoria?

**Solução:** `SaveChangesInterceptor` — intercepta o EF Core ANTES de enviar o SQL ao banco. Adiciona registros de `audit_log` ao contexto. Esses registros entram na mesma transação de banco.

```csharp
public override ValueTask<InterceptionResult<int>> SavingChangesAsync(...)
{
    // Para cada entidade Added/Modified/Deleted no ChangeTracker:
    //   1. Captura old_values (OriginalValues) e new_values (CurrentValues) como JSON
    //   2. Adiciona AuditLog ao context
    // O EF Core salva tudo — entidades + audit_logs — na mesma transação
}
```

**Código relevante:** `Infrastructure/Persistence/Interceptors/AuditInterceptor.cs`

**Por que JSONB?** O PostgreSQL permite queries dentro do JSON:
```sql
SELECT * FROM audit_logs WHERE new_values->>'Balance' = '0.00';
SELECT * FROM audit_logs WHERE table_name = 'accounts' AND action = 'UPDATE';
```

---

## 6. Soft Delete com Global Query Filter

**Problema:** Registros financeiros nunca devem ser deletados fisicamente (compliance, auditoria). Mas toda query precisaria ter `WHERE deleted_at IS NULL`.

**Solução:** Global Query Filter no EF Core — aplicado automaticamente em TODA query que usa o DbContext.

```csharp
builder.HasQueryFilter(a => a.DeletedAt == null);
```

```sql
-- Toda query gerada pelo EF Core em Accounts automaticamente inclui:
WHERE deleted_at IS NULL
```

Para ignorar o filtro (admin, relatório de contas encerradas):
```csharp
_context.Accounts.IgnoreQueryFilters().Where(...);
```

**Código relevante:** `Infrastructure/Persistence/Configurations/AccountConfiguration.cs`

---

## 7. Índice parcial

**Problema:** Um índice em `owner_name` indexaria tanto contas ativas quanto encerradas, sendo a maioria ativa.

**Solução:** Índice parcial — indexa apenas as linhas que satisfazem a condição.

```csharp
builder.HasIndex(a => a.OwnerName)
    .HasFilter("deleted_at IS NULL")  // só contas ativas entram no índice
    .HasDatabaseName("ix_accounts_owner_active");
```

**SQL gerado na migration:**
```sql
CREATE INDEX ix_accounts_owner_active ON accounts (owner_name)
WHERE deleted_at IS NULL;
```

**Benefício:** Índice menor → cabe mais no cache do PostgreSQL → consultas mais rápidas. Menos overhead de manutenção (INSERTs não precisam atualizar o índice para linhas com `deleted_at NOT NULL`).

---

## 8. Índice composto para extrato

**Problema:** A query de extrato filtra por `account_id` e ordena por `created_at`:
```sql
WHERE account_id = ? ORDER BY created_at DESC
```
Sem índice composto, o banco percorre TODAS as transações para encontrar as de uma conta.

**Solução:** Índice composto na ordem certa:

```csharp
builder.HasIndex(t => new { t.AccountId, t.CreatedAt })
    .HasDatabaseName("ix_transactions_account_created");
```

```sql
CREATE INDEX ix_transactions_account_created ON transactions (account_id, created_at);
```

**Por que essa ordem?** O PostgreSQL pode usar este índice tanto para filtrar `account_id` quanto para a ordenação `created_at` — sem precisar de um sort separado. Ordem inversa (`created_at, account_id`) não seria útil para esta query.

---

## 9. Check Constraint

**Problema:** Mesmo com validação na aplicação, um bug poderia inserir `amount = 0` ou negativo no banco.

**Solução:** Constraint de banco — última linha de defesa:

```csharp
builder.ToTable("transactions", t =>
{
    t.HasCheckConstraint("ck_transactions_amount_positive", "amount > 0");
});
```

**SQL gerado:**
```sql
ALTER TABLE transactions
ADD CONSTRAINT ck_transactions_amount_positive CHECK (amount > 0);
```

**Defesa em profundidade:** A validação FluentValidation detecta antes do banco. O domínio (`Account.Debit`) verifica antes de criar a transação. A constraint é o último firewall — nunca dependa de apenas uma camada.

---

## 10. CQRS — EF Core para escrita, Dapper para leitura

**Padrão:** Command Query Responsibility Segregation — separar o modelo de escrita do modelo de leitura.

| Aspecto | Comando (Escrita) | Query (Leitura) |
|---------|-------------------|-----------------|
| Biblioteca | Entity Framework Core | Dapper |
| SQL | Gerado (LINQ → SQL) | Escrito à mão |
| Change tracking | Sim (necessário) | Não (overhead desnecessário) |
| Transações | Sim (ACID) | Geralmente não |
| Window functions | Impossível em LINQ | SQL nativo |

**Por que não usar apenas EF Core?**  
A query de extrato usa `SUM() OVER (ORDER BY...)` — window function. O EF Core não consegue expressar isso em LINQ, você precisaria de `FromSqlRaw` de qualquer jeito, perdendo o benefício do LINQ sem ganhar nada.

**Código relevante:** `Infrastructure/Persistence/Repositories/StatementRepository.cs`

---

## 11. Window Function — Saldo corrente acumulado

**Problema:** Exibir o saldo da conta após cada transação ("saldo corrente") sem múltiplos roundtrips ao banco.

**Solução:** `SUM() OVER (ORDER BY...)` — calcula o saldo corrente em um único pass sobre os dados.

```sql
SUM(
    CASE
        WHEN type IN ('Deposit', 'TransferCredit') THEN  amount
        ELSE                                            -amount
    END
) OVER (
    PARTITION BY account_id
    ORDER BY created_at ASC
    ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW
) AS running_balance
```

**Como funciona:**
- `PARTITION BY account_id` — reinicia o cálculo para cada conta
- `ORDER BY created_at ASC` — processa na ordem cronológica
- `ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW` — soma da primeira linha até a atual
- O banco usa o índice `ix_transactions_account_created` para esse processamento

**Código relevante:** `Infrastructure/Persistence/Repositories/StatementRepository.cs`

---

## 12. Connection Pool (Npgsql)

**Problema:** Abrir uma conexão com o PostgreSQL tem custo (TCP handshake, autenticação, alocação de recursos no servidor). Uma aplicação web que abre/fecha conexões por request vai ser lenta.

**Solução:** O Npgsql mantém um pool de conexões abertas e as reutiliza.

**Configuração no `appsettings.json`:**
```json
"Default": "...; Pooling=true; Minimum Pool Size=1; Maximum Pool Size=20; Connection Idle Lifetime=300"
```

| Parâmetro | Valor | O que faz |
|-----------|-------|-----------|
| `Pooling` | true | Habilita o pool (padrão) |
| `Minimum Pool Size` | 1 | Mantém ao menos 1 conexão aberta |
| `Maximum Pool Size` | 20 | Nunca abre mais de 20 simultaneamente |
| `Connection Idle Lifetime` | 300 (s) | Fecha conexões ociosas após 5 minutos |

**Padrão no DI:** `IDbConnection` é `Scoped` — cada request HTTP usa a mesma conexão para EF Core e Dapper, evitando dois slots de pool por request.

---

## Referências

- [PostgreSQL xmin](https://www.postgresql.org/docs/current/ddl-system-columns.html)
- [EF Core Interceptors](https://learn.microsoft.com/en-us/ef/core/logging-events-diagnostics/interceptors)
- [EF Core Global Query Filters](https://learn.microsoft.com/en-us/ef/core/querying/filters)
- [Npgsql Connection Pooling](https://www.npgsql.org/doc/connection-string-parameters.html)
- [PostgreSQL Window Functions](https://www.postgresql.org/docs/current/tutorial-window.html)
- [MediatR CQRS](https://github.com/jbogard/MediatR/wiki)
