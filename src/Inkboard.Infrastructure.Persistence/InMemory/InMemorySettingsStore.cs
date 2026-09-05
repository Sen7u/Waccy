namespace Inkboard.Infrastructure.Persistence.InMemory;

using Inkboard.Infrastructure.Abstractions.Settings;

/// <summary>
/// 内存设置存储：脚手架默认实现，后续可换 JSON/SQLite。
/// </summary>
public sealed class InMemorySettingsStore : ISettingsStore
{
    private AppSettings _settings = new();
    private readonly object _gate = new();

    public Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            // 返回副本，避免外部直接改到内部状态
            return Task.FromResult(Clone(_settings));
        }
    }

    public Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            _settings = Clone(settings);
        }

        return Task.CompletedTask;
    }

    private static AppSettings Clone(AppSettings source) => new()
    {
        HistoryLimit = source.HistoryLimit,
        PopupHotkey = source.PopupHotkey,
        PauseCapture = source.PauseCapture,
        IgnoredApps = source.IgnoredApps.ToList(),
    };
}
