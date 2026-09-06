namespace Inkboard.Infrastructure.Abstractions.Focus;

/// <summary>
/// 读取当前前台应用标识，供剪贴板捕获写入 SourceApp（忽略规则依赖此字段）。
/// </summary>
public interface IForegroundAppInfo
{
    /// <summary>进程名（Windows 为 ProcessName，不含路径）；无法取得时返回 null。</summary>
    string? TryGetProcessName();
}
