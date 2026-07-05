namespace FinLedger.Application.Accounts.Queries.GetAllAccounts;

using MediatR;
using FinLedger.Domain.Interfaces;

public sealed class GetAllAccountsQueryHandler : IRequestHandler<GetAllAccountsQuery, IReadOnlyList<AccountSummaryDto>>
{
    private readonly IAccountRepository _accounts;

    public GetAllAccountsQueryHandler(IAccountRepository accounts) => _accounts = accounts;

    public async Task<IReadOnlyList<AccountSummaryDto>> Handle(
        GetAllAccountsQuery request, CancellationToken ct)
    {
        var accounts = await _accounts.GetAllByOwnerAsync(request.OwnerName, ct);
        return accounts
            .Select(a => new AccountSummaryDto(a.Id, a.Name, a.Type.ToString(), a.Balance, a.CreatedAt))
            .ToList();
    }
}
