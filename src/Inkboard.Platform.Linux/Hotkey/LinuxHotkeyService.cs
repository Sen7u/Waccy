using System;
using System.Threading;
using System.Threading.Tasks;
using Inkboard.Infrastructure.Abstractions.Hotkey;

namespace Inkboard.Platform.Linux.Hotkey;

/// <summary>
/// Linux 热键：全局抓键在 Wayland 下不可靠。
/// 这里提供可测试的事件源；真正切换依赖 App 内 Ctrl+Shift+V 兜底与显式 Toggle。
/// 若后续接入 X11 XGrabKey / portal，只需替换本类。
/// </summary>
public sealed class LinuxHotkeyService : IHotkeyService
{
    public event EventHandler? HotkeyPressed;

    private string _gesture = "Ctrl+Shift+V";

    public Task RegisterAsync(string gesture, CancellationToken cancellationToken = default)
    {
        _gesture = string.IsNullOrWhiteSpace(gesture) ? "Ctrl+Shift+V" : gesture.Trim();
        // 占位：记录手势，供诊断；全局抓键留待平台成熟后再做。
        _ = _gesture;
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    /// <summary>供窗口内快捷键或调试调用，触发与全局热键相同的事件。</summary>
    public void RaisePressed() => HotkeyPressed?.Invoke(this, EventArgs.Empty);
}
