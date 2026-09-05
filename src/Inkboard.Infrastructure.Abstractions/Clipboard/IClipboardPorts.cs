namespace Inkboard.Infrastructure.Abstractions.Clipboard;

using Inkboard.Domain.Entities;

/// <summary>
/// 剪贴板变化通知（平台适配器实现）。
/// </summary>
public interface IClipboardMonitor : IAsyncDisposable
{
    /// <summary>系统剪贴板出现新内容时触发。</summary>
    event EventHandler<HistoryItem>? Changed;

    /// <summary>开始监听。可重复调用，实现应自行幂等。</summary>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>停止监听。</summary>
    Task StopAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// 向系统剪贴板写入内容。
/// </summary>
public interface IClipboardWriter
{
    Task WriteAsync(HistoryItem item, CancellationToken cancellationToken = default);
}

/// <summary>
/// 向当前前台应用发送粘贴快捷键（如 Ctrl+V）。
/// Linux 在部分会话下可能无法实现，此时实现可为空操作。
/// </summary>
public interface IPasteSimulator
{
    Task PasteAsync(CancellationToken cancellationToken = default);
}
