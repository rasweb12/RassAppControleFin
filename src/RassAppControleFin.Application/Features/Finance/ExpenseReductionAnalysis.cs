namespace RassAppControleFin.Application.Features.Finance;

public static class ExpenseReductionAnalysis
{
    public static bool IsValidRange(DateOnly? from, DateOnly? to) =>
        from is null && to is null ||
        from is { } start && to is { } end && start.Year >= 1900 &&
        end < DateOnly.MaxValue && end.DayNumber - start.DayNumber is >= 0 and < 366;

    public static (DateTimeOffset PreviousStart, DateTimeOffset RecentStart, DateTimeOffset RecentEnd)
        Periods(ListExpenseReductionOpportunitiesQuery query, DateTimeOffset now)
    {
        if (query.From is { } from && query.To is { } to)
        {
            var start = AtUtcMidnight(from);
            var end = AtUtcMidnight(to.AddDays(1));
            return (start - (end - start), start, end);
        }

        var utc = now.UtcDateTime;
        var recentEnd = AtUtcMidnight(new DateOnly(utc.Year, utc.Month, 1)).AddMonths(1);
        var recentStart = recentEnd.AddMonths(-query.Months);
        return (recentStart.AddMonths(-query.Months), recentStart, recentEnd);
    }

    public static ExpenseReductionOpportunityView Compare(Guid categoryId, string category,
        string currency, decimal recent, decimal previous)
    {
        // A spending increase alone does not identify an adjustable expense.
        return new(categoryId, category, currency, recent, previous,
            Math.Max(0m, recent - previous), SuggestedReduction: 0m);
    }

    private static DateTimeOffset AtUtcMidnight(DateOnly day) =>
        new(day.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
}
