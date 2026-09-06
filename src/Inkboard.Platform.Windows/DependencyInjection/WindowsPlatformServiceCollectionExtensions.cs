namespace Inkboard.Platform.Windows.DependencyInjection;

using Inkboard.Infrastructure.Abstractions.Clipboard;
using Inkboard.Infrastructure.Abstractions.Focus;
using Inkboard.Infrastructure.Abstractions.Hotkey;
using Inkboard.Infrastructure.Abstractions.Screen;
using Inkboard.Infrastructure.Abstractions.Tray;
using Inkboard.Platform.Windows.Clipboard;
using Inkboard.Platform.Windows.Focus;
using Inkboard.Platform.Windows.Hotkey;
using Inkboard.Platform.Windows.Screen;
using Inkboard.Platform.Windows.Tray;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 仅在 Windows 上注册的平台适配器。
/// </summary>
public static class WindowsPlatformServiceCollectionExtensions
{
    public static IServiceCollection AddInkboardWindowsPlatform(this IServiceCollection services)
    {
        services.AddSingleton<IClipboardMonitor, WindowsClipboardMonitor>();
        services.AddSingleton<IClipboardWriter, WindowsClipboardWriter>();
        services.AddSingleton<IPasteSimulator, WindowsPasteSimulator>();
        services.AddSingleton<IHotkeyService, WindowsHotkeyService>();
        services.AddSingleton<ITrayService, WindowsTrayService>();
        services.AddSingleton<IPointerScreen, WindowsPointerScreen>();
        services.AddSingleton<IForegroundFocus, WindowsForegroundFocus>();
        return services;
    }
}
