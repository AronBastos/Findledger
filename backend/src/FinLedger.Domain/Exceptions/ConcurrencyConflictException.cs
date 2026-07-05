namespace FinLedger.Domain.Exceptions;

public sealed class ConcurrencyConflictException : Exception
{
    public ConcurrencyConflictException()
        : base("A concurrent modification was detected. Please retry the operation.") { }
}
