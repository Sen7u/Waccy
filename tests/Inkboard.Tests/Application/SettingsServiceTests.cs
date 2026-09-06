using Inkboard.Application.Services;
using Inkboard.Domain.Entities;
using Inkboard.Domain.Enums;
using Inkboard.Infrastructure.Abstractions.Settings;
using Inkboard.Infrastructure.Persistence.InMemory;
using Inkboard.Infrastructure.Persistence.Json;

namespace Inkboard.Tests.Application;

public class SettingsServiceTests
{
    [Theory]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    [InlineData(false, false, false)]
    [InlineData(false, true, true)]
    public void ShouldPaste_XorsDefaultWithInvert(bool pasteByDefault, bool invert, bool expected)
    {
        var settings = new AppSettings { PasteByDefault = pasteByDefault };
        Assert.Equal(expected, settings.ShouldPaste(invert));
    }

    [Fact]
    public async Task CaptureAsync_SkipsWhenPaused()
    {
        var store = new InMemoryHistoryStore();
        var settings = new InMemorySettingsStore();
        await settings.SaveAsync(new AppSettings { PauseCapture = true });
        var sut = new HistoryService(store, settings);

        await sut.CaptureAsync(new HistoryItem
        {
            Preview = "paused",
            Kind = ClipboardContentKind.Text,
        });

        Assert.Empty(await store.ListAsync());
    }

    [Fact]
    public async Task SettingsService_Update_PersistsAndRaisesChanged()
    {
        var path = Path.Combine(Path.GetTempPath(), "inkboard-test-" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            var store = new JsonSettingsStore(path);
            var sut = new SettingsService(store);
            var raised = 0;
            sut.Changed += (_, _) => raised++;

            await sut.UpdateAsync(s =>
            {
                s.PasteByDefault = false;
                s.HistoryLimit = 42;
            });

            var loaded = await sut.LoadAsync();
            Assert.False(loaded.PasteByDefault);
            Assert.Equal(42, loaded.HistoryLimit);
            Assert.Equal(1, raised);

            // 冷读文件确认落盘
            var fromDisk = await new JsonSettingsStore(path).LoadAsync();
            Assert.False(fromDisk.PasteByDefault);
            Assert.Equal(42, fromDisk.HistoryLimit);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}
