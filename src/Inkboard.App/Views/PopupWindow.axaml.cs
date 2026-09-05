using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Inkboard.App.ViewModels;

namespace Inkboard.App.Views;

public partial class PopupWindow : Window
{
    public PopupWindow()
    {
        AvaloniaXamlLoader.Load(this);
        Opened += async (_, _) =>
        {
            if (DataContext is PopupViewModel vm)
                await vm.InitializeAsync();
        };
        KeyDown += OnKeyDown;
    }

    private async void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not PopupViewModel vm)
            return;

        if (e.Key == Key.Escape)
        {
            Close();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Enter)
        {
            await vm.ActivateCommand.ExecuteAsync(vm.SelectedItem);
            e.Handled = true;
        }
    }

    private async void OnItemDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is PopupViewModel vm)
            await vm.ActivateCommand.ExecuteAsync(vm.SelectedItem);
    }
}
