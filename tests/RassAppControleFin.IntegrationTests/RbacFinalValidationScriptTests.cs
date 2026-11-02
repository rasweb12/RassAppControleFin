using Xunit;
using System.Diagnostics;

namespace RassAppControleFin.IntegrationTests;

public sealed class RbacFinalValidationScriptTests
{
    [Fact]
    public void Final_validation_script_is_allowlisted_and_does_not_run_migrations()
    {
        var root = FindRepositoryRoot();
        var script = File.ReadAllText(Path.Combine(root, "scripts", "checkpoint13", "Invoke-RbacFinalValidation.ps1"));

        Assert.Contains("br-odd-silence-b646inmx", script, StringComparison.Ordinal);
        Assert.Contains("ep-raspy-poetry-b6x8rzs3.c-2.sa-east-1.aws.neon.tech", script, StringComparison.Ordinal);
        Assert.Contains("--confirm-isolated-rbac-tests", script, StringComparison.Ordinal);
        Assert.Contains("UseIsolatedRbacTestEndpoint", script, StringComparison.Ordinal);
        Assert.Contains("Set-IsolatedRbacTestProcessConnections", script, StringComparison.Ordinal);
        Assert.Contains("rass_sprint1_tests", script, StringComparison.Ordinal);
        Assert.Contains("Get-UserSecretsValue", script, StringComparison.Ordinal);
        Assert.Contains("ConvertTo-ApprovedConnection", script, StringComparison.Ordinal);
        Assert.Contains("CredentialPreparationOnly", script, StringComparison.Ordinal);
        Assert.Contains("local User Secrets fallback", script, StringComparison.Ordinal);
        Assert.Contains("Build-IntegrationTests", script, StringComparison.Ordinal);
        Assert.Contains("System.EnvironmentVariableTarget]::Process", script, StringComparison.Ordinal);
        Assert.Contains("finally", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("bootstrap-rbac-test-schema", script, StringComparison.Ordinal);
        Assert.DoesNotContain("migrate-rbac-test-database", script, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Current_database_tool_accepts_the_script_pre_validation_phase_without_a_database_connection()
    {
        var root = FindRepositoryRoot();
        var start = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = root,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        start.ArgumentList.Add("run");
        start.ArgumentList.Add("--project");
        start.ArgumentList.Add(Path.Combine(root, "tools", "RassAppControleFin.Database", "RassAppControleFin.Database.csproj"));
        start.ArgumentList.Add("-c");
        start.ArgumentList.Add("Release");
        start.ArgumentList.Add("--no-build");
        start.ArgumentList.Add("--");
        start.ArgumentList.Add("validate-rbac-test-sql-phase");
        start.ArgumentList.Add("--phase");
        start.ArgumentList.Add("pre-validation");

        using var process = Process.Start(start) ?? throw new InvalidOperationException("Database tool did not start.");
        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        Assert.True(process.ExitCode == 0, error);
        Assert.Contains("RBAC SQL phase 'pre-validation' is approved.", output, StringComparison.Ordinal);
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
