using Telepath.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Telepath.Configuration;

public class StateConfigurator
{
    private readonly IServiceCollection _services;

    internal StateConfigurator(IServiceCollection services)
    {
        _services = services;
    }

    public StateConfigurator UseMemoryCache()
    {
        _services.AddMemoryCache();
        _services.AddSingleton<IStateService, MemoryCacheStateService>();
        return this;
    }

    public StateConfigurator Use<T>() where T : class, IStateService
    {
        _services.AddSingleton<IStateService, T>();
        return this;
    }
}
