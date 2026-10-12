namespace RassAppControleFin.Security;

public interface ICurrentUser
{
    bool IsAuthenticated { get; }
    string? Subject { get; }
}

public interface IUserContext : ICurrentUser
{
    IReadOnlySet<string> Permissions { get; }
}

public static class Permissions
{
    public const string MonitoringRead = "Monitoring.Read";
    public const string AccountsRead = "Accounts.Read";
    public const string AccountsWrite = "Accounts.Write";
    public const string TransactionsRead = "Transactions.Read";
    public const string TransactionsWrite = "Transactions.Write";
    public const string CategoriesRead = "Categories.Read";
    public const string CategoriesWrite = "Categories.Write";
    public const string CreditCardsRead = "CreditCards.Read";
    public const string CreditCardsWrite = "CreditCards.Write";
    public const string TransfersWrite = "Transfers.Write";
    public const string MembershipsManage = "Memberships.Manage";
    public const string AuditRead = "Audit.Read";
}
