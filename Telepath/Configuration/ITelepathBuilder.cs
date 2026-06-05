using Telepath.Middleware;

namespace Telepath.Configuration;

public interface ITelepathBuilder
{
    ITelepathBuilder Configure(BotSettings settings);
    ITelepathBuilder Use<T>() where T : ITelepathMiddleware;
    ITelepathBuilder Use<T>(Func<IServiceProvider, T> factory) where T : ITelepathMiddleware;
    ITelepathBuilder UseState(Action<StateConfigurator> configure);
}
