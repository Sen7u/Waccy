using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using Inkboard.App.Services;
using Inkboard.App.ViewModels;

namespace Inkboard.App.Views;

public partial class PopupWindow : Window
{
    private PopupHost? _host;
    private bool _allowClose;

    public PopupWindow()
    {
        AvaloniaXamlLoader.Load(this);
        Opened += OnOpened;
        KeyDown += OnKeyDown;
        Closing += OnClosing;
    }

    public void BindHost(PopupHost host)
    {
        _host = host;
        host.Attach(this);
    }

    /// <summary>进程退出时允许真正关闭。</summary>
    public void AllowClose() => _allowClose = true;

    private async void OnOpened(object? sender, EventArgs e)
    {
        if (DataContext is PopupViewModel vm)
            await vm.InitializeAsync();

        if (this.FindControl<TextBox>("SearchBox") is { } search)
            Dispatcher.UIThread.Post(() => search.Focus());
    }

    private async void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_allowClose || _host is null)
            return;

        e.Cancel = true;
        await _host.HideAsync();
    }

    private async void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not PopupViewModel vm)
            return;

        // 窗口内兜底：与设置默认热键一致，弥补 Linux 全局抓键限制
        if (e.KeyModifiers.HasFlag(KeyModifiers.Control) &&
            e.KeyModifiers.HasFlag(KeyModifiers.Shift) &&
            e.Key == Key.V)
        {
            if (_host is not null)
                await _host.ToggleAsync();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Escape)
        {
            if (_host is not null)
                await _host.HideAsync();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Enter)
        {
            await vm.ActivateCommand.ExecuteAsync(vm.SelectedItem);
            if (_host is not null)
                await _host.HideAsync();
            e.Handled = true;
        }
    }

    private async void OnItemDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is not PopupViewModel vm)
            return;

        await vm.ActivateCommand.ExecuteAsync(vm.SelectedItem);
        if (_host is not null)
            await _host.HideAsync();
    }

    /// <summary>
    /// 删除前先做横向淡出：动效时长走 Motion.DeleteMs，避免列表硬切。
    /// </summary>
    private async void OnDeleteClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: HistoryItemRow row } ||
            DataContext is not PopupViewModel vm)
            return;

        if (this.FindControl<ListBox>("HistoryList") is { } list &&
            list.ContainerFromItem(row) is Control container)
        {
            await AnimateDeleteAsync(container).ConfigureAwait(true);
        }

        await vm.DeleteCommand.ExecuteAsync(row).ConfigureAwait(true);
    }

    private static async Task AnimateDeleteAsync(Control container)
    {
        var ms = 150.0;
        if (container.TryGetResource("Motion.DeleteMs", out var v) && v is double d)
            ms = d;
        else if (Avalonia.Application.Current?.TryGetResource("Motion.DeleteMs", out v) == true &&
                 v is double d2)
            ms = d2;

        container.RenderTransformOrigin = new RelativePoint(0, 0.5, RelativeUnit.Relative);
        var tx = new TranslateTransform();
        container.RenderTransform = tx;

        var ease = new CubicEaseIn();
        var duration = TimeSpan.FromMilliseconds(ms);
        var sw = System.Diagnostics.Stopwatch.StartNew();

        while (sw.Elapsed < duration)
        {
            var t = ease.Ease(sw.Elapsed.TotalMilliseconds / duration.TotalMilliseconds);
            container.Opacity = 1 - t;
            tx.X = 24 * t;
            await Task.Delay(16).ConfigureAwait(true);
        }

        container.Opacity = 0;
        tx.X = 24;
    }
}
