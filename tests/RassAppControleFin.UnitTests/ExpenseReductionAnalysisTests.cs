using RassAppControleFin.Application.Features.Finance;
using Xunit;

namespace RassAppControleFin.UnitTests;

public sealed class ExpenseReductionAnalysisTests
{
    [Theory]
    [InlineData(0, 572)]
    [InlineData(500, 72)]
    [InlineData(600, 0)]
    public void Spending_change_does_not_establish_savings(int previous, int increase)
    {
        var row = ExpenseReductionAnalysis.Compare(Guid.NewGuid(), "Categoria", "BRL", 572m, previous);
        Assert.Equal((decimal)increase, row.Increase);
        Assert.Equal(0m, row.SuggestedReduction);
    }

    [Fact]
    public void Custom_range_includes_october_31_and_compares_an_equal_number_of_days()
    {
        var from = new DateOnly(2026, 9, 1);
        var to = new DateOnly(2026, 10, 31);
        var query = new ListExpenseReductionOpportunitiesQuery(From: from, To: to);
        var (previous, start, end) = ExpenseReductionAnalysis.Periods(query, DateTimeOffset.UtcNow);
        Assert.Equal(new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero), start);
        Assert.Equal(new DateTimeOffset(2026, 11, 1, 0, 0, 0, TimeSpan.Zero), end);
        Assert.Equal(TimeSpan.FromDays(61), end - start);
        Assert.Equal(end - start, start - previous);
        var validator = new ListExpenseReductionOpportunitiesValidator();
        Assert.True(validator.Validate(query).IsValid);
        Assert.False(validator.Validate(query with { To = null }).IsValid);
        Assert.False(validator.Validate(query with { To = from.AddDays(-1) }).IsValid);
        Assert.False(validator.Validate(query with { To = from.AddDays(366) }).IsValid);
    }
}
