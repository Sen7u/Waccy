using Inkboard.Infrastructure.Abstractions.Focus;

namespace Inkboard.Platform.Linux.Focus;

/// <summary>
/// Linux 焦点恢复：Wayland 下不可靠，占位以免拖累弹出层关闭路径。
/// </summary>
public sealed class LinuxForegroundFocus : IForegroundFocus
{
    public void Capture()
    {
    }

    public bool TryRestore() => false;
}
