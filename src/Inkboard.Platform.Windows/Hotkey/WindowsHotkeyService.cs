namespace Inkboard.Platform.Windows.Hotkey;

using Inkboard.Infrastructure.Abstractions.Hotkey;

/// <summary>
/// Windows 全局热键骨架（后续 RegisterHotKey）。
/// </summary>
public sealed class WindowsHotkeyService : IHotkeyService
{
    public event EventHandler? HotkeyPressed;

    public Task RegisterAsync(string gesture, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    internal void RaisePressed() => HotkeyPressed?.Invoke(this, EventArgs.Empty);
}
