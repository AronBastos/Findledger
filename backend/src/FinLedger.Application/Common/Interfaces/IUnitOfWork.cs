namespace FinLedger.Application.Common.Interfaces;

// PATTERN: Unit of Work
// Agrupa múltiplas operações em uma única transação de banco.
// Usado principalmente no TransferCommandHandler para garantir
// que débito + crédito + audit log sejam persistidos atomicamente.
public interface IUnitOfWork
{
    Task<int> CommitAsync(CancellationToken ct = default);

    // Retorna IAsyncDisposable para que o chamador possa usar 'await using'.
    // Ao sair do bloco (sucesso ou exceção), DisposeAsync faz rollback
    // automaticamente se CommitAsync ainda não foi chamado.
    Task<IAsyncDisposable> BeginTransactionAsync(CancellationToken ct = default);
}
