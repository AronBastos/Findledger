namespace FinLedger.Domain.Entities;

using FinLedger.Domain.Enums;
using FinLedger.Domain.Exceptions;

public sealed class Account
{
    public Guid Id { get; private set; }
    public string OwnerName { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public AccountType Type { get; private set; }
    public decimal Balance { get; private set; }

    // PATTERN: Optimistic Concurrency via PostgreSQL xmin
    // O xmin é uma coluna do sistema no PostgreSQL que armazena o ID da transação
    // que inseriu ou atualizou a linha. Ele muda automaticamente a cada UPDATE, sem
    // precisar de coluna extra. O EF Core/Npgsql inclui xmin no WHERE do UPDATE:
    //   UPDATE accounts SET balance=X WHERE id=Y AND xmin=Z
    // Se outra transação modificou a linha entre o SELECT e o UPDATE, xmin muda
    // e o UPDATE não encontra a linha → DbUpdateConcurrencyException.
    // Isso previne o problema de double-spend em transferências concorrentes.
    public uint RowVersion { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    private Account() { }

    public static Account Create(string ownerName, string name, AccountType type) => new()
    {
        Id = Guid.NewGuid(),
        OwnerName = ownerName,
        Name = name,
        Type = type,
        Balance = 0m,
        CreatedAt = DateTime.UtcNow
    };

    public void Credit(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Credit amount must be positive.", nameof(amount));
        Balance += amount;
    }

    public void Debit(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Debit amount must be positive.", nameof(amount));
        if (Balance < amount)
            throw new InsufficientFundsException(Id, Balance, amount);
        Balance -= amount;
    }

    public void SoftDelete()
    {
        if (DeletedAt.HasValue)
            throw new InvalidOperationException("Account is already closed.");
        DeletedAt = DateTime.UtcNow;
    }
}
