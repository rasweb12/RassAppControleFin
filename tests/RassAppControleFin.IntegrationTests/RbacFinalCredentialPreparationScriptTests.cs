using System.Diagnostics;
using System.Text.Json;
using Xunit;

namespace RassAppControleFin.IntegrationTests;

public sealed class RbacFinalCredentialPreparationScriptTests : IDisposable
{
    private const string Host = "ep-raspy-poetry-b6x8rzs3.c-2.sa-east-1.aws.neon.tech";
    private readonly string appData = Path.Combine(Path.GetTempPath(), "rass-rbac-ps-tests-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task Valid_process_variables_are_prepared_without_reading_user_secrets()
    {
        var result = await InvokeAsync(ValidEnvironment());

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("prepared and policy-validated locally", result.Output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Missing_process_value_uses_the_matching_valid_user_secret_as_fallback()
    {
        WriteSecrets(new Dictionary<string, string>
        {
            ["RASS_DB_TEST_RUNTIME"] = Connection("rass_app_runtime_v2", "fallback-runtime")
        });
        var environment = ValidEnvironment();
        environment["RASS_DB_TEST_RUNTIME"] = string.Empty;

        var result = await InvokeAsync(environment);

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("prepared and policy-validated locally", result.Output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Invalid_user_secrets_report_the_missing_logical_configuration_only()
    {
        var path = SecretsPath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllTextAsync(path, "not-json");
        var environment = ValidEnvironment();
        environment["RASS_DB_TEST_RUNTIME"] = string.Empty;

        var result = await InvokeAsync(environment);

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("RASS_DB_TEST_RUNTIME could not be read", result.Error, StringComparison.Ordinal);
        Assert.DoesNotContain("Password=", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Valid_process_values_win_when_user_secrets_contain_conflicting_values()
    {
        WriteSecrets(new Dictionary<string, string>
        {
            ["RASS_DB_TEST_ADMIN"] = "Host=production.invalid;Database=neondb;Username=neondb_owner;Password=conflict;SSL Mode=Require",
            ["RASS_DB_TEST_RUNTIME"] = "Host=production.invalid;Database=neondb;Username=rass_app_runtime_v2;Password=conflict;SSL Mode=Require",
            ["RASS_DB_TEST_PLATFORM_ADMIN"] = "Host=production.invalid;Database=neondb;Username=rass_platform_admin_v1;Password=conflict;SSL Mode=Require"
        });

        var result = await InvokeAsync(ValidEnvironment());

        Assert.Equal(0, result.ExitCode);
        Assert.DoesNotContain("production.invalid", result.Output + result.Error, StringComparison.OrdinalIgnoreCase);
    }

    public void Dispose()
    {
        if (Directory.Exists(appData))
            Directory.Delete(appData, recursive: true);
    }

    private Dictionary<string, string> ValidEnvironment() => new(StringComparer.Ordinal)
    {
        ["RASS_DB_TEST_ADMIN"] = Connection("neondb_owner", "process-admin"),
        ["RASS_DB_TEST_RUNTIME"] = Connection("rass_app_runtime_v2", "process-runtime"),
        ["RASS_DB_TEST_PLATFORM_ADMIN"] = Connection("rass_platform_admin_v1", "process-platform")
    };

    private static string Connection(string role, string password)
        => $"Host={Host};Port=5432;Database=rass_sprint1_tests;Username={role};Password={password};SSL Mode=VerifyFull;No Reset On Close=false;Multiplexing=false";

    private void WriteSecrets(IReadOnlyDictionary<string, string> values)
    {
        var path = SecretsPath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(values));
    }

    private string SecretsPath()
        => Path.Combine(appData, "Microsoft", "UserSecrets", "RassAppControleFin-Database-Development", "secrets.json");

    private async Task<(int ExitCode, string Output, string Error)> InvokeAsync(IReadOnlyDictionary<string, string> values)
    {
        var root = FindRepositoryRoot();
        var start = new ProcessStartInfo("powershell.exe")
        {
            WorkingDirectory = root,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        start.ArgumentList.Add("-NoProfile");
        start.ArgumentList.Add("-ExecutionPolicy");
        start.ArgumentList.Add("Bypass");
        start.ArgumentList.Add("-File");
        start.ArgumentList.Add(Path.Combine(root, "scripts", "checkpoint13", "Invoke-RbacFinalValidation.ps1"));
        start.ArgumentList.Add("-ExpectedBranchId");
        start.ArgumentList.Add("br-odd-silence-b646inmx");
        start.ArgumentList.Add("-ExpectedHost");
        start.ArgumentList.Add(Host);
        start.ArgumentList.Add("-NeonBranchConfirmed");
        start.ArgumentList.Add("-UseIsolatedRbacTestEndpoint");
        start.ArgumentList.Add("-CredentialPreparationOnly");
        start.Environment["APPDATA"] = appData;
        foreach (var (name, value) in values)
            start.Environment[name] = value;

        using var process = Process.Start(start) ?? throw new InvalidOperationException("PowerShell did not start.");
        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        return (process.ExitCode, output, error);
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
