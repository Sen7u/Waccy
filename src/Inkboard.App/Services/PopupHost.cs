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
/// 弹出层宿主：集中处理显示/隐藏与进出场动效，避免 Window 代码膨胀。
/// Avalonia 12 不能对 Transform 直接 Animation.RunAsync，故用插值驱动。
/// </summary>
public sealed class PopupHost
{
    private Window? _window;
    private bool _busy;

    public void Attach(Window window) => _window = window;

    public bool IsVisible => _window?.IsVisible == true;

    public async Task ShowAsync()
    {
        if (_window is null || _busy)
            return;

        if (_window.IsVisible)
        {
            _window.Activate();
            return;
        }

        _busy = true;
        try
        {
            PrepareEnter(_window);
            _window.Show();
            _window.Activate();
            await AnimateAsync(_window, 0, 1, 0.96, 1).ConfigureAwait(true);
        }
        finally
        {
            _busy = false;
        }
    }

    public async Task HideAsync()
    {
        if (_window is null || !_window.IsVisible || _busy)
            return;

        _busy = true;
        try
        {
            await AnimateAsync(_window, 1, 0, 1, 0.96).ConfigureAwait(true);
            _window.Hide();
            Reset(_window);
        }
        finally
        {
            _busy = false;
        }
    }

    public Task ToggleAsync() => IsVisible ? HideAsync() : ShowAsync();

    private static void PrepareEnter(Window w)
    {
        w.Opacity = 0;
        w.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);
        w.RenderTransform = new ScaleTransform(0.96, 0.96);
    }

    private static void Reset(Window w)
    {
        w.Opacity = 1;
        w.RenderTransform = null;
    }

    private static Task AnimateAsync(
        Window window,
        double fromOpacity,
        double toOpacity,
        double fromScale,
        double toScale)
    {
        var ms = ReadMs(window, "Motion.PopupMs", 200);
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        Dispatcher.UIThread.Post(async () =>
        {
            try
            {
                var scale = window.RenderTransform as ScaleTransform
                            ?? new ScaleTransform(fromScale, fromScale);
                window.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);
                window.RenderTransform = scale;

                var ease = new CubicEaseOut();
                var duration = TimeSpan.FromMilliseconds(ms);
                var sw = Stopwatch.StartNew();

                while (sw.Elapsed < duration)
                {
                    var t = ease.Ease(sw.Elapsed.TotalMilliseconds / duration.TotalMilliseconds);
                    window.Opacity = Lerp(fromOpacity, toOpacity, t);
                    var s = Lerp(fromScale, toScale, t);
                    scale.ScaleX = s;
                    scale.ScaleY = s;
                    await Task.Delay(16).ConfigureAwait(true);
                }

                window.Opacity = toOpacity;
                scale.ScaleX = toScale;
                scale.ScaleY = toScale;
                tcs.TrySetResult();
            }
            catch (Exception ex)
            {
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
