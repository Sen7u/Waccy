namespace Inkboard.Platform.Windows.Clipboard;

using Inkboard.Domain.Entities;
using Inkboard.Infrastructure.Abstractions.Clipboard;

/// <summary>
/// Windows 剪贴板监听骨架。后续用 Win32（AddClipboardFormatListener 等）填充。
/// </summary>
public sealed class WindowsClipboardMonitor : IClipboardMonitor
{
    public event EventHandler<HistoryItem>? Changed;

    public Task StartAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    // 供后续实现触发事件，避免未使用警告
    internal void RaiseChanged(HistoryItem item) => Changed?.Invoke(this, item);
}

/// <summary>
/// Windows 剪贴板写入骨架。
/// </summary>
public sealed class WindowsClipboardWriter : IClipboardWriter
{
    public Task WriteAsync(HistoryItem item, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}

/// <summary>
/// Windows 粘贴模拟骨架（后续发送 Ctrl+V）。
/// </summary>
public sealed class WindowsPasteSimulator : IPasteSimulator
{
    public Task PasteAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
