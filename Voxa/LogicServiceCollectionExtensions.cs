using Microsoft.Extensions.DependencyInjection;
using Voxa.Abstractions.Logic;
using Voxa.Logic;

namespace Voxa;

public static class LogicServiceCollectionExtensions
{
    /// <summary>Registers every logic class. Add new ones here so Program.cs stays short.</summary>
    public static IServiceCollection AddVoxaLogic(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<IItemLogic, ItemLogic>();
        return services;
    }
}
