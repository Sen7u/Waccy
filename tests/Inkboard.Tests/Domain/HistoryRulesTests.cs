using Inkboard.Domain.Entities;
using Inkboard.Domain.Enums;
using Inkboard.Domain.Rules;

namespace Inkboard.Tests.Domain;

public class HistoryRulesTests
{
    [Fact]
    public void IsDuplicateOf_SamePreviewAndKind_ReturnsTrue()
    {
        Assert.True(HistoryRules.IsDuplicateOf(Text("hello"), Text("hello")));
        Assert.False(HistoryRules.IsDuplicateOf(Text("hello"), Text("other")));
    }

    [Fact]
    public void SortForDisplay_PinnedFirstThenRecency()
    {
        var pinned = Text("pin", DateTimeOffset.UtcNow.AddMinutes(-5), pinKey: "a");
        var newer = Text("new", DateTimeOffset.UtcNow);

        var sorted = HistoryRules.SortForDisplay([newer, pinned]);

        Assert.Equal(pinned.Id, sorted[0].Id);
        Assert.Equal(newer.Id, sorted[1].Id);
    }

    [Fact]
    public void TrimToLimit_RespectsMaxCount()
    {
        var items = Enumerable.Range(0, 5)
            .Select(i => Text($"t{i}", DateTimeOffset.UtcNow.AddMinutes(-i)))
            .ToList();

        var trimmed = HistoryRules.TrimToLimit(items, maxCount: 3);

        Assert.Equal(3, trimmed.Count);
        Assert.Equal("t0", trimmed[0].Preview);
    }

    private static HistoryItem Text(string preview, DateTimeOffset? at = null, string? pinKey = null) => new()
    {
        Preview = preview,
        Kind = ClipboardContentKind.Text,
        CopiedAt = at ?? DateTimeOffset.UtcNow,
        PinKey = pinKey,
        Payload = System.Text.Encoding.UTF8.GetBytes(preview),
    };
}
