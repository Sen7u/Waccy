using Inkboard.Domain.Entities;
using Inkboard.Domain.Enums;
using Inkboard.Domain.Services;

namespace Inkboard.Tests.Domain;

public class HistorySearchTests
{
    [Fact]
    public void Search_MatchesSubstring_CaseInsensitive()
    {
        var items = new[]
        {
            new HistoryItem { Preview = "Hello World", Kind = ClipboardContentKind.Text },
            new HistoryItem { Preview = "Other", Kind = ClipboardContentKind.Text },
        };

        var hits = new HistorySearch().Search(items, "hello");

        Assert.Single(hits);
        Assert.Equal("Hello World", hits[0].Preview);
    }

    [Fact]
    public void Search_BlankQuery_ReturnsAll()
    {
        var items = new[]
        {
            new HistoryItem { Preview = "A", Kind = ClipboardContentKind.Text },
            new HistoryItem { Preview = "B", Kind = ClipboardContentKind.Text },
        };

        Assert.Equal(2, new HistorySearch().Search(items, " ").Count);
    }

    [Fact]
    public void Search_FuzzySubsequence_MatchesOutOfOrderGaps()
    {
        var items = new[]
        {
            new HistoryItem { Preview = "Clipboard Manager", Kind = ClipboardContentKind.Text },
            new HistoryItem { Preview = "Unrelated", Kind = ClipboardContentKind.Text },
        };

        var hits = new HistorySearch().Search(items, "clmgr");

        Assert.Single(hits);
        Assert.Equal("Clipboard Manager", hits[0].Preview);
    }
}
