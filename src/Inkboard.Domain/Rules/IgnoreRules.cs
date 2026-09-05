namespace Inkboard.Domain.Rules;

/// <summary>
/// 忽略规则：命中则不写入历史。
/// 骨架阶段仅提供应用名精确匹配；后续可扩展正则与格式过滤。
/// </summary>
public sealed class IgnoreRules
{
    private readonly HashSet<string> _ignoredApps;

    public IgnoreRules(IEnumerable<string>? ignoredApps = null)
    {
        _ignoredApps = new HashSet<string>(
            ignoredApps ?? Array.Empty<string>(),
            StringComparer.OrdinalIgnoreCase);
    }

    public bool ShouldSkip(string? sourceApp)
    {
        if (string.IsNullOrWhiteSpace(sourceApp))
            return false;

        return _ignoredApps.Contains(sourceApp);
    }
}
