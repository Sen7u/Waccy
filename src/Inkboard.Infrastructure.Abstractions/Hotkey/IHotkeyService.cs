namespace Inkboard.Infrastructure.Abstractions.Hotkey;

/// <summary>
/// 全局热键服务。
/// </summary>
public interface IHotkeyService : IAsyncDisposable
{
    /// <summary>用户按下已注册热键时触发。</summary>
    event EventHandler? HotkeyPressed;

    /// <summary>
    /// 注册/更新弹出层热键。格式约定由实现解析（例如 "Ctrl+Shift+V"）。
    /// </summary>
    Task RegisterAsync(string gesture, CancellationToken cancellationToken = default);
}
