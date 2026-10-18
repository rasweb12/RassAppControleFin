using RassAppControleFin.Domain.Entities.Finance;
using RassAppControleFin.Domain.Entities;
using RassAppControleFin.Domain.ValueObjects;
using Xunit;

namespace RassAppControleFin.UnitTests;

public sealed class FinanceDomainTests
{
    [Fact]
    public void Account_rejects_overdraft_and_preserves_balance()
    {
        var account = Account.Open(Guid.NewGuid(), Guid.NewGuid(), "user", "Conta", Money.Create(100m, "BRL", true), DateTimeOffset.UtcNow);
        Assert.False(account.TryApply(-100.01m));
        Assert.Equal(100m, account.Balance);
        Assert.Equal(1, account.Version);
    }

    [Fact]
    public void Credit_card_has_no_cardholder_data_and_enforces_limit()
    {
        var card = CreditCard.Create(Guid.NewGuid(), Guid.NewGuid(), "user", "Cartao", Money.Create(100m, "BRL"), 10, 20, DateTimeOffset.UtcNow);
        Assert.True(card.TryCharge(Money.Create(100m, "BRL")));
        Assert.False(card.TryCharge(Money.Create(0.01m, "BRL")));
        Assert.True(card.TryPay(Money.Create(100m, "BRL")));
        Assert.True(card.Archive());
    }

    [Fact]
    public void Budget_requires_a_valid_utc_period()
    {
        var now = DateTimeOffset.UtcNow;
        Assert.Throws<ArgumentException>(() => Budget.Create(Guid.NewGuid(), Guid.NewGuid(), "user", null,
            Money.Create(100m, "BRL"), now, now, now));
    }

    [Fact]
    public void Goal_completes_without_exceeding_its_target()
    {
        var goal = Goal.Create(Guid.NewGuid(), Guid.NewGuid(), "user", "Reserva",
            Money.Create(100m, "BRL"), null, DateTimeOffset.UtcNow);
        Assert.True(goal.AddProgress(Money.Create(100m, "BRL")));
        Assert.Equal("Completed", goal.Status);
        Assert.False(goal.AddProgress(Money.Create(0.01m, "BRL")));
    }

    [Fact]
    public void Expense_reduction_goal_requires_a_lower_target_and_valid_period()
    {
        var now = DateTimeOffset.UtcNow;
        var goal = ExpenseReductionGoal.Create(Guid.NewGuid(), Guid.NewGuid(), "user", null,
            Money.Create(900m, "BRL"), Money.Create(700m, "BRL"), now, now.AddMonths(1), now);
        Assert.Equal(200m, goal.ReferenceAmount - goal.TargetAmount);
        Assert.Throws<ArgumentException>(() => ExpenseReductionGoal.Create(Guid.NewGuid(), Guid.NewGuid(), "user", null,
            Money.Create(900m, "BRL"), Money.Create(900m, "BRL"), now, now.AddMonths(1), now));
    }

    [Fact]
    public void Profile_rename_trims_the_name_and_increments_version()
    {
        var profile = UserProfile.Create(Guid.NewGuid(), Guid.NewGuid(), "user", "Nome inicial", DateTimeOffset.UtcNow);
        profile.Rename("  Novo nome  ");
        Assert.Equal("Novo nome", profile.DisplayName);
        Assert.Equal(2, profile.Version);
    }

    [Fact]
    public void Financial_notification_can_be_marked_read_only_once()
    {
        var notification = FinancialNotification.Create(Guid.NewGuid(), Guid.NewGuid(), "user", "budget", "Orçamento", "Atenção", DateTimeOffset.UtcNow);
        Assert.True(notification.MarkRead(DateTimeOffset.UtcNow));
        Assert.False(notification.MarkRead(DateTimeOffset.UtcNow));
        Assert.Equal(2, notification.Version);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1.001)]
    public void Money_rejects_invalid_amounts(decimal amount)
        => Assert.False(Money.IsValid(amount, "BRL"));
}
