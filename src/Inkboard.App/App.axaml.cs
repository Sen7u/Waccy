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
using Inkboard.Application.Services;
using Inkboard.Infrastructure.Abstractions.Hotkey;
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
    private SettingsService? _settings;
    private TrayIcon? _tray;
    private PopupWindow? _popup;
    private SettingsWindow? _settingsWindow;
    private NativeMenuItem? _pauseItem;
    private NativeMenuItem? _pasteDefaultItem;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override async void OnFrameworkInitializationCompleted()
    {
        Services = AppComposition.Build();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            _settings = Services.GetRequiredService<SettingsService>();
            _popupHost = Services.GetRequiredService<PopupHost>();
            _popup = new PopupWindow
            {
                DataContext = Services.GetRequiredService<PopupViewModel>(),
            };
            _popup.BindHost(_popupHost);

            // 作为主窗口挂接生命周期；启动时软隐藏，热键 / 托盘再唤起（对齐 Maccy）
            desktop.MainWindow = _popup;
            _popupHost.PrepareHidden();

            InstallTray(desktop);

            _hotkeys = Services.GetRequiredService<IHotkeyService>();
            _hotkeys.HotkeyPressed += (_, _) =>
                Dispatcher.UIThread.Post(() => _ = _popupHost.ToggleAsync());

            _settings.Changed += (_, _) =>
                Dispatcher.UIThread.Post(() => _ = OnSettingsChangedAsync());

            try
            {
                var settings = await _settings.LoadAsync().ConfigureAwait(true);
                await _hotkeys.RegisterAsync(settings.PopupHotkey).ConfigureAwait(true);
                SyncTrayChecks(settings.PauseCapture, settings.PasteByDefault);
            }
            catch
            {
                // 热键注册失败不阻断启动；窗口内 Ctrl+Shift+V / 托盘仍可用
            }

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

    private async Task OnSettingsChangedAsync()
    {
        if (_settings is null || _hotkeys is null)
            return;

        try
        {
            var s = await _settings.LoadAsync().ConfigureAwait(true);
            SyncTrayChecks(s.PauseCapture, s.PasteByDefault);
            await _hotkeys.RegisterAsync(s.PopupHotkey).ConfigureAwait(true);
        }
        catch
        {
            // 重注册失败时保留旧热键，不弹错打断用户
        }
    }

    private void SyncTrayChecks(bool pauseCapture, bool pasteByDefault)
    {
        if (_pauseItem is not null)
            _pauseItem.IsChecked = pauseCapture;
        if (_pasteDefaultItem is not null)
            _pasteDefaultItem.IsChecked = pasteByDefault;
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

        _pauseItem = new NativeMenuItem("暂停捕获")
        {
            ToggleType = MenuItemToggleType.CheckBox,
        };
        _pauseItem.Click += (_, _) =>
            Dispatcher.UIThread.Post(() => _ = TogglePauseCaptureAsync());

        _pasteDefaultItem = new NativeMenuItem("选中后默认粘贴")
        {
            ToggleType = MenuItemToggleType.CheckBox,
            IsChecked = true,
        };
        _pasteDefaultItem.Click += (_, _) =>
            Dispatcher.UIThread.Post(() => _ = TogglePasteByDefaultAsync());

        var openSettings = new NativeMenuItem("设置…");
        openSettings.Click += (_, _) =>
            Dispatcher.UIThread.Post(OpenSettings);

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
            Menu = new NativeMenu
            {
                Items =
                {
                    toggle,
                    _pauseItem,
                    _pasteDefaultItem,
                    openSettings,
                    quit,
                },
            },
        };
        _tray.Clicked += (_, _) =>
            Dispatcher.UIThread.Post(() => _ = _popupHost?.ToggleAsync());
    }

    private async Task TogglePauseCaptureAsync()
    {
        if (_settings is null)
            return;

        await _settings.UpdateAsync(s => s.PauseCapture = !s.PauseCapture).ConfigureAwait(true);
    }

    private async Task TogglePasteByDefaultAsync()
    {
        if (_settings is null)
            return;

        await _settings.UpdateAsync(s => s.PasteByDefault = !s.PasteByDefault).ConfigureAwait(true);
    }

    private void OpenSettings()
    {
        if (_settingsWindow is { IsVisible: true })
        {
            _settingsWindow.Activate();
            return;
        }

        _settingsWindow = new SettingsWindow
        {
            DataContext = Services.GetRequiredService<SettingsViewModel>(),
        };
        _settingsWindow.Closed += (_, _) => _settingsWindow = null;
        _settingsWindow.Show();
    }
}
