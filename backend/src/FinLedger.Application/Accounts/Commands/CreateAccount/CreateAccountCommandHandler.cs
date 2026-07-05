namespace FinLedger.Application.Accounts.Commands.CreateAccount;

using MediatR;
using FinLedger.Application.Common.Interfaces;
using FinLedger.Domain.Entities;
using FinLedger.Domain.Interfaces;

public sealed class CreateAccountCommandHandler : IRequestHandler<CreateAccountCommand, Guid>
{
    private readonly IAccountRepository _accounts;
    private readonly IUnitOfWork _uow;

    public CreateAccountCommandHandler(IAccountRepository accounts, IUnitOfWork uow)
    {
        _accounts = accounts;
        _uow = uow;
    }

    public async Task<Guid> Handle(CreateAccountCommand request, CancellationToken ct)
    {
        var account = Account.Create(request.OwnerName, request.Name, request.Type);
        await _accounts.AddAsync(account, ct);
        await _uow.CommitAsync(ct);
        return account.Id;
    }
}
