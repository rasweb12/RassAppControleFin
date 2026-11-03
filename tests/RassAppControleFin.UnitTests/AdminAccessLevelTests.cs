using RassAppControleFin.Admin.Web.Services;
using Xunit;

namespace RassAppControleFin.UnitTests;

public sealed class AdminAccessLevelTests
{
    [Fact]
    public void User_has_no_admin_display_access()
        => Assert.Equal(AdminAccessLevel.None, AdminAccessPolicy.From(false, ["User"]));

    [Fact]
    public void Tenant_admin_has_tenant_admin_display_access()
        => Assert.Equal(AdminAccessLevel.TenantAdmin, AdminAccessPolicy.From(false, ["TenantAdmin"]));

    [Fact]
    public void Super_admin_has_global_display_access()
        => Assert.Equal(AdminAccessLevel.SuperAdmin, AdminAccessPolicy.From(true, ["User"]));

    [Fact]
    public void Platform_page_redirects_non_superadmins_before_rendering_global_controls()
    {
        var root = FindRepositoryRoot();
        var page = File.ReadAllText(Path.Combine(root, "src", "RassAppControleFin.Admin.Web", "Components", "Pages", "Platform.razor"));
        Assert.Contains("Navigation.NavigateTo(\"/\", replace: true)", page, StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "RassAppControleFin.slnx"))) return directory.FullName;
        }
        throw new DirectoryNotFoundException("Repository root was not found.");
    }
}
