namespace FinLedger.Domain.Interfaces;

using FinLedger.Domain.Entities;

public interface ITransactionRepository
{
    Task AddAsync(Transaction transaction, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<Transaction> transactions, CancellationToken ct = default);
}
