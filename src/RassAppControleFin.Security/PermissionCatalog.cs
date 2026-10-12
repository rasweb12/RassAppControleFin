namespace RassAppControleFin.Security;

public static class PermissionCatalog
{
    public static IReadOnlySet<string> ForRole(string role)
    {
        var permissions = new HashSet<string>(StringComparer.Ordinal);
        if (role is "User" or "TenantAdmin" or "FinancialAdmin")
            permissions.UnionWith([Permissions.AccountsRead, Permissions.AccountsWrite,
                Permissions.TransactionsRead, Permissions.TransactionsWrite, Permissions.TransfersWrite,
                Permissions.CategoriesRead, Permissions.CategoriesWrite,
                Permissions.CreditCardsRead, Permissions.CreditCardsWrite]);
        if (role is "TenantAdmin") permissions.Add(Permissions.MembershipsManage);
        if (role is "TenantAdmin" or "FinancialAdmin") permissions.Add(Permissions.AuditRead);
        return permissions;
    }
}
