using Telepath.Models;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace Telepath.Middleware;

internal class AnswerCallbackQueryMiddleware : ITelepathMiddleware
{
    private readonly ITelegramBotClient _bot;

    public AnswerCallbackQueryMiddleware(ITelegramBotClient bot)
    {
        _bot = bot;
    }

    public async Task InvokeAsync(TelepathContext context, Func<TelepathContext, CancellationToken, Task> next, CancellationToken cancellationToken)
    {
        await next(context, cancellationToken);

        if (context.Update.Type == UpdateType.CallbackQuery && !context.CallbackQueryAnswered)
            await _bot.AnswerCallbackQuery(context.Update.CallbackQuery!.Id, cancellationToken: cancellationToken);
    }
}
