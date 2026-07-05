namespace FinLedger.Application.Transactions.Commands.Transfer;

using MediatR;
using FinLedger.Application.Common.Interfaces;
using FinLedger.Domain.Entities;
using FinLedger.Domain.Enums;
using FinLedger.Domain.Exceptions;
using FinLedger.Domain.Interfaces;

// ============================================================
// PATTERN: ACID Transfer — o handler mais importante do projeto
// ============================================================
//
// PROBLEMA: Sem transação de banco, uma falha entre o débito e o crédito
// deixa o sistema em estado inconsistente (dinheiro some ou aparece do nada).
//
// SOLUÇÃO: Tudo dentro de uma única transação de banco:
//   BEGIN
//     SELECT ... FOR UPDATE (lock nas duas contas)
//     UPDATE accounts SET balance -= X WHERE id = source
//     UPDATE accounts SET balance += X WHERE id = dest
//     INSERT INTO transactions ... (débito)
//     INSERT INTO transactions ... (crédito)
//     INSERT INTO audit_logs ... (via AuditInterceptor)
//   COMMIT  ← tudo ou nada
//
// PROBLEMA 2: Deadlock — Transfer(A→B) e Transfer(B→A) simultâneos
// podem travar um ao outro se cada um adquirir o lock do seu source primeiro.
//
// SOLUÇÃO: Adquirir locks sempre na ordem do Guid menor.
// Isso garante que as duas transferências disputam o mesmo lock primeiro,
// então uma espera a outra terminar em vez de travarem.
public sealed class TransferCommandHandler : IRequestHandler<TransferCommand, TransferResult>
{
    private readonly IAccountRepository _accounts;
    private readonly ITransactionRepository _transactions;
    private readonly IUnitOfWork _uow;

    public TransferCommandHandler(
        IAccountRepository accounts,
        ITransactionRepository transactions,
        IUnitOfWork uow)
    {
        _accounts = accounts;
        _transactions = transactions;
        _uow = uow;
    }

    public async Task<TransferResult> Handle(TransferCommand request, CancellationToken ct)
    {
        if (request.SourceAccountId == request.DestinationAccountId)
            throw new InvalidOperationException("Source and destination accounts must differ.");

        // STEP 1: Ordenação determinística — lock sempre no Guid menor primeiro.
        var (firstId, secondId) = request.SourceAccountId.CompareTo(request.DestinationAccountId) < 0
            ? (request.SourceAccountId, request.DestinationAccountId)
            : (request.DestinationAccountId, request.SourceAccountId);

        // STEP 2: Transação explícita de banco.
        // O 'await using' garante ROLLBACK automático se sairmos por exceção.
        await using var dbTransaction = await _uow.BeginTransactionAsync(ct);

        // STEP 3: SELECT FOR UPDATE — lock pessimista nas duas linhas.
        var first = await _accounts.GetByIdForUpdateAsync(firstId, ct)
            ?? throw new AccountNotFoundException(firstId);
        var second = await _accounts.GetByIdForUpdateAsync(secondId, ct)
            ?? throw new AccountNotFoundException(secondId);

        var (source, destination) = firstId == request.SourceAccountId
            ? (first, second)
            : (second, first);

        // STEP 4: Lógica de negócio no domínio.
        // Debit lança InsufficientFundsException se saldo < amount.
        source.Debit(request.Amount);
        destination.Credit(request.Amount);

        // STEP 5: Dois registros de transação vinculados pelo CounterpartyAccountId.
        var debitTx = Transaction.Create(
            source.Id, TransactionType.TransferDebit,
            request.Amount,
            request.Description ?? $"Transfer to {destination.Name}",
            counterpartyAccountId: destination.Id);

        var creditTx = Transaction.Create(
            destination.Id, TransactionType.TransferCredit,
            request.Amount,
            request.Description ?? $"Transfer from {source.Name}",
            counterpartyAccountId: source.Id);

        await _transactions.AddRangeAsync([debitTx, creditTx], ct);

        // STEP 6: CommitAsync envia todos os UPDATEs e INSERTs em batch.
        // O AuditInterceptor adiciona audit_logs na mesma transação.
        // ConcurrencyConflictException é relançada pelo UnitOfWork se xmin mudou.
        await _uow.CommitAsync(ct);

        return new TransferResult(debitTx.Id, creditTx.Id);
    }
}
