namespace Inkboard.Infrastructure.Persistence.DependencyInjection;

using Inkboard.Infrastructure.Abstractions.Persistence;
using Inkboard.Infrastructure.Abstractions.Settings;
using Inkboard.Infrastructure.Persistence.InMemory;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 持久化实现注册。骨架阶段使用内存实现，换 SQLite 时只改这里。
/// </summary>
public static class PersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddInkboardPersistence(this IServiceCollection services)
    {
        services.AddSingleton<IHistoryStore, InMemoryHistoryStore>();
        services.AddSingleton<ISettingsStore, InMemorySettingsStore>();
        return services;
    }
}
