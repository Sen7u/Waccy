namespace Inkboard.Application.Services;

using Inkboard.Domain.Entities;
using Inkboard.Domain.Rules;
using Inkboard.Domain.Services;
using Inkboard.Infrastructure.Abstractions.Persistence;
using Inkboard.Infrastructure.Abstractions.Settings;

/// <summary>
/// 历史用例：读取、搜索、删除、清空、置顶。
/// 不直接碰平台 API。
/// </summary>
public sealed class HistoryService
{
    private readonly IHistoryStore _store;
    private readonly ISettingsStore _settings;
    private readonly HistorySearch _search = new();

    public HistoryService(IHistoryStore store, ISettingsStore settings)
    {
        _store = store;
        _settings = settings;
    }

    public async Task<IReadOnlyList<HistoryItem>> GetVisibleAsync(
        string? query = null,
        CancellationToken cancellationToken = default)
    {
        var items = await _store.ListAsync(cancellationToken).ConfigureAwait(false);
        var sorted = HistoryRules.SortForDisplay(items);
        return query is null ? sorted : _search.Search(sorted, query);
    }

    public async Task CaptureAsync(HistoryItem item, CancellationToken cancellationToken = default)
    {
        var settings = await _settings.LoadAsync(cancellationToken).ConfigureAwait(false);
        if (settings.PauseCapture)
            return;

        var ignore = new IgnoreRules(settings.IgnoredApps);
        if (ignore.ShouldSkip(item.SourceApp))
            return;

        var existing = await _store.ListAsync(cancellationToken).ConfigureAwait(false);
        var latest = HistoryRules.SortForDisplay(existing).FirstOrDefault(i => !i.IsPinned);

        if (HistoryRules.IsDuplicateOf(item, latest) && latest is not null)
        {
            latest.CopiedAt = item.CopiedAt;
            await _store.UpsertAsync(latest, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await _store.UpsertAsync(item, cancellationToken).ConfigureAwait(false);
        }

        var all = await _store.ListAsync(cancellationToken).ConfigureAwait(false);
        var trimmed = HistoryRules.TrimToLimit(all, settings.HistoryLimit);
        var trimmedIds = trimmed.Select(t => t.Id).ToHashSet();
        foreach (var orphan in all.Where(a => !trimmedIds.Contains(a.Id)))
            await _store.DeleteAsync(orphan.Id, cancellationToken).ConfigureAwait(false);
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        => _store.DeleteAsync(id, cancellationToken);

    public Task ClearUnpinnedAsync(CancellationToken cancellationToken = default)
        => _store.ClearUnpinnedAsync(cancellationToken);

    public async Task TogglePinAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var items = await _store.ListAsync(cancellationToken).ConfigureAwait(false);
        var target = items.FirstOrDefault(i => i.Id == id);
        if (target is null)
            return;

        target.PinKey = target.IsPinned ? null : Guid.NewGuid().ToString("N")[..4];
        await _store.UpsertAsync(target, cancellationToken).ConfigureAwait(false);
    }
}
