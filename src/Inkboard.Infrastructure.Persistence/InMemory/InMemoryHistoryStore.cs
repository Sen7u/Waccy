namespace Inkboard.Infrastructure.Persistence.InMemory;

using System.Collections.Concurrent;
using Inkboard.Domain.Entities;
using Inkboard.Infrastructure.Abstractions.Persistence;

/// <summary>
/// 内存历史库：脚手架与单测用，后续由 SQLite 实现替换注册即可。
/// </summary>
public sealed class InMemoryHistoryStore : IHistoryStore
{
    private readonly ConcurrentDictionary<Guid, HistoryItem> _items = new();

    public Task<IReadOnlyList<HistoryItem>> ListAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<HistoryItem> snapshot = _items.Values.ToList();
        return Task.FromResult(snapshot);
    }

    public Task UpsertAsync(HistoryItem item, CancellationToken cancellationToken = default)
    {
        _items[item.Id] = item;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _items.TryRemove(id, out _);
        return Task.CompletedTask;
    }

    public Task ClearUnpinnedAsync(CancellationToken cancellationToken = default)
    {
        foreach (var id in _items.Where(kv => !kv.Value.IsPinned).Select(kv => kv.Key).ToList())
            _items.TryRemove(id, out _);
        return Task.CompletedTask;
    }
}
