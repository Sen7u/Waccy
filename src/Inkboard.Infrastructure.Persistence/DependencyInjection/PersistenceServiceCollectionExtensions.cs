namespace Inkboard.Infrastructure.Persistence.DependencyInjection;

using Inkboard.Infrastructure.Abstractions.Persistence;
using Inkboard.Infrastructure.Abstractions.Settings;
using Inkboard.Infrastructure.Persistence.InMemory;
using Inkboard.Infrastructure.Persistence.Json;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 持久化实现注册。历史仍内存；设置落盘 JSON，换 SQLite 历史时只改这里。
/// </summary>
public static class PersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddInkboardPersistence(this IServiceCollection services)
    {
        services.AddSingleton<IHistoryStore, InMemoryHistoryStore>();
        services.AddSingleton<ISettingsStore, JsonSettingsStore>();
        return services;
    }
}
