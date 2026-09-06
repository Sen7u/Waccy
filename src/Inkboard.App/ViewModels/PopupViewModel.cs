using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Inkboard.Application.Services;
using Inkboard.Domain.Entities;
using Inkboard.Domain.Enums;
using Inkboard.Infrastructure.Abstractions.Clipboard;

namespace Inkboard.App.ViewModels;

/// <summary>
/// 弹出层状态：搜索 / 列表 / 激活写回 / 置顶 / 删除。
/// ViewModel 保持薄，规则仍在 Application。
/// </summary>
public partial class PopupViewModel : ViewModelBase
{
    private readonly HistoryService _history;
    private readonly ClipboardCaptureService _capture;
    private readonly IClipboardWriter _writer;

    public PopupViewModel(
        HistoryService history,
        ClipboardCaptureService capture,
        IClipboardWriter writer)
    {
        _history = history;
        _capture = capture;
        _writer = writer;
        _capture.HistoryChanged += (_, _) =>
            Avalonia.Threading.Dispatcher.UIThread.Post(() => _ = RefreshAsync());
    }

    public ObservableCollection<HistoryItemRow> Items { get; } = new();

    public bool HasItems => Items.Count > 0;
    public bool IsEmpty => Items.Count == 0;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private HistoryItemRow? _selectedItem;

    [ObservableProperty]
    private string _statusText = "复制任意文本，会出现在这里";

    partial void OnSearchTextChanged(string value) => _ = RefreshAsync();

    public async Task InitializeAsync()
    {
        await _capture.StartAsync().ConfigureAwait(true);
        await RefreshAsync().ConfigureAwait(true);
    }

    public async Task RefreshAsync()
    {
        var query = string.IsNullOrWhiteSpace(SearchText) ? null : SearchText;
        var list = await _history.GetVisibleAsync(query).ConfigureAwait(true);

        // 尽量保留选中项，避免刷新后键盘焦点丢失
        var selectedId = SelectedItem?.Id;
        Items.Clear();
        foreach (var item in list)
            Items.Add(HistoryItemRow.From(item));

        SelectedItem = selectedId is Guid id
            ? Items.FirstOrDefault(i => i.Id == id) ?? Items.FirstOrDefault()
            : Items.FirstOrDefault();

        OnPropertyChanged(nameof(HasItems));
        OnPropertyChanged(nameof(IsEmpty));

        StatusText = IsEmpty
            ? "还没有历史 — 复制点什么吧"
            : $"{Items.Count} 条记录";
    }

    [RelayCommand]
    private async Task Activate(object? parameter)
    {
        var row = parameter as HistoryItemRow ?? SelectedItem;
        if (row is null)
            return;

        await _writer.WriteAsync(row.ToEntity()).ConfigureAwait(true);
        StatusText = "已复制";
        SearchText = string.Empty;
    }

    [RelayCommand]
    private async Task Delete(object? parameter)
    {
        if (parameter is not HistoryItemRow row)
            return;

        await _history.DeleteAsync(row.Id).ConfigureAwait(true);
        await RefreshAsync().ConfigureAwait(true);
    }

    [RelayCommand]
    private async Task TogglePin(object? parameter)
    {
        if (parameter is not HistoryItemRow row)
            return;

        await _history.TogglePinAsync(row.Id).ConfigureAwait(true);
        await RefreshAsync().ConfigureAwait(true);
    }

    [RelayCommand]
    private async Task Clear()
    {
        await _history.ClearUnpinnedAsync().ConfigureAwait(true);
        await RefreshAsync().ConfigureAwait(true);
    }
}

/// <summary>列表行投影：只保留展示所需字段。</summary>
public sealed class HistoryItemRow
{
    public Guid Id { get; init; }
    public string Preview { get; init; } = string.Empty;
    public string Meta { get; init; } = string.Empty;
    public bool IsPinned { get; init; }
    /// <summary>置顶星透明度：已置顶实心，未置顶淡显可点。</summary>
    public double PinOpacity => IsPinned ? 1.0 : 0.35;
    public byte[] Payload { get; init; } = Array.Empty<byte>();
    public DateTimeOffset CopiedAt { get; init; }

    public static HistoryItemRow From(HistoryItem item) => new()
    {
        Id = item.Id,
        Preview = string.IsNullOrWhiteSpace(item.Preview)
            ? "(空)"
            : item.Preview.Replace('\r', ' ').Replace('\n', ' '),
        Meta = (item.IsPinned ? "置顶 · " : string.Empty)
               + item.CopiedAt.ToLocalTime().ToString("HH:mm:ss"),
        IsPinned = item.IsPinned,
        Payload = item.Payload,
        CopiedAt = item.CopiedAt,
    };

    public HistoryItem ToEntity() => new()
    {
        Id = Id,
        Preview = Preview,
        Kind = ClipboardContentKind.Text,
        Payload = Payload.Length > 0 ? Payload : Encoding.UTF8.GetBytes(Preview),
        CopiedAt = CopiedAt,
        PinKey = IsPinned ? "pin" : null,
    };
}
