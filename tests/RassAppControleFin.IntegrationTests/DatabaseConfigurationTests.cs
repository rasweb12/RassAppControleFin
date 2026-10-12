using RassAppControleFin.Infrastructure.Configuration;
using RassAppControleFin.Infrastructure.Persistence;
using Xunit;

namespace RassAppControleFin.IntegrationTests;

public sealed class DatabaseConfigurationTests
{
    [Theory]
    [InlineData("Host=remote.example;Database=app;Username=rass_app_runtime;SSL Mode=VerifyFull", true)]
    [InlineData("Host=localhost;Database=app;Username=rass_app_runtime", true)]
    [InlineData("", true)]
    [InlineData("Host=remote.example;Database=app;Username=rass_app_runtime;SSL Mode=Require", false)]
    [InlineData("Host=localhost;Database=app;Username=owner", false)]
    [InlineData("Host=localhost;Database=app;Username=rass_app_runtime;No Reset On Close=true", false)]
    [InlineData("Host=localhost;Database=app;Username=rass_app_runtime;Multiplexing=true", false)]
    [InlineData("Host=localhost;Database=app;Username=rass_app_runtime;Include Error Detail=true", false)]
    [InlineData("Host=localhost;Database=app;Username=rass_app_runtime;Log Parameters=true", false)]
    [InlineData("NotAConnection=secret-sentinel", false)]
    [InlineData("Username=rass_app_runtime", false)]
    public void Unsafe_database_settings_are_rejected_without_exposing_values(string connection, bool expected)
        => Assert.Equal(expected, DatabaseConnectionSettings.IsValid(new DatabaseOptions { ConnectionString = connection }));

    [Fact]
    public void Runtime_pool_and_timeouts_are_bounded_and_reset_is_enabled()
    {
        var settings = DatabaseConnectionSettings.Build(new DatabaseOptions
        {
            ConnectionString = "Host=localhost;Database=app;Username=rass_app_runtime",
            MaxPoolSize = 5, ConnectionTimeoutSeconds = 7, CommandTimeoutSeconds = 12
        });
        Assert.Equal(5, settings.MaxPoolSize);
        Assert.Equal(7, settings.Timeout);
        Assert.Equal(12, settings.CommandTimeout);
        Assert.False(settings.IncludeErrorDetail);
        Assert.False(settings.LogParameters);
        Assert.False(settings.NoResetOnClose);
        Assert.False(settings.Multiplexing);
        Assert.False(settings.Enlist);
    }
}
