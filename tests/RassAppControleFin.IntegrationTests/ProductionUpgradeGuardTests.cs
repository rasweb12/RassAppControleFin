using System.Diagnostics;
using Xunit;

namespace RassAppControleFin.IntegrationTests;

public sealed class ProductionUpgradeGuardTests
{
    [Fact]
    public void Production_orchestrator_defaults_to_read_only_and_requires_two_explicit_write_controls()
    {
        var root = FindRepositoryRoot();
        var script = File.ReadAllText(Path.Combine(root, "scripts", "production", "Invoke-ProductionUpgrade.ps1"));

        Assert.Contains("br-lucky-paper-b6egkr8q", script, StringComparison.Ordinal);
        Assert.Contains("RASS_PRODUCTION_MIGRATIONS_CONNECTION", script, StringComparison.Ordinal);
        Assert.Contains("[switch]$Apply", script, StringComparison.Ordinal);
        Assert.Contains("ConfirmProductionWrite", script, StringComparison.Ordinal);
        Assert.Contains("APPLY-CHECKPOINT14-PRODUCTION", script, StringComparison.Ordinal);
        Assert.DoesNotContain("RASS_DB_TEST_RUNTIME", script, StringComparison.Ordinal);
        Assert.DoesNotContain("UserSecrets", script, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Production_tool_rejects_an_unapproved_branch_before_opening_a_connection()
    {
        var root = FindRepositoryRoot();
        var start = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = root,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        start.Environment["RASS_PRODUCTION_MIGRATIONS_CONNECTION"] =
            "Host=unapproved.example;Database=neondb;Username=neondb_owner;Password=not-a-secret;SSL Mode=VerifyFull";
        start.ArgumentList.Add("run");
        start.ArgumentList.Add("--project");
        start.ArgumentList.Add(Path.Combine(root, "tools", "RassAppControleFin.Database", "RassAppControleFin.Database.csproj"));
        start.ArgumentList.Add("-c");
        start.ArgumentList.Add("Release");
        start.ArgumentList.Add("--no-build");
        start.ArgumentList.Add("--");
        start.ArgumentList.Add("validate-production-upgrade-connection");
        start.ArgumentList.Add("--expected-host");
        start.ArgumentList.Add("unapproved.example");
        start.ArgumentList.Add("--confirmed-branch-id");
        start.ArgumentList.Add("br-not-production");

        using var process = Process.Start(start) ?? throw new InvalidOperationException("Database tool did not start.");
        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        Assert.Equal(2, process.ExitCode);
        Assert.DoesNotContain("not-a-secret", output + error, StringComparison.Ordinal);
        Assert.Contains("rejected", error, StringComparison.OrdinalIgnoreCase);
    }

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Directory.Build.props"))
                && File.Exists(Path.Combine(directory.FullName, "RassAppControleFin.slnx")))
                return directory.FullName;
        }
        throw new DirectoryNotFoundException("Repository root was not found.");
    }
}
