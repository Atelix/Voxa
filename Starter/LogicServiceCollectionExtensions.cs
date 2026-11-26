using Microsoft.Extensions.DependencyInjection;
using Starter.Abstractions.Logic;
using Starter.Logic;

namespace Starter;

public static class LogicServiceCollectionExtensions
{
    /// <summary>Registers every logic class. Add new ones here so Program.cs stays short.</summary>
    public static IServiceCollection AddStarterLogic(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<IItemLogic, ItemLogic>();
        return services;
    }
}
