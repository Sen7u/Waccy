using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using Inkboard.Infrastructure.Abstractions.Screen;

namespace Inkboard.App.Services;

/// <summary>
/// 弹出层宿主：对齐 Maccy FloatingPanel —
/// 贴光标显示、失焦关闭、软隐藏（移出屏幕）。
///
/// Linux/X11：动效只打 RootChrome；Window.Opacity 保持 1。
/// </summary>
public sealed class PopupHost
{
    /// <summary>对齐 Maccy Defaults.windowSize 默认宽。</summary>
    public const double DefaultWidth = 450;

    /// <summary>首屏可见高度：比 Maccy 默认 max 800 更紧凑，仍可滚动。</summary>
    public const double DefaultHeight = 380;

    private readonly IPointerScreen _pointer;
    private Window? _window;
    private Control? _chrome;
    private bool _busy;
    private bool _open;
    private bool _ignoreDeactivate;
    /// <summary>软隐藏待命中：禁止 TryRepair 把窗口又拉回前台。</summary>
    private bool _dormant;

    public PopupHost(IPointerScreen pointer) => _pointer = pointer;

    public void Attach(Window window)
    {
        if (_window is not null)
        {
            _window.Activated -= OnActivated;
            _window.Deactivated -= OnDeactivated;
            _window.PropertyChanged -= OnWindowPropertyChanged;
        }

        _window = window;
        _chrome = window.FindControl<Control>("RootChrome");
        window.Activated += OnActivated;
        window.Deactivated += OnDeactivated;
        window.PropertyChanged += OnWindowPropertyChanged;
        window.Opacity = 1;
        window.Width = DefaultWidth;
        window.Height = DefaultHeight;
        window.ShowInTaskbar = false;
    }

    public bool IsOpen => _open && _window?.IsVisible == true;

    /// <summary>启动时软藏起，不闪主屏中心。</summary>
    public void PrepareHidden()
    {
        if (_window is null)
            return;

        EnsureChrome();
        _ignoreDeactivate = true;
        try
        {
            _window.ShowInTaskbar = false;
            _window.IsHitTestVisible = false;
            _window.Topmost = false;
            if (!_window.IsVisible)
                _window.Show();
            SoftHideOffscreen();
            Reset(Chrome);
            _open = false;
            _dormant = true;
        }
        finally
        {
            _ignoreDeactivate = false;
        }
    }

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
        _ignoreDeactivate = true;
        try
        {
            var target = Chrome;
            _window.Opacity = 1;
            _window.IsHitTestVisible = true;
            _window.Topmost = true;
            _window.ShowInTaskbar = false;
            _window.Width = DefaultWidth;
            _window.Height = DefaultHeight;

            PrepareEnter(target);

            if (!_window.IsVisible)
                _window.Show();

            if (_window.WindowState != WindowState.Normal)
                _window.WindowState = WindowState.Normal;

            _dormant = false;

            // 对齐 Maccy popupPosition=cursor：每次唤醒重新贴光标
            _window.Position = ResolveCursorOrigin(
                (int)Math.Round(_window.Width * (_window.RenderScaling <= 0 ? 1 : _window.RenderScaling)),
                (int)Math.Round(_window.Height * (_window.RenderScaling <= 0 ? 1 : _window.RenderScaling)));

            _window.Activate();

            await AnimateAsync(target, 0, 1, 0.96, 1).ConfigureAwait(true);
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
            // 稍后再听失焦，避免 Activate 过程误关
            Dispatcher.UIThread.Post(() => _ignoreDeactivate = false, DispatcherPriority.Background);
        }
    }

    public async Task HideAsync()
    {
        if (_window is null || !_open || _busy)
            return;

        EnsureChrome();
        _busy = true;
        _ignoreDeactivate = true;
        try
        {
            var target = Chrome;
            await AnimateAsync(target, 1, 0, 1, 0.96).ConfigureAwait(true);

            _window.IsHitTestVisible = false;
            _window.Topmost = false;
            _window.Opacity = 1;
            SoftHideOffscreen();
            Reset(target);
            _open = false;
            _dormant = true;
        }
        finally
        {
            _busy = false;
            _ignoreDeactivate = false;
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

    private void SoftHideOffscreen()
    {
        if (_window is null)
            return;
        _window.Position = new PixelPoint(-32000, -32000);
    }

    /// <summary>
    /// Maccy：窗口顶边贴光标、主体在光标下方，并钳进工作区。
    /// Windows/Avalonia 坐标原点左上、Y 向下，直接以光标为左上角再钳制即可。
    /// </summary>
    private PixelPoint ResolveCursorOrigin(int pixelWidth, int pixelHeight)
    {
        var cursor = _pointer.GetCursorPosition();
        var work = ResolveWorkingArea(cursor);

        var x = cursor.X;
        var y = cursor.Y;
        x = Math.Clamp(x, work.X, Math.Max(work.X, work.Right - pixelWidth));
        y = Math.Clamp(y, work.Y, Math.Max(work.Y, work.Bottom - pixelHeight));
        return new PixelPoint(x, y);
    }

    private ScreenRect ResolveWorkingArea(ScreenPoint cursor)
    {
        if (_window?.Screens.ScreenFromPoint(new PixelPoint(cursor.X, cursor.Y)) is { } screen)
        {
            var b = screen.WorkingArea;
            return new ScreenRect(b.X, b.Y, b.Width, b.Height);
        }

        return _pointer.GetWorkingAreaContaining(cursor);
    }

    private static void EnsureShownVisuals(Control target)
    {
        target.Opacity = 1;
        target.RenderTransform = null;
    }

    private void OnActivated(object? sender, EventArgs e) => TryRepair();

    private void OnDeactivated(object? sender, EventArgs e)
    {
        // 对齐 Maccy FloatingPanel.resignKey → close
        if (_ignoreDeactivate || !_open || _busy)
            return;

        Dispatcher.UIThread.Post(() =>
        {
            if (_ignoreDeactivate || !_open || _busy)
                return;
            _ = HideAsync();
        });
    }

    private void OnWindowPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == Window.WindowStateProperty || e.Property == Visual.IsVisibleProperty)
            TryRepair();
    }

    private void TryRepair()
    {
        if (_window is null || _busy || _open || _dormant)
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
