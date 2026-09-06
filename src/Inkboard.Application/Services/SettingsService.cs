namespace Inkboard.Application.Services;

using Inkboard.Infrastructure.Abstractions.Settings;

/// <summary>
/// 设置用例：统一读改与变更广播，避免 UI / 托盘各自直连 store 改脏状态。
/// </summary>
public sealed class SettingsService
{
    private readonly ISettingsStore _store;
    private readonly object _gate = new();
    private AppSettings? _cache;

    public SettingsService(ISettingsStore store) => _store = store;

    /// <summary>Save 成功后触发；订阅方可重注册热键、刷新托盘勾选。</summary>
    public event EventHandler? Changed;

    public async Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            if (_cache is not null)
                return _cache.Clone();
        }

        var loaded = await _store.LoadAsync(cancellationToken).ConfigureAwait(false);
        lock (_gate)
        {
            _cache = loaded.Clone();
            return _cache.Clone();
        }
    }

    public async Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        var snapshot = settings.Clone();
        await _store.SaveAsync(snapshot, cancellationToken).ConfigureAwait(false);
        lock (_gate)
            _cache = snapshot.Clone();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>读-改-写一体，适合托盘勾选这类单字段翻转。</summary>
    public async Task UpdateAsync(
        Action<AppSettings> mutate,
        CancellationToken cancellationToken = default)
    {
        var current = await LoadAsync(cancellationToken).ConfigureAwait(false);
        mutate(current);
        await SaveAsync(current, cancellationToken).ConfigureAwait(false);
    }
}
