namespace Inkboard.Platform.Linux.Tray;

using Inkboard.Infrastructure.Abstractions.Tray;

/// <summary>
/// Linux 托盘状态骨架。
/// </summary>
public sealed class LinuxTrayService : ITrayService
{
    public bool IsPaused { get; private set; }

    public void SetPaused(bool paused) => IsPaused = paused;
}
