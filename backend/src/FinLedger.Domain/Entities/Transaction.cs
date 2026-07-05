namespace FinLedger.Domain.Entities;

using FinLedger.Domain.Enums;

// PATTERN: Transactions são registros imutáveis — nunca sofrem UPDATE.
// Todo o histórico financeiro é construído por INSERT-only, igual a um ledger contábil.
// Isso torna auditoria trivial e elimina problemas de concorrência em leituras.
public sealed class Transaction
{
    public Guid Id { get; private set; }
    public Guid AccountId { get; private set; }
    public Guid? CounterpartyAccountId { get; private set; }
    public TransactionType Type { get; private set; }
    public decimal Amount { get; private set; }
    public string? Description { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public Account Account { get; private set; } = null!;

    private Transaction() { }

    public static Transaction Create(
        Guid accountId,
        TransactionType type,
        decimal amount,
        string? description = null,
        Guid? counterpartyAccountId = null) => new()
    {
        Id = Guid.NewGuid(),
        AccountId = accountId,
        CounterpartyAccountId = counterpartyAccountId,
        Type = type,
        Amount = amount,
        Description = description,
        CreatedAt = DateTime.UtcNow
    };
}
