namespace Inkboard.Platform.Linux.DependencyInjection;

using Inkboard.Infrastructure.Abstractions.Clipboard;
using Inkboard.Infrastructure.Abstractions.Focus;
using Inkboard.Infrastructure.Abstractions.Hotkey;
using Inkboard.Infrastructure.Abstractions.Screen;
using Inkboard.Infrastructure.Abstractions.Tray;
using Inkboard.Platform.Linux.Clipboard;
using Inkboard.Platform.Linux.Focus;
using Inkboard.Platform.Linux.Hotkey;
using Inkboard.Platform.Linux.Screen;
using Inkboard.Platform.Linux.Tray;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 仅在 Linux 上注册的平台适配器。
/// </summary>
public static class LinuxPlatformServiceCollectionExtensions
{
    public static IServiceCollection AddInkboardLinuxPlatform(this IServiceCollection services)
    {
        services.AddSingleton<IClipboardMonitor, LinuxClipboardMonitor>();
        services.AddSingleton<IClipboardWriter, LinuxClipboardWriter>();
        services.AddSingleton<IPasteSimulator, LinuxPasteSimulator>();
        services.AddSingleton<IHotkeyService, LinuxHotkeyService>();
        services.AddSingleton<ITrayService, LinuxTrayService>();
        services.AddSingleton<IPointerScreen, LinuxPointerScreen>();
        services.AddSingleton<IForegroundFocus, LinuxForegroundFocus>();
        services.AddSingleton<IForegroundAppInfo, LinuxForegroundAppInfo>();
        return services;
    }
}
