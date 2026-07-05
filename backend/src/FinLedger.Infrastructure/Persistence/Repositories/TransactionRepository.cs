namespace FinLedger.Infrastructure.Persistence.Repositories;

using FinLedger.Domain.Entities;
using FinLedger.Domain.Interfaces;

public sealed class TransactionRepository : ITransactionRepository
{
    private readonly FinLedgerDbContext _context;

    public TransactionRepository(FinLedgerDbContext context) => _context = context;

    public async Task AddAsync(Transaction transaction, CancellationToken ct = default)
        => await _context.Transactions.AddAsync(transaction, ct);

    public async Task AddRangeAsync(IEnumerable<Transaction> transactions, CancellationToken ct = default)
        => await _context.Transactions.AddRangeAsync(transactions, ct);
}
