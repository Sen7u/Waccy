namespace Inkboard.Domain.Rules;

using Inkboard.Domain.Entities;

/// <summary>
/// 历史列表的纯领域规则：去重、上限裁剪、置顶排序。
/// 不含 IO，便于单测。
/// </summary>
public static class HistoryRules
{
    /// <summary>
    /// 若新条目与最近一条预览相同且类型相同，视为重复（应刷新时间而非再插一条）。
    /// </summary>
    public static bool IsDuplicateOf(HistoryItem newer, HistoryItem? existing)
    {
        if (existing is null)
            return false;

        return newer.Kind == existing.Kind
               && string.Equals(newer.Preview, existing.Preview, StringComparison.Ordinal);
    }

    /// <summary>
    /// 按「置顶优先，再按复制时间倒序」排序。
    /// </summary>
    public static IReadOnlyList<HistoryItem> SortForDisplay(IEnumerable<HistoryItem> items)
    {
        return items
            .OrderByDescending(i => i.IsPinned)
            .ThenByDescending(i => i.CopiedAt)
            .ToList();
    }

    /// <summary>
    /// 保留置顶项 + 最近的非置顶项，使总数不超过 <paramref name="maxCount"/>。
    /// </summary>
    public static IReadOnlyList<HistoryItem> TrimToLimit(IEnumerable<HistoryItem> items, int maxCount)
    {
        if (maxCount < 1)
            throw new ArgumentOutOfRangeException(nameof(maxCount), "历史上限至少为 1。");

        var ordered = SortForDisplay(items);
        if (ordered.Count <= maxCount)
            return ordered;

        return ordered.Take(maxCount).ToList();
    }
}
