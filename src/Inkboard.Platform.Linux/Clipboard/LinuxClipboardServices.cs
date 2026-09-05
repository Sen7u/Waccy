namespace Inkboard.Platform.Linux.Clipboard;

using Inkboard.Domain.Entities;
using Inkboard.Infrastructure.Abstractions.Clipboard;

/// <summary>
/// Linux 剪贴板监听骨架。后续按 X11/Wayland 会话填充；Wayland 限制需在 README 说明。
/// </summary>
public sealed class LinuxClipboardMonitor : IClipboardMonitor
{
    public event EventHandler<HistoryItem>? Changed;

    public Task StartAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    internal void RaiseChanged(HistoryItem item) => Changed?.Invoke(this, item);
}

public sealed class LinuxClipboardWriter : IClipboardWriter
{
    public Task WriteAsync(HistoryItem item, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}

/// <summary>
/// Linux 粘贴模拟：部分环境不可用，骨架为空操作（仅写入剪贴板由上层保证）。
/// </summary>
public sealed class LinuxPasteSimulator : IPasteSimulator
{
    public Task PasteAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
