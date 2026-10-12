namespace RassAppControleFin.Domain.Entities.Finance;

public enum FinancialFlow { Income, Expense }
public sealed class Category : FinancialAggregate
{
    private Category(Guid id, Guid tenantId, string userId, DateTimeOffset createdAt) : base(id, tenantId, userId, createdAt) { }
    public string Name { get; private set; } = "";
    public FinancialFlow Flow { get; private set; }
    public bool IsArchived { get; private set; }
    public static Category Create(Guid id, Guid tenantId, string userId, string name, FinancialFlow flow, DateTimeOffset now)
    {
        if (!Enum.IsDefined(flow)) throw new ArgumentException("Invalid flow.");
        return new(id, tenantId, userId, now) { Name = NameValue(name), Flow = flow };
    }
    public void Rename(string name) { Name = NameValue(name); Version = checked(Version + 1); }
    public void Archive() { IsArchived = true; Version = checked(Version + 1); }
}
