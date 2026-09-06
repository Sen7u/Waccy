namespace Inkboard.Platform.Windows.Clipboard;

using System.Text;
using System.Runtime.InteropServices;
using Inkboard.Domain.Entities;
using Inkboard.Domain.Enums;
using Inkboard.Infrastructure.Abstractions.Clipboard;
using TextCopy;

/// <summary>
/// Windows 文本剪贴板监听（TextCopy 轮询）。后续可换 Win32 消息以降低延迟。
/// </summary>
public sealed class WindowsClipboardMonitor : IClipboardMonitor
{
    private readonly PeriodicTimer _timer = new(TimeSpan.FromMilliseconds(400));
    private CancellationTokenSource? _cts;
    private Task? _loop;
    private string? _lastText;

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
                continue;
            }

            if (string.IsNullOrWhiteSpace(text) || text == _lastText)
                continue;

            _lastText = text;
            Changed?.Invoke(this, new HistoryItem
            {
                Preview = text.Length > 500 ? text[..500] : text,
                Kind = ClipboardContentKind.Text,
                Payload = Encoding.UTF8.GetBytes(text),
                CopiedAt = DateTimeOffset.UtcNow,
            });
        }
    }
}

public sealed class WindowsClipboardWriter : IClipboardWriter
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
/// Windows 粘贴：keybd_event 模拟 Ctrl+V（对齐 Maccy Clipboard.paste）。
/// 调用前须已把焦点交回目标应用。
/// </summary>
public sealed class WindowsPasteSimulator : IPasteSimulator
{
    private const byte VkControl = 0x11;
    private const byte VkV = 0x56;
    private const uint KeyUp = 0x0002;

    public Task PasteAsync(CancellationToken cancellationToken = default)
    {
        keybd_event(VkControl, 0, 0, UIntPtr.Zero);
        keybd_event(VkV, 0, 0, UIntPtr.Zero);
        keybd_event(VkV, 0, KeyUp, UIntPtr.Zero);
        keybd_event(VkControl, 0, KeyUp, UIntPtr.Zero);
        return Task.CompletedTask;
    }

    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);
}
