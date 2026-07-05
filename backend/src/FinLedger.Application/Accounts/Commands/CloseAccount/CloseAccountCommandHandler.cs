namespace FinLedger.Application.Accounts.Commands.CloseAccount;

using MediatR;
using FinLedger.Application.Common.Interfaces;
using FinLedger.Domain.Exceptions;
using FinLedger.Domain.Interfaces;

public sealed class CloseAccountCommandHandler : IRequestHandler<CloseAccountCommand>
{
    private readonly IAccountRepository _accounts;
    private readonly IUnitOfWork _uow;

    public CloseAccountCommandHandler(IAccountRepository accounts, IUnitOfWork uow)
    {
        _accounts = accounts;
        _uow = uow;
    }

    public async Task Handle(CloseAccountCommand request, CancellationToken ct)
    {
        var account = await _accounts.GetByIdAsync(request.AccountId, ct)
            ?? throw new AccountNotFoundException(request.AccountId);

        account.SoftDelete();
        await _uow.CommitAsync(ct);
    }
}
