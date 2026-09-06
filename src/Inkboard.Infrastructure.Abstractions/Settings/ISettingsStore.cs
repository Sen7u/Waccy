namespace Inkboard.Infrastructure.Abstractions.Settings;

/// <summary>
/// 用户设置端口。实现负责序列化与原子写；调用方拿到的是副本。
/// </summary>
public interface ISettingsStore
{
    Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default);
}

/// <summary>
/// 应用设置快照。新字段在此扩展，避免散落魔法字符串。
/// </summary>
public sealed class AppSettings
{
    public int HistoryLimit { get; set; } = 200;

    public string PopupHotkey { get; set; } = "Ctrl+Shift+V";

    public IList<string> IgnoredApps { get; set; } = new List<string>();

    public bool PauseCapture { get; set; }

    /// <summary>
    /// 选中历史后是否自动粘贴。true 时 Alt 改为仅复制；false 时 Alt 改为粘贴（对齐 Maccy pasteByDefault）。
    /// </summary>
    public bool PasteByDefault { get; set; } = true;

    public AppSettings Clone() => new()
    {
        HistoryLimit = HistoryLimit,
        PopupHotkey = PopupHotkey,
        PauseCapture = PauseCapture,
        PasteByDefault = PasteByDefault,
        IgnoredApps = IgnoredApps.ToList(),
    };

    /// <summary>Alt（或其它反转键）按下时与默认粘贴行为异或。</summary>
    public bool ShouldPaste(bool invertHeld) => PasteByDefault ^ invertHeld;
}
