namespace Inkboard.Platform.Windows.Tray;

using Inkboard.Infrastructure.Abstractions.Tray;

/// <summary>
/// Windows 托盘状态骨架；实际图标可由 Avalonia TrayIcon 承载。
/// </summary>
public sealed class WindowsTrayService : ITrayService
{
    public bool IsPaused { get; private set; }

    public void SetPaused(bool paused) => IsPaused = paused;
}
