namespace FinLedger.Tests.Unit;

using FluentAssertions;
using FinLedger.Domain.Entities;
using FinLedger.Domain.Enums;
using FinLedger.Domain.Exceptions;

public class AccountTests
{
    [Fact]
    public void Credit_PositiveAmount_IncreasesBalance()
    {
        var account = Account.Create("João", "Conta Corrente", AccountType.Checking);
        account.Credit(500m);
        account.Balance.Should().Be(500m);
    }

    [Fact]
    public void Credit_NegativeAmount_Throws()
    {
        var account = Account.Create("João", "Conta Corrente", AccountType.Checking);
        var act = () => account.Credit(-100m);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Debit_SufficientBalance_DecreasesBalance()
    {
        var account = Account.Create("João", "Conta Corrente", AccountType.Checking);
        account.Credit(1000m);
        account.Debit(300m);
        account.Balance.Should().Be(700m);
    }

    [Fact]
    public void Debit_InsufficientFunds_ThrowsInsufficientFundsException()
    {
        var account = Account.Create("João", "Conta Corrente", AccountType.Checking);
        account.Credit(100m);

        var act = () => account.Debit(200m);

        act.Should().Throw<InsufficientFundsException>()
            .Which.CurrentBalance.Should().Be(100m);
    }

    [Fact]
    public void Debit_ExactBalance_LeavesZeroBalance()
    {
        var account = Account.Create("João", "Conta Corrente", AccountType.Checking);
        account.Credit(100m);
        account.Debit(100m);
        account.Balance.Should().Be(0m);
    }

    [Fact]
    public void SoftDelete_SetsDeletedAt()
    {
        var account = Account.Create("João", "Conta Corrente", AccountType.Checking);
        account.SoftDelete();
        account.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public void SoftDelete_AlreadyDeleted_Throws()
    {
        var account = Account.Create("João", "Conta Corrente", AccountType.Checking);
        account.SoftDelete();
        var act = () => account.SoftDelete();
        act.Should().Throw<InvalidOperationException>();
    }
}
