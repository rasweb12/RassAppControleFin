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
}
