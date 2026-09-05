using CommunityToolkit.Mvvm.ComponentModel;
using Inkboard.Application.Services;

namespace Inkboard.App.ViewModels;

/// <summary>
/// 主窗口骨架 ViewModel：验证 DI 接线；功能列表后续替换。
/// </summary>
public partial class MainViewModel : ViewModelBase
{
    private readonly HistoryService _history;

    public MainViewModel(HistoryService history)
    {
        _history = history;
        StatusText = "Inkboard 脚手架就绪 — 分层 / DI / 主题令牌已就位";
    }

    [ObservableProperty]
    private string _statusText = string.Empty;

    /// <summary>占位：证明应用层已注入（后续接列表绑定）。</summary>
    public string HistoryServiceName => _history.GetType().Name;
}
