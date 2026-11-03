using Xunit;

namespace RassAppControleFin.IntegrationTests;

public sealed class MvpHomologationScriptTests
{
    [Fact]
    public void Homologation_orchestrator_requires_an_explicit_isolated_destination_for_writing_tests()
    {
        var script = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "scripts", "homologacao", "Invoke-MvpHomologation.ps1"));
        Assert.Contains("[switch]$IncludeIntegration", script, StringComparison.Ordinal);
        Assert.Contains("NeonTestBranchConfirmed", script, StringComparison.Ordinal);
        Assert.Contains("rass_sprint1_tests", script, StringComparison.Ordinal);
        Assert.Contains("ep-square-rain-b6i4dhum", script, StringComparison.Ordinal);
        Assert.Contains("The command never executes migrations", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("database update", script, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Homologation_orchestrator_reports_missing_device_end_to_end_coverage_as_blocked()
    {
        var script = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "scripts", "homologacao", "Invoke-MvpHomologation.ps1"));
        Assert.Contains("Flutter integration_test", script, StringComparison.Ordinal);
        Assert.Contains("BLOQUEADO", script, StringComparison.Ordinal);
        Assert.Contains("Android AAB release", script, StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Directory.Build.props")) && File.Exists(Path.Combine(directory.FullName, "RassAppControleFin.slnx"))) return directory.FullName;
        }
        throw new DirectoryNotFoundException("Repository root was not found.");
    }
}
