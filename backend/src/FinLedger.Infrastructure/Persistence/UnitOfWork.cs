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

    public async Task<IAsyncDisposable> BeginTransactionAsync(CancellationToken ct = default)
    {
        _currentTransaction = await _context.Database.BeginTransactionAsync(ct);
        return _currentTransaction;
    }

    public async Task<int> CommitAsync(CancellationToken ct = default)
    {
        try
        {
            var result = await _context.SaveChangesAsync(ct);

            if (_currentTransaction is not null)
            {
                await _currentTransaction.CommitAsync(ct);
                _currentTransaction = null;
            }

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
