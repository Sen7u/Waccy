namespace Inkboard.App.Composition;

using System.Runtime.InteropServices;
using Inkboard.App.Services;
using Inkboard.App.ViewModels;
using Inkboard.Application.DependencyInjection;
using Inkboard.Infrastructure.Persistence.DependencyInjection;
using Inkboard.Platform.Linux.DependencyInjection;
using Inkboard.Platform.Windows.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// DI 组合根：按 OS 只挂一套平台适配器。
/// </summary>
public static class AppComposition
{
    public static ServiceProvider Build()
    {
        var services = new ServiceCollection();

        services.AddInkboardApplication();
        services.AddInkboardPersistence();
        AddPlatform(services);

        services.AddSingleton<PopupHost>();
        services.AddTransient<PopupViewModel>();
        services.AddTransient<SettingsViewModel>();

        return services.BuildServiceProvider();
    }

    private static void AddPlatform(IServiceCollection services)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            services.AddInkboardWindowsPlatform();
            return;
        }

        // Linux 及其他：走 Linux 适配器（含窗口内热键兜底）
        services.AddInkboardLinuxPlatform();
    }
}
