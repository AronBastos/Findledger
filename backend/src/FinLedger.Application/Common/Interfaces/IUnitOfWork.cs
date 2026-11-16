namespace FinLedger.Application.Common.Interfaces;

// PATTERN: Unit of Work
// Agrupa múltiplas operações em uma única transação de banco.
// Usado principalmente no TransferCommandHandler para garantir
// que débito + crédito + audit log sejam persistidos atomicamente.
public interface IUnitOfWork
{
    // Salva as mudanças pendentes e, se houver transação aberta por ExecuteInTransactionAsync, faz o COMMIT.
    Task<int> CommitAsync(CancellationToken ct = default);

    // Executa 'operation' dentro de uma transação de banco.
    // A transação inteira roda sob a execution strategy do EF Core: se ocorrer uma falha
    // transitória (rede, failover), TODA a operação é repetida do zero — por isso ela deve
    // reler seu estado do banco em vez de reaproveitar entidades de uma tentativa anterior.
    // Se a operação lançar exceção, a transação sofre ROLLBACK.
    Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken ct = default);
}
