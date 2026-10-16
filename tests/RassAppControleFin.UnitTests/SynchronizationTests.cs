using RassAppControleFin.Application.Features.Synchronization;
using Xunit;

namespace RassAppControleFin.UnitTests;

public sealed class SynchronizationTests
{
    [Fact]
    public void Cursor_round_trips_timestamp_and_identifier()
    {
        var timestamp = DateTimeOffset.UtcNow;
        var id = Guid.NewGuid();

        var cursor = SyncCursor.Create(timestamp, id);

        Assert.True(SyncCursor.TryParse(cursor, out var parsedTimestamp, out var parsedId));
        Assert.Equal(timestamp.UtcTicks, parsedTimestamp!.Value.UtcTicks);
        Assert.Equal(id, parsedId);
    }

    [Theory]
    [InlineData("not-base64")]
    [InlineData("aW52YWxpZA==")]
    public void Cursor_rejects_malformed_values(string cursor)
    {
        Assert.False(SyncCursor.TryParse(cursor, out _, out _));
    }
}
