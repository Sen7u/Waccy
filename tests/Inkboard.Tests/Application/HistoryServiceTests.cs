using Inkboard.Application.Services;
using Inkboard.Domain.Entities;
using Inkboard.Domain.Enums;
using Inkboard.Infrastructure.Abstractions.Settings;
using Inkboard.Infrastructure.Persistence.InMemory;

namespace Inkboard.Tests.Application;

public class HistoryServiceTests
{
    [Fact]
    public async Task CaptureAsync_SkipsIgnoredApp()
    {
        var store = new InMemoryHistoryStore();
        var settings = new InMemorySettingsStore();
        await settings.SaveAsync(new AppSettings
        {
            IgnoredApps = ["secret-app"],
            HistoryLimit = 50,
        });

        var sut = new HistoryService(store, settings);
        await sut.CaptureAsync(new HistoryItem
        {
            Preview = "secret",
            SourceApp = "secret-app",
            Kind = ClipboardContentKind.Text,
        });

        Assert.Empty(await store.ListAsync());
    }

    [Fact]
    public async Task CaptureAsync_Duplicate_RefreshesTimestamp()
    {
        var store = new InMemoryHistoryStore();
        var settings = new InMemorySettingsStore();
        var sut = new HistoryService(store, settings);

        var first = new HistoryItem
        {
            Preview = "same",
            Kind = ClipboardContentKind.Text,
            CopiedAt = DateTimeOffset.UtcNow.AddMinutes(-1),
        };
        await sut.CaptureAsync(first);

        var second = new HistoryItem
        {
            Preview = "same",
            Kind = ClipboardContentKind.Text,
            CopiedAt = DateTimeOffset.UtcNow,
        };
        await sut.CaptureAsync(second);

        var items = await store.ListAsync();
        Assert.Single(items);
        Assert.Equal(first.Id, items[0].Id);
        Assert.Equal(second.CopiedAt, items[0].CopiedAt);
    }

    [Fact]
    public async Task CaptureAsync_TrimsToHistoryLimit()
    {
        var store = new InMemoryHistoryStore();
        var settings = new InMemorySettingsStore();
        await settings.SaveAsync(new AppSettings { HistoryLimit = 2 });
        var sut = new HistoryService(store, settings);

        for (var i = 0; i < 5; i++)
        {
            await sut.CaptureAsync(new HistoryItem
            {
                Preview = $"item-{i}",
                Kind = ClipboardContentKind.Text,
                CopiedAt = DateTimeOffset.UtcNow.AddSeconds(i),
            });
        }

        Assert.Equal(2, (await store.ListAsync()).Count);
    }
}
