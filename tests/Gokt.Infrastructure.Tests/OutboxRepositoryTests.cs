using Gokt.Domain.Entities;
using Gokt.Infrastructure.Persistence;
using Gokt.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Gokt.Infrastructure.Tests;

public class OutboxRepositoryTests
{
    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public async Task CountFailedAsync_ShouldCountOnlyFailedEvents()
    {
        await using var db = CreateDbContext();
        var repo = new OutboxRepository(db);

        var failed1 = OutboxEvent.Create("ride.requested", "k1", "{}");
        failed1.MarkFailed("err");
        var failed2 = OutboxEvent.Create("ride.requested", "k2", "{}");
        failed2.MarkFailed("err");
        var pending = OutboxEvent.Create("ride.requested", "k3", "{}");

        await repo.AddAsync(failed1);
        await repo.AddAsync(failed2);
        await repo.AddAsync(pending);
        await db.SaveChangesAsync();

        var count = await repo.CountFailedAsync();

        Assert.Equal(2, count);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnMatchingEvent()
    {
        await using var db = CreateDbContext();
        var repo = new OutboxRepository(db);

        var evt = OutboxEvent.Create("ride.requested", "k1", "{}");
        await repo.AddAsync(evt);
        await db.SaveChangesAsync();

        var loaded = await repo.GetByIdAsync(evt.Id);

        Assert.NotNull(loaded);
        Assert.Equal(evt.Id, loaded!.Id);
    }

    [Fact]
    public async Task GetFailedAsync_ShouldReturnPagedFailedEventsOnly()
    {
        await using var db = CreateDbContext();
        var repo = new OutboxRepository(db);

        for (var i = 0; i < 5; i++)
        {
            var failed = OutboxEvent.Create("ride.requested", $"k{i}", "{}");
            failed.MarkFailed("err");
            await repo.AddAsync(failed);
        }

        await repo.AddAsync(OutboxEvent.Create("ride.requested", "pending", "{}"));
        await db.SaveChangesAsync();

        var page = await repo.GetFailedAsync(skip: 1, take: 2);

        Assert.Equal(2, page.Count);
        Assert.All(page, e => Assert.Equal(Gokt.Domain.Enums.OutboxStatus.Failed, e.Status));
    }

    [Fact]
    public async Task ReplayAllFailedAsync_ShouldResetFailedEventsToPending()
    {
        await using var db = CreateDbContext();
        var repo = new OutboxRepository(db);

        var failed = OutboxEvent.Create("ride.requested", "k1", "{}");
        failed.IncrementRetry("err");
        failed.MarkFailed("err");
        await repo.AddAsync(failed);

        await repo.AddAsync(OutboxEvent.Create("ride.requested", "pending", "{}"));
        await db.SaveChangesAsync();

        var replayed = await repo.ReplayAllFailedAsync();
        await db.SaveChangesAsync();

        var reloaded = await repo.GetByIdAsync(failed.Id);

        Assert.Equal(1, replayed);
        Assert.NotNull(reloaded);
        Assert.Equal(Gokt.Domain.Enums.OutboxStatus.Pending, reloaded!.Status);
        Assert.Equal(0, reloaded.RetryCount);
        Assert.Null(reloaded.LastError);
    }
}
