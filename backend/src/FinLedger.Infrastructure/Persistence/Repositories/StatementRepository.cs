namespace FinLedger.Infrastructure.Persistence.Repositories;

using System.Data;
using Dapper;
using FinLedger.Application.Accounts.Queries.GetStatement;
using FinLedger.Application.Common.Interfaces;
using FinLedger.Application.Common.Models;

// PATTERN: CQRS — Read-side com Dapper
//
// Por que Dapper aqui e não EF Core?
// O extrato usa uma Window Function do PostgreSQL (SUM() OVER ORDER BY) para calcular
// o saldo corrente acumulado linha a linha. O EF Core não consegue expressar window
// functions em LINQ — você precisaria de FromSqlRaw de qualquer jeito.
// Dapper elimina o overhead do change tracking (desnecessário para leitura)
// e permite SQL legível e performático.
//
// A query usa duas CTEs:
// 1. 'ordered' — filtra as transações da conta e calcula o running_balance via window function
// 2. 'counted' — conta o total de registros para a paginação
// Isso evita dois roundtrips ao banco para obter dados + total count.
public sealed class StatementRepository : IStatementRepository
{
    private readonly IDbConnection _connection;

    public StatementRepository(IDbConnection connection) => _connection = connection;

    private const string StatementSql = """
        WITH ordered AS (
            SELECT
                id,
                type,
                amount,
                description,
                created_at,
                -- PATTERN: Window Function SUM() OVER (ORDER BY...)
                -- Calcula o saldo corrente acumulado para cada linha do extrato.
                -- O banco processa isso em um único pass sobre os dados, usando o
                -- índice ix_transactions_account_created para eficiência máxima.
                -- Deposits/Credits somam; Withdrawals/TransferDebits subtraem.
                SUM(
                    CASE
                        WHEN type IN ('Deposit', 'TransferCredit') THEN  amount
                        ELSE                                             -amount
                    END
                ) OVER (
                    PARTITION BY account_id
                    ORDER BY created_at ASC
                    ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW
                ) AS running_balance,
                COUNT(*) OVER (PARTITION BY account_id) AS total_count
            FROM transactions
            WHERE account_id = @AccountId
        )
        SELECT
            id              AS TransactionId,
            type            AS Type,
            amount          AS Amount,
            running_balance AS RunningBalance,
            description     AS Description,
            created_at      AS CreatedAt,
            total_count     AS TotalCount
        FROM ordered
        ORDER BY created_at DESC
        LIMIT @PageSize OFFSET @Offset;
        """;

    public async Task<PagedResult<StatementLineDto>> GetStatementAsync(
        Guid accountId,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var offset = (page - 1) * pageSize;
        var rows = await _connection.QueryAsync<StatementRow>(
            StatementSql,
            new { AccountId = accountId, PageSize = pageSize, Offset = offset });

        var list = rows.ToList();
        var totalCount = (int)(list.FirstOrDefault()?.TotalCount ?? 0);

        return new PagedResult<StatementLineDto>(
            list.Select(r => new StatementLineDto(
                r.TransactionId,
                r.Type,
                r.Amount,
                r.RunningBalance,
                r.Description,
                r.CreatedAt)).ToList(),
            totalCount,
            page,
            pageSize);
    }

    private record StatementRow(
        Guid TransactionId,
        string Type,
        decimal Amount,
        decimal RunningBalance,
        string? Description,
        DateTime CreatedAt,
        long TotalCount);
}
