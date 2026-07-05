namespace FinLedger.Domain.Interfaces;

using FinLedger.Domain.Entities;

// PATTERN: Repository interface no domínio
// A interface vive no Domain — a implementação fica na Infrastructure.
// Isso inverte a dependência: Infrastructure depende de Domain, nunca o contrário.
// Resultado: o domínio é puro C#, sem referência a EF Core, Dapper, ou PostgreSQL.
public interface IAccountRepository
{
    Task<Account?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Account?> GetByIdForUpdateAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Account>> GetAllByOwnerAsync(string ownerName, CancellationToken ct = default);
    Task AddAsync(Account account, CancellationToken ct = default);
}
