namespace Inkboard.Infrastructure.Abstractions.Persistence;

using Inkboard.Domain.Entities;

/// <summary>
/// 历史持久化端口。
/// </summary>
public interface IHistoryStore
{
    Task<IReadOnlyList<HistoryItem>> ListAsync(CancellationToken cancellationToken = default);

    Task UpsertAsync(HistoryItem item, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task ClearUnpinnedAsync(CancellationToken cancellationToken = default);
}
