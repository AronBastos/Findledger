namespace FinLedger.Application.Common.Interfaces;

using FinLedger.Application.Accounts.Queries.GetStatement;
using FinLedger.Application.Common.Models;

// PATTERN: CQRS - Read-side repository
// Separada do IAccountRepository (write-side) intencionalmente.
// Implementada com Dapper na Infrastructure para permitir SQL com window functions
// (SUM OVER ORDER BY) que o EF Core não consegue expressar em LINQ.
public interface IStatementRepository
{
    Task<PagedResult<StatementLineDto>> GetStatementAsync(
        Guid accountId,
        int page,
        int pageSize,
        CancellationToken ct = default);
}
