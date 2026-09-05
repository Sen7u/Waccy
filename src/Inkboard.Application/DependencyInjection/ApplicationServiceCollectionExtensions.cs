namespace Inkboard.Application.DependencyInjection;

using Inkboard.Application.Services;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 应用层服务注册扩展。
/// </summary>
public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddInkboardApplication(this IServiceCollection services)
    {
        services.AddSingleton<HistoryService>();
        return services;
    }
}
