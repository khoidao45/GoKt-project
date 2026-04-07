using Gokt.Domain.Entities;
using Gokt.Domain.Enums;
using Xunit;

namespace Gokt.Domain.Tests;

public class OutboxEventTests
{
    [Fact]
    public void Create_ShouldInitializePendingEvent()
    {
        var evt = OutboxEvent.Create("ride.requested", "ride-1", "{\"x\":1}");

        Assert.Equal("ride.requested", evt.Type);
        Assert.Equal("ride-1", evt.MessageKey);
        Assert.Equal("{\"x\":1}", evt.Payload);
        Assert.Equal(OutboxStatus.Pending, evt.Status);
        Assert.Equal(0, evt.RetryCount);
        Assert.Null(evt.LastError);
        Assert.Null(evt.ProcessedAt);
    }

    [Fact]
    public void MarkProcessed_ShouldSetProcessedStatusAndTimestamp()
    {
        var evt = OutboxEvent.Create("ride.requested", "ride-1", "{}");

        evt.MarkProcessed();

        Assert.Equal(OutboxStatus.Processed, evt.Status);
        Assert.NotNull(evt.ProcessedAt);
        Assert.Null(evt.LastError);
    }

    [Fact]
    public void IncrementRetry_AndMarkFailed_ShouldTrackRetryAndFailure()
    {
        var evt = OutboxEvent.Create("ride.requested", "ride-1", "{}");

        evt.IncrementRetry("kafka timeout");
        evt.MarkFailed("kafka timeout");

        Assert.Equal(1, evt.RetryCount);
        Assert.Equal(OutboxStatus.Failed, evt.Status);
        Assert.Equal("kafka timeout", evt.LastError);
    }

    [Fact]
    public void ResetForReplay_ShouldSetPendingAndClearErrorState()
    {
        var evt = OutboxEvent.Create("ride.requested", "ride-1", "{}");
        evt.IncrementRetry("err");
        evt.MarkFailed("err");

        evt.ResetForReplay();

        Assert.Equal(OutboxStatus.Pending, evt.Status);
        Assert.Equal(0, evt.RetryCount);
        Assert.Null(evt.LastError);
        Assert.Null(evt.ProcessedAt);
    }
}
