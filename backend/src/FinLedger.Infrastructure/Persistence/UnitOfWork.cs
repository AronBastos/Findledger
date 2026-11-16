namespace FinLedger.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using FinLedger.Application.Common.Interfaces;
using FinLedger.Domain.Exceptions;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly FinLedgerDbContext _context;
    private IDbContextTransaction? _currentTransaction;

    public UnitOfWork(FinLedgerDbContext context) => _context = context;

    // PATTERN: Transação explícita + Execution Strategy
    // Com EnableRetryOnFailure ativo, o EF Core proíbe BeginTransaction "solto": a transação
    // precisa estar dentro de strategy.ExecuteAsync, que repete o bloco inteiro numa falha
    // transitória. O ChangeTracker é limpo a cada tentativa para não carregar estado antigo.
    public Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken ct = default)
    {
        var strategy = _context.Database.CreateExecutionStrategy();

        return strategy.ExecuteAsync(async () =>
        {
            _context.ChangeTracker.Clear();

            await using var transaction = await _context.Database.BeginTransactionAsync(ct);
            _currentTransaction = transaction;
            try
            {
                // Sair por exceção (sem CommitAsync) → DisposeAsync da transação faz ROLLBACK.
                return await operation(ct);
            }
            finally
            {
                _currentTransaction = null;
            }
        });
    }

    public async Task<int> CommitAsync(CancellationToken ct = default)
    {
        try
        {
            var result = await _context.SaveChangesAsync(ct);

            if (_currentTransaction is not null)
                await _currentTransaction.CommitAsync(ct);

            return result;
        }
        catch (DbUpdateConcurrencyException)
        {
            // A camada de Infrastructure é a única que conhece EF Core.
            // Capturamos a exceção de EF aqui e relançamos como exceção de domínio,
            // mantendo a camada Application livre de dependências de infraestrutura.
            throw new ConcurrencyConflictException();
        }
    }
}
