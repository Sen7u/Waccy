using System;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Inkboard.App.Composition;
using Inkboard.App.ViewModels;
using Inkboard.App.Views;
using Microsoft.Extensions.DependencyInjection;

namespace Inkboard.App;

/// <summary>
/// Avalonia 应用入口。注意：不能写裸 Application，会与 Inkboard.Application 程序集命名空间冲突。
/// </summary>
public partial class App : Avalonia.Application
{
    /// <summary>进程级服务提供者，窗口关闭时释放。</summary>
    public static IServiceProvider Services { get; private set; } = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        Services = AppComposition.Build();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = Services.GetRequiredService<MainViewModel>(),
            };

            desktop.ShutdownRequested += (_, _) =>
            {
                if (Services is IDisposable disposable)
                    disposable.Dispose();
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
