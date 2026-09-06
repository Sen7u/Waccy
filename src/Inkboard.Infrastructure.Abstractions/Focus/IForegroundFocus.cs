namespace Inkboard.Infrastructure.Abstractions.Focus;

/// <summary>
/// 记录/恢复前台窗口，用于弹出层关闭后把焦点交回原应用（对齐 Maccy resignKey）。
/// </summary>
public interface IForegroundFocus
{
    /// <summary>在弹出层抢焦点前调用，记下当时的前台窗口。</summary>
    void Capture();

    /// <summary>尝试把焦点交回 Capture 时的窗口。成功返回 true。</summary>
    bool TryRestore();
}
