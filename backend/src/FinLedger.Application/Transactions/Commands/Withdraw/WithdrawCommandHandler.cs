namespace FinLedger.Application.Transactions.Commands.Withdraw;

using MediatR;
using FinLedger.Application.Common.Interfaces;
using FinLedger.Domain.Entities;
using FinLedger.Domain.Enums;
using FinLedger.Domain.Exceptions;
using FinLedger.Domain.Interfaces;

public sealed class WithdrawCommandHandler : IRequestHandler<WithdrawCommand, Guid>
{
    private readonly IAccountRepository _accounts;
    private readonly ITransactionRepository _transactions;
    private readonly IUnitOfWork _uow;

    public WithdrawCommandHandler(
        IAccountRepository accounts,
        ITransactionRepository transactions,
        IUnitOfWork uow)
    {
        _accounts = accounts;
        _transactions = transactions;
        _uow = uow;
    }

    public async Task<Guid> Handle(WithdrawCommand request, CancellationToken ct)
    {
        var account = await _accounts.GetByIdAsync(request.AccountId, ct)
            ?? throw new AccountNotFoundException(request.AccountId);

        // O método Debit lança InsufficientFundsException se saldo < amount.
        // Nenhum try/catch aqui — a exceção sobe para o ExceptionHandlingMiddleware.
        account.Debit(request.Amount);

        var tx = Transaction.Create(
            account.Id,
            TransactionType.Withdrawal,
            request.Amount,
            request.Description ?? "Withdrawal");

        await _transactions.AddAsync(tx, ct);
        await _uow.CommitAsync(ct);
        return tx.Id;
    }
}
