using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using Avalonia.Threading;
using Inkboard.App.Composition;
using Inkboard.App.Services;
using Inkboard.App.ViewModels;
using Inkboard.App.Views;
using Inkboard.Infrastructure.Abstractions.Hotkey;
using Inkboard.Infrastructure.Abstractions.Settings;
using Microsoft.Extensions.DependencyInjection;

namespace Inkboard.App;

/// <summary>
/// 应用入口：组装 DI、弹出层宿主、托盘与热键切换。
/// 使用 Avalonia.Application 全名，避免与 Inkboard.Application 程序集冲突。
/// </summary>
public partial class App : Avalonia.Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    private PopupHost? _popupHost;
    private IHotkeyService? _hotkeys;
    private TrayIcon? _tray;
    private PopupWindow? _popup;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override async void OnFrameworkInitializationCompleted()
    {
        Services = AppComposition.Build();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            _popupHost = Services.GetRequiredService<PopupHost>();
            _popup = new PopupWindow
            {
                DataContext = Services.GetRequiredService<PopupViewModel>(),
            };
            _popup.BindHost(_popupHost);

            // 作为主窗口挂接生命周期；平时可隐藏，热键 / 托盘再唤起
            desktop.MainWindow = _popup;
            await _popupHost.ShowAsync();

            InstallTray(desktop);

            _hotkeys = Services.GetRequiredService<IHotkeyService>();
            _hotkeys.HotkeyPressed += (_, _) =>
                Dispatcher.UIThread.Post(() => _ = _popupHost.ToggleAsync());

            var settings = await Services.GetRequiredService<ISettingsStore>()
                .LoadAsync()
                .ConfigureAwait(true);
            await _hotkeys.RegisterAsync(settings.PopupHotkey).ConfigureAwait(true);

            desktop.ShutdownRequested += async (_, _) =>
            {
                _popup.AllowClose();
                _tray?.Dispose();
                if (_hotkeys is not null)
                    await _hotkeys.DisposeAsync().ConfigureAwait(true);
                if (Services is IDisposable d)
                    d.Dispose();
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>
    /// Linux 全局热键受限时，托盘是隐藏后再打开的可靠入口。
    /// </summary>
    private void InstallTray(IClassicDesktopStyleApplicationLifetime desktop)
    {
        WindowIcon? icon = null;
        try
        {
            using var stream = AssetLoader.Open(new Uri("avares://Inkboard.App/Assets/tray.png"));
            icon = new WindowIcon(stream);
        }
        catch
        {
            // 无图标也不阻塞启动；菜单仍可用
        }

        var toggle = new NativeMenuItem("显示 / 隐藏");
        toggle.Click += (_, _) =>
            Dispatcher.UIThread.Post(() => _ = _popupHost?.ToggleAsync());

        var quit = new NativeMenuItem("退出");
        quit.Click += (_, _) =>
        {
            _popup?.AllowClose();
            desktop.Shutdown();
        };

        _tray = new TrayIcon
        {
            Icon = icon,
            ToolTipText = "Inkboard",
            IsVisible = true,
            Menu = new NativeMenu { Items = { toggle, quit } },
        };
        _tray.Clicked += (_, _) =>
            Dispatcher.UIThread.Post(() => _ = _popupHost?.ToggleAsync());
    }
}
