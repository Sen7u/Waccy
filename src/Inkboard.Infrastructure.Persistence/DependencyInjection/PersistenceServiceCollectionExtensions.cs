namespace Inkboard.Infrastructure.Persistence.DependencyInjection;

using Inkboard.Infrastructure.Abstractions.Persistence;
using Inkboard.Infrastructure.Abstractions.Settings;
using Inkboard.Infrastructure.Persistence.Json;
using Inkboard.Infrastructure.Persistence.Sqlite;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 持久化实现注册。历史 SQLite、设置 JSON；单测可自行替换为内存实现。
/// </summary>
public static class PersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddInkboardPersistence(this IServiceCollection services)
    {
        services.AddSingleton<IHistoryStore, SqliteHistoryStore>();
        services.AddSingleton<ISettingsStore, JsonSettingsStore>();
        return services;
    }
}
