namespace RassAppControleFin.Domain.Entities.Finance;

public sealed class FinancialNotification : FinancialAggregate
{
    private FinancialNotification(Guid id, Guid tenantId, string userId, DateTimeOffset createdAt)
        : base(id, tenantId, userId, createdAt) { }

    public string Type { get; private set; } = "";
    public string Title { get; private set; } = "";
    public string Message { get; private set; } = "";
    public DateTimeOffset? ReadAt { get; private set; }

    public static FinancialNotification Create(Guid id, Guid tenantId, string userId, string type, string title,
        string message, DateTimeOffset createdAt)
    {
        if (createdAt.Offset != TimeSpan.Zero) throw new ArgumentException("UTC required.", nameof(createdAt));
        var item = new FinancialNotification(id, tenantId, userId, createdAt)
        {
            Type = Text(type, 64),
            Title = Text(title, 160),
            Message = Text(message, 1000)
        };
        return item;
    }

    public bool MarkRead(DateTimeOffset readAt)
    {
        if (readAt.Offset != TimeSpan.Zero) throw new ArgumentException("UTC required.", nameof(readAt));
        if (ReadAt is not null) return false;
        ReadAt = readAt;
        Version = checked(Version + 1);
        return true;
    }

    private static string Text(string value, int maximum)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var text = value.Trim();
        if (text.Length > maximum) throw new ArgumentOutOfRangeException(nameof(value));
        return text;
    }
}
