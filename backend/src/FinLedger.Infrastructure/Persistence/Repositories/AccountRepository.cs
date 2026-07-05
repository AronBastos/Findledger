namespace FinLedger.Infrastructure.Persistence.Repositories;

using Microsoft.EntityFrameworkCore;
using FinLedger.Domain.Entities;
using FinLedger.Domain.Interfaces;

public sealed class AccountRepository : IAccountRepository
{
    private readonly FinLedgerDbContext _context;

    public AccountRepository(FinLedgerDbContext context) => _context = context;

    public Task<Account?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _context.Accounts.FirstOrDefaultAsync(a => a.Id == id, ct);

    // PATTERN: SELECT FOR UPDATE — lock pessimista
    // Quando a query retorna, a linha no banco fica bloqueada até o fim da transação.
    // Nenhuma outra transação pode ler (com FOR UPDATE) ou modificar essa linha.
    // Usado no TransferCommandHandler para prevenir condições de corrida.
    // Nota: requer uma transação explícita ativa (BeginTransactionAsync).
    public Task<Account?> GetByIdForUpdateAsync(Guid id, CancellationToken ct = default)
        => _context.Accounts
            .FromSqlRaw("SELECT * FROM accounts WHERE id = {0} AND deleted_at IS NULL FOR UPDATE", id)
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<Account>> GetAllByOwnerAsync(
        string ownerName, CancellationToken ct = default)
    {
        // HasQueryFilter garante deleted_at IS NULL automaticamente
        return await _context.Accounts
            .Where(a => a.OwnerName == ownerName)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task AddAsync(Account account, CancellationToken ct = default)
        => await _context.Accounts.AddAsync(account, ct);
}
