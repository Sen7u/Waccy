namespace Inkboard.Domain.Rules;

using Inkboard.Domain.Entities;
using Inkboard.Domain.Enums;

/// <summary>
/// 历史列表纯规则：去重、置顶排序、上限裁剪。不含 IO。
/// </summary>
public static class HistoryRules
{
    /// <summary>
    /// 文本比预览；图片/文件比载荷字节，避免「同类型不同内容」被误合并。
    /// </summary>
    public static bool IsDuplicateOf(HistoryItem newer, HistoryItem? existing)
    {
        if (existing is null || newer.Kind != existing.Kind)
            return false;

        if (newer.Kind == ClipboardContentKind.Text)
            return string.Equals(newer.Preview, existing.Preview, StringComparison.Ordinal);

        return newer.Payload.AsSpan().SequenceEqual(existing.Payload);
    }

    /// <summary>置顶优先，再按复制时间倒序。</summary>
    public static IReadOnlyList<HistoryItem> SortForDisplay(IEnumerable<HistoryItem> items)
    {
        return items
            .OrderByDescending(i => i.IsPinned)
            .ThenByDescending(i => i.CopiedAt)
            .ToList();
    }

    /// <summary>保留置顶 + 最近非置顶，使总数不超过上限。</summary>
    public static IReadOnlyList<HistoryItem> TrimToLimit(IEnumerable<HistoryItem> items, int maxCount)
    {
        if (maxCount < 1)
            throw new ArgumentOutOfRangeException(nameof(maxCount), "历史上限至少为 1。");

        var ordered = SortForDisplay(items);
        return ordered.Count <= maxCount ? ordered : ordered.Take(maxCount).ToList();
    }
}
