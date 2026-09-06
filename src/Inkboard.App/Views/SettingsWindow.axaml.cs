using System;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Inkboard.App.ViewModels;

namespace Inkboard.App.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        AvaloniaXamlLoader.Load(this);
        Opened += OnOpened;
    }

    private async void OnOpened(object? sender, EventArgs e)
    {
        if (DataContext is SettingsViewModel vm)
            await vm.LoadAsync();
    }
}
