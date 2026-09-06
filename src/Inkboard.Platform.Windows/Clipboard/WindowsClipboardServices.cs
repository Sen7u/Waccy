namespace Inkboard.Platform.Windows.Clipboard;

using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using Inkboard.Domain.Entities;
using Inkboard.Domain.Enums;
using Inkboard.Infrastructure.Abstractions.Clipboard;
using Inkboard.Infrastructure.Abstractions.Focus;
using SkiaSharp;
using TextCopy;

/// <summary>
/// Windows 剪贴板监听：优先文本，其次 PNG/DIB 图片；TextCopy 轮询 + 轻量 Win32 读图。
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class WindowsClipboardMonitor : IClipboardMonitor
{
    private readonly IForegroundAppInfo _foregroundApp;
    private readonly PeriodicTimer _timer = new(TimeSpan.FromMilliseconds(400));
    private CancellationTokenSource? _cts;
    private Task? _loop;
    private string? _lastText;
    private int _lastImageHash;

    public WindowsClipboardMonitor(IForegroundAppInfo foregroundApp)
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
            if (TryCaptureText(out var textItem))
            {
                Changed?.Invoke(this, textItem!);
                continue;
            }

            if (TryCaptureImage(out var imageItem))
                Changed?.Invoke(this, imageItem!);
        }
    }

    private bool TryCaptureText(out HistoryItem? item)
    {
        item = null;
        string? text;
        try
        {
            text = ClipboardService.GetText();
        }
        catch
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(text) || text == _lastText)
            return false;

        _lastText = text;
        _lastImageHash = 0;
        item = new HistoryItem
        {
            Preview = text.Length > 500 ? text[..500] : text,
            Kind = ClipboardContentKind.Text,
            Payload = Encoding.UTF8.GetBytes(text),
            CopiedAt = DateTimeOffset.UtcNow,
            SourceApp = _foregroundApp.TryGetProcessName(),
        };
        return true;
    }

    private bool TryCaptureImage(out HistoryItem? item)
    {
        item = null;
        if (!WindowsClipboardImage.TryReadPng(out var png) || png is null || png.Length == 0)
            return false;

        var hash = ComputeHash(png);
        if (hash == _lastImageHash)
            return false;

        _lastImageHash = hash;
        _lastText = null;
        var sizeLabel = DescribePng(png);
        item = new HistoryItem
        {
            Preview = sizeLabel,
            Kind = ClipboardContentKind.Image,
            Payload = png,
            CopiedAt = DateTimeOffset.UtcNow,
            SourceApp = _foregroundApp.TryGetProcessName(),
        };
        return true;
    }

    private static int ComputeHash(byte[] data)
    {
        // 足够去抖：不全量哈希大图
        var h = data.Length;
        var step = Math.Max(1, data.Length / 64);
        for (var i = 0; i < data.Length; i += step)
            h = HashCode.Combine(h, data[i]);
        return h;
    }

    private static string DescribePng(byte[] png)
    {
        try
        {
            using var bitmap = SKBitmap.Decode(png);
            return bitmap is null ? "图片" : $"图片 {bitmap.Width}×{bitmap.Height}";
        }
        catch
        {
            return "图片";
        }
    }
}

