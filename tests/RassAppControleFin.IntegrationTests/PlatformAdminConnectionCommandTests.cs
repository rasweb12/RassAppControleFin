using System.Diagnostics;
using Xunit;

namespace RassAppControleFin.IntegrationTests;

public sealed class PlatformAdminConnectionCommandTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("Host=ep-raspy-poetry-b6x8rzs3.c-2.sa-east-1.aws.neon.tech;Database=rass_sprint1_tests;Username=rass_platform_admin_v1;Password=   ;SSL Mode=VerifyFull")]
    [InlineData("Host=ep-raspy-poetry-b6x8rzs3.c-2.sa-east-1.aws.neon.tech;Database=rass_sprint1_tests;Username=rass_app_runtime_v2;Password=nonempty;SSL Mode=VerifyFull")]
    [InlineData("Host=unapproved.example;Database=neondb;Username=rass_platform_admin_v1;Password=nonempty;SSL Mode=Require")]
    public async Task Validate_platform_admin_connection_rejects_invalid_inputs_without_connecting(string? connection)
    {
        var root = FindRepositoryRoot();
        var start = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = root,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        start.Environment["RASS_DB_TEST_PLATFORM_ADMIN"] = connection ?? string.Empty;
        start.ArgumentList.Add("run");
        start.ArgumentList.Add("--project");
        start.ArgumentList.Add(Path.Combine(root, "tools", "RassAppControleFin.Database", "RassAppControleFin.Database.csproj"));
        start.ArgumentList.Add("-c");
        start.ArgumentList.Add("Release");
        start.ArgumentList.Add("--no-build");
        start.ArgumentList.Add("--");
        start.ArgumentList.Add("validate-platform-admin-connection");

        using var process = Process.Start(start) ?? throw new InvalidOperationException("Database tool did not start.");
        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        Assert.Equal(2, process.ExitCode);
        Assert.DoesNotContain("PASS", output, StringComparison.Ordinal);
        Assert.Contains("FAIL:", error, StringComparison.Ordinal);
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
