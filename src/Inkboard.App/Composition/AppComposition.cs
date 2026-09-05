namespace Inkboard.App.Composition;

using System.Runtime.InteropServices;
using Inkboard.Application.DependencyInjection;
using Inkboard.Infrastructure.Persistence.DependencyInjection;
using Inkboard.Platform.Linux.DependencyInjection;
using Inkboard.Platform.Windows.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// DI 组合根：集中注册各层积木，并按 OS 只挂载一套平台适配器。
/// </summary>
public static class AppComposition
{
    public static ServiceProvider Build()
    {
        var services = new ServiceCollection();

        services.AddInkboardApplication();
        services.AddInkboardPersistence();
        AddPlatform(services);

        services.AddTransient<ViewModels.MainViewModel>();
        services.AddTransient<ViewModels.PopupViewModel>();

        return services.BuildServiceProvider();
    }

    private static void AddPlatform(IServiceCollection services)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            services.AddInkboardWindowsPlatform();
            return;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            services.AddInkboardLinuxPlatform();
            return;
        }

        // 其他 OS：先挂 Linux 桩，避免启动即崩；正式支持前可再拆。
        services.AddInkboardLinuxPlatform();
    }
}
