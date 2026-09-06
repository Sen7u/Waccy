namespace Inkboard.Domain.Services;

using Inkboard.Domain.Entities;

/// <summary>
/// 历史搜索：子串优先，其次字符顺序模糊匹配（无第三方依赖）。
/// </summary>
public sealed class HistorySearch
{
    public IReadOnlyList<HistoryItem> Search(IEnumerable<HistoryItem> items, string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return items.ToList();

        var needle = query.Trim();
        return items
            .Select(item => (item, score: Score(item.Preview, needle)))
            .Where(x => x.score > 0)
            .OrderByDescending(x => x.score)
            .Select(x => x.item)
            .ToList();
    }

    /// <summary>子串命中优先；否则按模糊跨度打分。</summary>
    internal static int Score(string haystack, string needle)
    {
        var index = haystack.IndexOf(needle, StringComparison.OrdinalIgnoreCase);
        if (index >= 0)
            return 1_000_000 - index;

        return FuzzyScore(haystack, needle);
    }

    private static int FuzzyScore(string haystack, string needle)
    {
        if (needle.Length == 0 || needle.Length > haystack.Length)
            return 0;

        var hIndex = 0;
        var first = -1;
        var last = -1;

        foreach (var ch in needle)
        {
            var target = char.ToLowerInvariant(ch);
            var found = false;
            for (; hIndex < haystack.Length; hIndex++)
            {
                if (char.ToLowerInvariant(haystack[hIndex]) != target)
                    continue;

                if (first < 0)
                    first = hIndex;
                last = hIndex;
                hIndex++;
                found = true;
                break;
            }

            if (!found)
                return 0;
        }

        var span = last - first + 1;
        return Math.Max(1, 100_000 - span * 100 - first);
    }
}
