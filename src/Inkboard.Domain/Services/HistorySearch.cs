namespace Inkboard.Domain.Services;

using Inkboard.Domain.Entities;

/// <summary>
/// 历史搜索（领域服务）：骨架阶段仅做预览子串精确匹配（忽略大小写）。
/// </summary>
public sealed class HistorySearch
{
    public IReadOnlyList<HistoryItem> Search(IEnumerable<HistoryItem> items, string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return items.ToList();

        return items
            .Where(i => i.Preview.Contains(query.Trim(), StringComparison.OrdinalIgnoreCase))
            .ToList();
    }
}
