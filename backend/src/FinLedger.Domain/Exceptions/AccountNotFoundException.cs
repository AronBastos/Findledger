namespace FinLedger.Domain.Exceptions;

public sealed class AccountNotFoundException : Exception
{
    public AccountNotFoundException(Guid id)
        : base($"Account '{id}' was not found.") { }
}
