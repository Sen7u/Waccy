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
using Avalonia.VisualTree;
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
        Width = PopupHost.DefaultWidth;
        Height = PopupHost.DefaultHeight;
        Opened += OnOpened;
        KeyDown += OnKeyDown;
        Closing += OnClosing;
    }

    public void BindHost(PopupHost host)
    {
        if (_host is not null)
            _host.Shown -= OnHostShown;

        _host = host;
        host.Attach(this);
        host.Shown += OnHostShown;
    }

    /// <summary>进程退出时允许真正关闭。</summary>
    public void AllowClose() => _allowClose = true;

    private async void OnOpened(object? sender, EventArgs e)
    {
        if (DataContext is PopupViewModel vm)
            await vm.InitializeAsync();

        FocusSearch();
    }

    /// <summary>每次热键唤起都重新聚焦搜索（Opened 只在首次 Show 触发）。</summary>
    private void OnHostShown(object? sender, EventArgs e)
    {
        if (DataContext is PopupViewModel vm && vm.SelectedItem is null && vm.Items.Count > 0)
            vm.SelectedItem = vm.Items[0];

        FocusSearch();
    }

    private void FocusSearch()
    {
        if (this.FindControl<TextBox>("SearchBox") is { } search)
            Dispatcher.UIThread.Post(() => search.Focus(), DispatcherPriority.Input);
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

        // 搜索框聚焦时仍可用方向键浏览列表（对齐 Maccy）
        if (e.Key is Key.Up or Key.Down && vm.Items.Count > 0)
        {
            MoveSelection(vm, e.Key == Key.Down ? 1 : -1);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Enter)
        {
            // Alt+Enter：复制并粘贴到原前台应用（对齐 Maccy ⌥+Enter）
            var paste = e.KeyModifiers.HasFlag(KeyModifiers.Alt);
            await ConfirmSelectionAsync(vm, paste);
            e.Handled = true;
        }
    }

    /// <summary>
    /// 单击条目即复制并关闭（对齐 Maccy 空修饰键）。
    /// Alt+单击：复制并粘贴到原应用。
    /// </summary>
    private async void OnItemTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is not PopupViewModel vm)
            return;

        if (e.Source is Control source && source.FindAncestorOfType<Button>() is not null)
            return;

        HistoryItemRow? row = null;
        if (e.Source is Control c)
        {
            var container = c.FindAncestorOfType<ListBoxItem>();
            if (container?.DataContext is HistoryItemRow fromContainer)
                row = fromContainer;
        }

        row ??= vm.SelectedItem;
        if (row is null)
            return;

        vm.SelectedItem = row;
        var paste = e.KeyModifiers.HasFlag(KeyModifiers.Alt);
        await ConfirmSelectionAsync(vm, paste);
    }

    private async Task ConfirmSelectionAsync(PopupViewModel vm, bool pasteAfter = false)
    {
        await vm.ActivateCommand.ExecuteAsync(vm.SelectedItem);
        if (_host is not null)
            await _host.HideAsync(pasteAfter);
    }

    private static void MoveSelection(PopupViewModel vm, int delta)
    {
        if (vm.Items.Count == 0)
            return;

        var index = vm.SelectedItem is null
            ? (delta > 0 ? 0 : vm.Items.Count - 1)
            : vm.Items.IndexOf(vm.SelectedItem);

        if (index < 0)
            index = 0;

        index = Math.Clamp(index + delta, 0, vm.Items.Count - 1);
        vm.SelectedItem = vm.Items[index];
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
