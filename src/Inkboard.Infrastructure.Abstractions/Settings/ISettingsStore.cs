namespace Inkboard.Infrastructure.Abstractions.Settings;

/// <summary>
/// 用户设置端口（骨架：仅历史上限与热键手势）。
/// </summary>
public interface ISettingsStore
{
    Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default);
}

/// <summary>
/// 应用设置快照。后续 Appearance 等字段在此扩展，避免散落魔法字符串。
/// </summary>
public sealed class AppSettings
{
    public int HistoryLimit { get; set; } = 200;

    public string PopupHotkey { get; set; } = "Ctrl+Shift+V";

    public IList<string> IgnoredApps { get; set; } = new List<string>();

    public bool PauseCapture { get; set; }
}
