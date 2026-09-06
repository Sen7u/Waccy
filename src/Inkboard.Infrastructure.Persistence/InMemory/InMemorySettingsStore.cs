namespace Inkboard.Infrastructure.Persistence.InMemory;

using Inkboard.Infrastructure.Abstractions.Settings;

/// <summary>
/// 内存设置存储：单测与无磁盘场景使用。
/// </summary>
public sealed class InMemorySettingsStore : ISettingsStore
{
    private AppSettings _settings = new();
    private readonly object _gate = new();

    public Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        lock (_gate)
            return Task.FromResult(_settings.Clone());
    }

    public Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        lock (_gate)
            _settings = settings.Clone();

        return Task.CompletedTask;
    }
}
