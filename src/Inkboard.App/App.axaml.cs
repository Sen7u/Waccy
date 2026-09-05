using System;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Inkboard.App.Composition;
using Inkboard.App.ViewModels;
using Inkboard.App.Views;
using Microsoft.Extensions.DependencyInjection;

namespace Inkboard.App;

/// <summary>
/// Avalonia 应用入口。使用 Avalonia.Application 全名，避免与 Inkboard.Application 程序集冲突。
/// </summary>
public partial class App : Avalonia.Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        Services = AppComposition.Build();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new PopupWindow
            {
                DataContext = Services.GetRequiredService<PopupViewModel>(),
            };

            desktop.ShutdownRequested += (_, _) =>
            {
                if (Services is IDisposable d)
                    d.Dispose();
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
