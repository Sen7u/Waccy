using System;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Inkboard.Application.Services;
using Inkboard.Infrastructure.Abstractions.Settings;

namespace Inkboard.App.ViewModels;

/// <summary>
/// 设置页：编辑快照，Save 才落盘；热键重注册由 SettingsService.Changed 驱动。
/// </summary>
public partial class SettingsViewModel : ViewModelBase
{
    private readonly SettingsService _settings;

    public SettingsViewModel(SettingsService settings) => _settings = settings;

    [ObservableProperty]
    private int _historyLimit = 200;

    [ObservableProperty]
    private string _popupHotkey = "Ctrl+Shift+V";

    [ObservableProperty]
    private bool _pasteByDefault = true;

    [ObservableProperty]
    private string _ignoredAppsText = string.Empty;

    [ObservableProperty]
    private string _statusText = string.Empty;

    public async Task LoadAsync()
    {
        var s = await _settings.LoadAsync().ConfigureAwait(true);
        HistoryLimit = s.HistoryLimit;
        PopupHotkey = s.PopupHotkey;
        PasteByDefault = s.PasteByDefault;
        IgnoredAppsText = string.Join(Environment.NewLine, s.IgnoredApps);
        StatusText = string.Empty;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (HistoryLimit < 1)
            HistoryLimit = 1;
        if (HistoryLimit > 5000)
            HistoryLimit = 5000;

        var hotkey = string.IsNullOrWhiteSpace(PopupHotkey)
            ? "Ctrl+Shift+V"
            : PopupHotkey.Trim();

        var ignored = IgnoredAppsText
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(line => line.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        // 保留托盘可能刚改过的 PauseCapture，避免设置页覆盖
        var current = await _settings.LoadAsync().ConfigureAwait(true);
        await _settings.SaveAsync(new AppSettings
        {
            HistoryLimit = HistoryLimit,
            PopupHotkey = hotkey,
            PasteByDefault = PasteByDefault,
            PauseCapture = current.PauseCapture,
            IgnoredApps = ignored,
        }).ConfigureAwait(true);

        PopupHotkey = hotkey;
        StatusText = "已保存";
    }
}
