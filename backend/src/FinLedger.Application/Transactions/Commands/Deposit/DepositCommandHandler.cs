namespace FinLedger.Application.Transactions.Commands.Deposit;

using MediatR;
using FinLedger.Application.Common.Interfaces;
using FinLedger.Domain.Entities;
using FinLedger.Domain.Enums;
using FinLedger.Domain.Exceptions;
using FinLedger.Domain.Interfaces;

public sealed class DepositCommandHandler : IRequestHandler<DepositCommand, Guid>
{
    private readonly IAccountRepository _accounts;
    private readonly ITransactionRepository _transactions;
    private readonly IUnitOfWork _uow;

    public DepositCommandHandler(
        IAccountRepository accounts,
        ITransactionRepository transactions,
        IUnitOfWork uow)
    {
        _accounts = accounts;
        _transactions = transactions;
        _uow = uow;
    }

    public async Task<Guid> Handle(DepositCommand request, CancellationToken ct)
    {
        var account = await _accounts.GetByIdAsync(request.AccountId, ct)
            ?? throw new AccountNotFoundException(request.AccountId);

        account.Credit(request.Amount);

        var tx = Transaction.Create(
            account.Id,
            TransactionType.Deposit,
            request.Amount,
            request.Description ?? "Deposit");

        await _transactions.AddAsync(tx, ct);

        // O EF Core rastreia tanto 'account' (modificado) quanto 'tx' (novo)
        // e persiste ambos em um único roundtrip ao banco.
        await _uow.CommitAsync(ct);
        return tx.Id;
    }
}
