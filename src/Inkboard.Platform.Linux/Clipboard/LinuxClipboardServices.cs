namespace Inkboard.Platform.Linux.Clipboard;

using System.Text;
using Inkboard.Domain.Entities;
using Inkboard.Domain.Enums;
using Inkboard.Infrastructure.Abstractions.Clipboard;
using Inkboard.Infrastructure.Abstractions.Focus;
using TextCopy;

/// <summary>
/// Linux 文本剪贴板：用 TextCopy 读写，定时轮询变更（Wayland/X11 通用折中）。
/// 图片/文件格式后续再补原生实现。
/// </summary>
public sealed class LinuxClipboardMonitor : IClipboardMonitor
{
    private readonly IForegroundAppInfo _foregroundApp;
    private readonly PeriodicTimer _timer = new(TimeSpan.FromMilliseconds(500));
    private CancellationTokenSource? _cts;
    private Task? _loop;
    private string? _lastText;

    public LinuxClipboardMonitor(IForegroundAppInfo foregroundApp)
        => _foregroundApp = foregroundApp;

    public event EventHandler<HistoryItem>? Changed;

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_loop is { IsCompleted: false })
            return Task.CompletedTask;

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _loop = RunAsync(_cts.Token);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_cts is null)
            return;

        _cts.Cancel();
        if (_loop is not null)
        {
            try { await _loop.ConfigureAwait(false); }
            catch (OperationCanceledException) { }
        }

        _cts.Dispose();
        _cts = null;
        _loop = null;
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);
        _timer.Dispose();
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        while (await _timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
        {
            string? text;
            try
            {
                text = await ClipboardService.GetTextAsync(cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                // 会话无剪贴板权限时忽略本轮
                continue;
            }

            if (string.IsNullOrWhiteSpace(text) || text == _lastText)
                continue;

            _lastText = text;
            var item = new HistoryItem
            {
                Preview = text.Length > 500 ? text[..500] : text,
                Kind = ClipboardContentKind.Text,
                Payload = Encoding.UTF8.GetBytes(text),
                CopiedAt = DateTimeOffset.UtcNow,
                SourceApp = _foregroundApp.TryGetProcessName(),
            };
            Changed?.Invoke(this, item);
        }
    }
}

public sealed class LinuxClipboardWriter : IClipboardWriter
{
    public async Task WriteAsync(HistoryItem item, CancellationToken cancellationToken = default)
    {
        var text = item.Kind == ClipboardContentKind.Text
            ? Encoding.UTF8.GetString(item.Payload)
            : item.Preview;
        await ClipboardService.SetTextAsync(text, cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
/// Linux 粘贴：多数会话无法稳定注入按键，骨架为空操作（上层仍会写入剪贴板）。
/// </summary>
public sealed class LinuxPasteSimulator : IPasteSimulator
{
    public Task PasteAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
