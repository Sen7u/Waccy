namespace Inkboard.Platform.Linux.Hotkey;

using Inkboard.Infrastructure.Abstractions.Hotkey;

/// <summary>
/// Linux 全局热键骨架。Wayland 下可能受限。
/// </summary>
public sealed class LinuxHotkeyService : IHotkeyService
{
    public event EventHandler? HotkeyPressed;

    public Task RegisterAsync(string gesture, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    internal void RaisePressed() => HotkeyPressed?.Invoke(this, EventArgs.Empty);
}
