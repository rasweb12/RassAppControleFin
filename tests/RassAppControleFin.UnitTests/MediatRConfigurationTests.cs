using Xunit;

namespace RassAppControleFin.UnitTests;

public sealed class MediatRConfigurationTests
{
    [Fact]
    public void Application_reads_a_license_only_from_runtime_configuration()
    {
        var source = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "src", "RassAppControleFin.Application", "DependencyInjection.cs"));

        Assert.Contains("Environment.GetEnvironmentVariable(\"MediatR__LicenseKey\")", source, StringComparison.Ordinal);
        Assert.Contains("if (!string.IsNullOrWhiteSpace(licenseKey))", source, StringComparison.Ordinal);
        Assert.DoesNotContain("LicenseKey = \"", source, StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Directory.Build.props")) &&
                File.Exists(Path.Combine(directory.FullName, "RassAppControleFin.slnx"))) return directory.FullName;
        }
        throw new DirectoryNotFoundException("Repository root was not found.");
    }
}
