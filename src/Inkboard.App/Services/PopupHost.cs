using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;

namespace Inkboard.App.Services;

/// <summary>
/// 弹出层宿主：集中处理显示/隐藏与进出场动效。
///
/// 关键点（Linux/X11 透明窗）：
/// 1. 动效只打在 RootChrome，Window.Opacity 永远为 1——对 Window 做透明度会留下空壳。
/// 2. 隐藏用软隐藏（移出屏幕 + 关闭命中），不 Hide/Minimize。
/// </summary>
public sealed class PopupHost
{
    private Window? _window;
    private Control? _chrome;
    private bool _busy;
    private bool _open;
    private PixelPoint? _restoredPosition;

    public void Attach(Window window)
    {
        if (_window is not null)
        {
            _window.Activated -= OnActivated;
            _window.PropertyChanged -= OnWindowPropertyChanged;
        }

        _window = window;
        // 必须在 Loaded 后解析；若此时为 null，首次 Show 再找一次
        _chrome = window.FindControl<Control>("RootChrome");
        window.Activated += OnActivated;
        window.PropertyChanged += OnWindowPropertyChanged;
        window.Opacity = 1;
    }

    public bool IsOpen => _open && _window?.IsVisible == true;

    public async Task ShowAsync()
    {
        if (_window is null || _busy)
            return;

        EnsureChrome();
        if (IsOpen && Chrome.Opacity >= 0.99)
        {
            _window.Activate();
            return;
        }

        _busy = true;
        try
        {
            var target = Chrome;
            // 窗口本体禁止透明
            _window.Opacity = 1;
            _window.IsHitTestVisible = true;
            _window.Topmost = true;
            _window.ShowInTaskbar = true;

            PrepareEnter(target);

            if (!_window.IsVisible)
                _window.Show();

            if (_window.WindowState != WindowState.Normal)
                _window.WindowState = WindowState.Normal;

            if (_restoredPosition is { } pos)
                _window.Position = pos;

            _window.Activate();

            await AnimateAsync(target, 0, 1, 0.96, 1).ConfigureAwait(true);
            // 兜底：动画中断时也不能停在 0
            EnsureShownVisuals(target);
            _open = true;
        }
        catch
        {
            if (_chrome is not null)
                EnsureShownVisuals(_chrome);
            throw;
        }
        finally
        {
            _busy = false;
        }
    }

    public async Task HideAsync()
    {
        if (_window is null || !_open || _busy)
            return;

        EnsureChrome();
        _busy = true;
        try
        {
            var target = Chrome;
            _restoredPosition = _window.Position;

            await AnimateAsync(target, 1, 0, 1, 0.96).ConfigureAwait(true);

            _window.IsHitTestVisible = false;
            _window.Topmost = false;
            _window.Opacity = 1;
            // 软隐藏：移出屏幕，保留托管状态
            _window.Position = new PixelPoint(-32000, -32000);
            // 藏好后把 chrome 重置为不透明，避免下次外部激活时先露出空壳
            Reset(target);
            _open = false;
        }
        finally
        {
            _busy = false;
        }
    }

    public Task ToggleAsync() => IsOpen ? HideAsync() : ShowAsync();

    private Control Chrome => _chrome ?? _window!;

    private void EnsureChrome()
    {
        if (_window is null)
            return;
        _chrome ??= _window.FindControl<Control>("RootChrome");
    }

    private static void EnsureShownVisuals(Control target)
    {
        target.Opacity = 1;
        target.RenderTransform = null;
    }

    private void OnActivated(object? sender, EventArgs e) => TryRepair();

    private void OnWindowPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == Window.WindowStateProperty || e.Property == Visual.IsVisibleProperty)
            TryRepair();
    }

    private void TryRepair()
    {
        if (_window is null || _busy || _open)
            return;

        if (_window.IsVisible && _window.WindowState != WindowState.Minimized)
            Dispatcher.UIThread.Post(() => _ = ShowAsync());
    }

    private static void PrepareEnter(Control c)
    {
        c.Opacity = 0;
        c.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);
        c.RenderTransform = new ScaleTransform(0.96, 0.96);
    }

    private static void Reset(Control c)
    {
        c.Opacity = 1;
        c.RenderTransform = null;
    }

    private static Task AnimateAsync(
        Control control,
        double fromOpacity,
        double toOpacity,
        double fromScale,
        double toScale)
    {
        var ms = ReadMs(control, "Motion.PopupMs", 200);
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        Dispatcher.UIThread.Post(async () =>
        {
            try
            {
                var scale = control.RenderTransform as ScaleTransform
                            ?? new ScaleTransform(fromScale, fromScale);
                control.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);
                control.RenderTransform = scale;
                control.Opacity = fromOpacity;

                var ease = new CubicEaseOut();
                var duration = TimeSpan.FromMilliseconds(ms);
                var sw = Stopwatch.StartNew();

                while (sw.Elapsed < duration)
                {
                    var t = ease.Ease(sw.Elapsed.TotalMilliseconds / duration.TotalMilliseconds);
                    control.Opacity = Lerp(fromOpacity, toOpacity, t);
                    var s = Lerp(fromScale, toScale, t);
                    scale.ScaleX = s;
                    scale.ScaleY = s;
                    await Task.Delay(16).ConfigureAwait(true);
                }

                control.Opacity = toOpacity;
                scale.ScaleX = toScale;
                scale.ScaleY = toScale;
                tcs.TrySetResult();
            }
            catch (Exception ex)
            {
                control.Opacity = toOpacity;
                tcs.TrySetException(ex);
            }
        });

        return tcs.Task;
    }

    private static double Lerp(double a, double b, double t) => a + (b - a) * t;

    private static double ReadMs(StyledElement el, string key, double fallback)
    {
        if (el.TryGetResource(key, out var v) && v is double d)
            return d;
        if (Avalonia.Application.Current?.TryGetResource(key, out v) == true && v is double d2)
            return d2;
        return fallback;
    }
}
