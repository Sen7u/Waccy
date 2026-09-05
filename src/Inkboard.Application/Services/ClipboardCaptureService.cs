namespace Inkboard.Application.Services;

using Inkboard.Domain.Entities;
using Inkboard.Infrastructure.Abstractions.Clipboard;

/// <summary>
/// 把剪贴板监听接到历史用例：平台事件 → CaptureAsync。
/// </summary>
public sealed class ClipboardCaptureService : IAsyncDisposable
{
    private readonly IClipboardMonitor _monitor;
    private readonly HistoryService _history;

    public ClipboardCaptureService(IClipboardMonitor monitor, HistoryService history)
    {
        _monitor = monitor;
        _history = history;
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _monitor.Changed -= OnChanged;
        _monitor.Changed += OnChanged;
        await _monitor.StartAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
        => _monitor.StopAsync(cancellationToken);

    public async ValueTask DisposeAsync()
    {
        _monitor.Changed -= OnChanged;
        await _monitor.DisposeAsync().ConfigureAwait(false);
    }

    private async void OnChanged(object? sender, HistoryItem item)
    {
        try
        {
            await _history.CaptureAsync(item).ConfigureAwait(false);
            HistoryChanged?.Invoke(this, EventArgs.Empty);
        }
        catch
        {
            // 捕获失败不打断监听循环
        }
    }

    /// <summary>历史有新增/更新时通知 UI 刷新。</summary>
    public event EventHandler? HistoryChanged;
}
