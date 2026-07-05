namespace FinLedger.Domain.Exceptions;

public sealed class InsufficientFundsException : Exception
{
    public Guid AccountId { get; }
    public decimal CurrentBalance { get; }
    public decimal RequiredAmount { get; }

    public InsufficientFundsException(Guid accountId, decimal currentBalance, decimal requiredAmount)
        : base($"Insufficient funds. Balance: {currentBalance:C2}, Required: {requiredAmount:C2}.")
    {
        AccountId = accountId;
        CurrentBalance = currentBalance;
        RequiredAmount = requiredAmount;
    }
}
