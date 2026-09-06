using Inkboard.Infrastructure.Abstractions.Focus;

namespace Inkboard.Platform.Linux.Focus;

/// <summary>
/// Linux 前台应用识别依赖会话类型（X11/Wayland），MVP 返回 null，忽略规则暂不生效。
/// </summary>
public sealed class LinuxForegroundAppInfo : IForegroundAppInfo
{
    public string? TryGetProcessName() => null;
}
