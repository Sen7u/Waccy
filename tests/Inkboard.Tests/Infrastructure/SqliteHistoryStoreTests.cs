using Inkboard.Domain.Entities;
using Inkboard.Domain.Enums;
using Inkboard.Infrastructure.Persistence.Sqlite;

namespace Inkboard.Tests.Infrastructure;

public class SqliteHistoryStoreTests
{
    [Fact]
    public async Task Upsert_List_Delete_RoundTrip()
    {
        var path = Path.Combine(Path.GetTempPath(), $"inkboard-hist-{Guid.NewGuid():N}.db");
        try
        {
            await using var store = new SqliteHistoryStore(path);
            var item = new HistoryItem
            {
                Preview = "hello sqlite",
                Kind = ClipboardContentKind.Text,
                Payload = "hello sqlite"u8.ToArray(),
                SourceApp = "test",
            };

            await store.UpsertAsync(item);
            var listed = await store.ListAsync();
            Assert.Single(listed);
            Assert.Equal(item.Id, listed[0].Id);
            Assert.Equal("hello sqlite", listed[0].Preview);
            Assert.Equal("test", listed[0].SourceApp);

            await store.DeleteAsync(item.Id);
            Assert.Empty(await store.ListAsync());
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public async Task ClearUnpinned_KeepsPinned()
    {
        var path = Path.Combine(Path.GetTempPath(), $"inkboard-hist-{Guid.NewGuid():N}.db");
        try
        {
            await using var store = new SqliteHistoryStore(path);
            var pinned = new HistoryItem { Preview = "pin", PinKey = "ab12", Payload = "pin"u8.ToArray() };
            var loose = new HistoryItem { Preview = "loose", Payload = "loose"u8.ToArray() };
            await store.UpsertAsync(pinned);
            await store.UpsertAsync(loose);

            await store.ClearUnpinnedAsync();
            var left = await store.ListAsync();
            Assert.Single(left);
            Assert.Equal(pinned.Id, left[0].Id);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public async Task Upsert_PersistsImagePayload()
    {
        var path = Path.Combine(Path.GetTempPath(), $"inkboard-hist-{Guid.NewGuid():N}.db");
        try
        {
            await using var store = new SqliteHistoryStore(path);
            var png = new byte[] { 0x89, 0x50, 0x4E, 0x47, 1, 2, 3, 4 };
            var item = new HistoryItem
            {
                Preview = "图片 1×1",
                Kind = ClipboardContentKind.Image,
                Payload = png,
            };
            await store.UpsertAsync(item);

            var listed = await store.ListAsync();
            Assert.Equal(ClipboardContentKind.Image, listed[0].Kind);
            Assert.Equal(png, listed[0].Payload);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}
