using Telepath.Models;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace Telepath.Middleware;

internal class BotInfoMiddleware : ITelepathMiddleware
{
    private readonly Lazy<Task<User>> _me;

    public BotInfoMiddleware(ITelegramBotClient bot)
    {
        _me = new Lazy<Task<User>>(() => bot.GetMe());
    }

    public async Task InvokeAsync(TelepathContext context, Func<TelepathContext, CancellationToken, Task> next, CancellationToken cancellationToken)
    {
        context.BotUser = await _me.Value;
        await next(context, cancellationToken);
    }
}