[SupportedOSPlatform("windows")]
public sealed class WindowsClipboardWriter : IClipboardWriter
{
    public Task WriteAsync(HistoryItem item, CancellationToken cancellationToken = default)
    {
        if (item.Kind == ClipboardContentKind.Image)
        {
            WindowsClipboardImage.WritePng(item.Payload);
            return Task.CompletedTask;
        }

        var text = item.Kind == ClipboardContentKind.Text && item.Payload.Length > 0
            ? Encoding.UTF8.GetString(item.Payload)
            : item.Preview;
        ClipboardService.SetText(text);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Windows 粘贴：keybd_event 模拟 Ctrl+V。调用前须已交回焦点。
/// </summary>
[SupportedOSPlatform("windows")]
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

/// <summary>
/// Win32 剪贴板图片读写：优先注册格式 PNG，其次 CF_DIB 再编码为 PNG。
/// </summary>
[SupportedOSPlatform("windows")]
internal static class WindowsClipboardImage
{
    private const uint CfDib = 8;
    private const uint GmemMoveable = 0x0002;
    private static readonly uint PngFormat = RegisterClipboardFormat("PNG");

    public static bool TryReadPng(out byte[]? png)
    {
        png = null;
        if (!OpenClipboard(IntPtr.Zero))
            return false;

        try
        {
            if (PngFormat != 0 && IsClipboardFormatAvailable(PngFormat))
            {
                png = ReadHGlobalBytes(GetClipboardData(PngFormat));
                return png is { Length: > 0 };
            }

            if (!IsClipboardFormatAvailable(CfDib))
                return false;

            var dib = ReadHGlobalBytes(GetClipboardData(CfDib));
            if (dib is null || dib.Length == 0)
                return false;

            png = DibToPng(dib);
            return png is { Length: > 0 };
        }
        catch
        {
            return false;
        }
        finally
        {
            CloseClipboard();
        }
    }

    public static void WritePng(byte[] png)
    {
        if (png.Length == 0 || PngFormat == 0)
            return;

        if (!OpenClipboard(IntPtr.Zero))
            return;

        try
        {
            EmptyClipboard();
            var handle = CopyToHGlobal(png);
            if (handle != IntPtr.Zero)
                SetClipboardData(PngFormat, handle);
        }
        finally
        {
            CloseClipboard();
        }
    }

    private static byte[]? ReadHGlobalBytes(IntPtr handle)
    {
        if (handle == IntPtr.Zero)
            return null;

        var size = checked((int)GlobalSize(handle));
        if (size <= 0)
            return null;

        var ptr = GlobalLock(handle);
        if (ptr == IntPtr.Zero)
            return null;

        try
        {
            var bytes = new byte[size];
            Marshal.Copy(ptr, bytes, 0, size);
            return bytes;
        }
        finally
        {
            GlobalUnlock(handle);
        }
    }

    private static IntPtr CopyToHGlobal(byte[] data)
    {
        var handle = GlobalAlloc(GmemMoveable, (UIntPtr)data.Length);
        if (handle == IntPtr.Zero)
            return IntPtr.Zero;

        var ptr = GlobalLock(handle);
        if (ptr == IntPtr.Zero)
            return IntPtr.Zero;

        try
        {
            Marshal.Copy(data, 0, ptr, data.Length);
        }
        finally
        {
            GlobalUnlock(handle);
        }

        return handle;
    }

    private static byte[]? DibToPng(byte[] dib)
    {
        // CF_DIB = BITMAPINFOHEADER + 可选调色板 + 像素；补 BMP 文件头后交给 Skia
        if (dib.Length < 40)
            return null;

        var headerSize = BitConverter.ToInt32(dib, 0);
        if (headerSize < 40)
            return null;

        var bitCount = BitConverter.ToInt16(dib, 14);
        var colorsUsed = BitConverter.ToInt32(dib, 32);
        var paletteEntries = bitCount <= 8
            ? (colorsUsed != 0 ? colorsUsed : 1 << bitCount)
            : 0;
        var pixelOffset = 14 + headerSize + paletteEntries * 4;

        var bmp = new byte[14 + dib.Length];
        bmp[0] = (byte)'B';
        bmp[1] = (byte)'M';
        BitConverter.TryWriteBytes(bmp.AsSpan(2, 4), bmp.Length);
        BitConverter.TryWriteBytes(bmp.AsSpan(10, 4), pixelOffset);
        Buffer.BlockCopy(dib, 0, bmp, 14, dib.Length);

        using var bitmap = SKBitmap.Decode(bmp);
        if (bitmap is null)
            return null;

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 90);
        return data?.ToArray();
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool OpenClipboard(IntPtr hWndNewOwner);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool CloseClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool EmptyClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool IsClipboardFormatAvailable(uint format);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr GetClipboardData(uint format);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetClipboardData(uint format, IntPtr hMem);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern uint RegisterClipboardFormat(string format);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalLock(IntPtr hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GlobalUnlock(IntPtr hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern UIntPtr GlobalSize(IntPtr hMem);
}
