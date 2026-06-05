using Telepath.Models;
using Telepath.Routing.Handlers;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;

namespace Telepath.Routing.Internal;

internal class Route<T> : IRoute where T : IRouterHandler
{
    private readonly Predicate<TelepathContext> _predicate;

    public Route(Predicate<TelepathContext> predicate)
    {
        _predicate = predicate;
    }

    public bool Matches(TelepathContext context) => _predicate(context);

    public async Task ExecuteAsync(IServiceProvider provider, TelepathContext context, CancellationToken ct)
    {
        await using var scope = provider.CreateAsyncScope();
        var bot = scope.ServiceProvider.GetRequiredService<ITelegramBotClient>();
        var handler = scope.ServiceProvider.GetRequiredService<T>();
        await handler.ExecuteAsync(context, bot, ct);
    }
}
